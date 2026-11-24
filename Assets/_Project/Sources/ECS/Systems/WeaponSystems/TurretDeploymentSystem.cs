using System.Collections.Generic;
using ECS.AttackComponents;
using ECS.CommonComponents;
using ECS.ProjectileComponents;
using ECS.ProjectileSystems;
using ECS.WeaponComponents;
using Gameplay.Navigation;
using Gameplay.View;
using Scellecs.Morpeh;
using UnityEngine;
using WeaponConfigs;

namespace ECS.WeaponSystems
{
    internal sealed class TurretDeploymentSystem : ISystem
    {
        private const float MinimumSpacing = 0.8f;

        private readonly Transform _container;
        private readonly NavigationGrid _navigation;
        private readonly List<Vector3> _occupied = new List<Vector3>();
        private readonly HashSet<Entity> _expired = new HashSet<Entity>();

        private Filter _deployments;
        private Filter _turrets;
        private Filter _projectiles;
        private WeaponFactory _weapons;
        private ProjectileCleanup _projectileCleanup;

        private Stash<TurretDeploymentComponent> _deploymentStash;
        private Stash<TurretComponent> _turretStash;
        private Stash<WeaponComponent> _weaponStash;
        private Stash<PositionComponent> _positionStash;
        private Stash<AttackComponent> _attackStash;
        private Stash<TargetComponent> _targetStash;
        private Stash<AttackStatusComponent> _statusStash;
        private Stash<DeathComponent> _deathStash;
        private Stash<ProjectileComponent> _projectileStash;
        private Stash<CleanupRequest> _cleanupStash;

        public World World { get; set; }

        internal TurretDeploymentSystem(Transform container, NavigationGrid navigation)
        {
            _container = container;
            _navigation = navigation;
        }

        public void OnAwake()
        {
            _deployments = World.Filter.With<TurretDeploymentComponent>().Build();
            _turrets = World.Filter.With<TurretComponent>().Build();
            _projectiles = World.Filter.With<ProjectileComponent>().Build();
            _weapons = new WeaponFactory(World);
            _projectileCleanup = new ProjectileCleanup(World);

            _deploymentStash = World.GetStash<TurretDeploymentComponent>();
            _turretStash = World.GetStash<TurretComponent>();
            _weaponStash = World.GetStash<WeaponComponent>();
            _positionStash = World.GetStash<PositionComponent>();
            _attackStash = World.GetStash<AttackComponent>();
            _targetStash = World.GetStash<TargetComponent>();
            _statusStash = World.GetStash<AttackStatusComponent>();
            _deathStash = World.GetStash<DeathComponent>();
            _projectileStash = World.GetStash<ProjectileComponent>();
            _cleanupStash = World.GetStash<CleanupRequest>();
        }

        public void OnUpdate(float deltaTime)
        {
            if (deltaTime <= 0f)
                return;

            _occupied.Clear();
            _expired.Clear();
            foreach (Entity entity in _turrets)
                UpdateTurret(entity, deltaTime);

            if (_expired.Count > 0)
                CancelShots(includeLaunched: false);

            foreach (Entity entity in _deployments)
                TryDeploy(entity, deltaTime);
        }

        private void UpdateTurret(Entity entity, float deltaTime)
        {
            ref TurretComponent turret = ref _turretStash.Get(entity);
            if (!_statusStash.Has(entity))
                return;

            turret.LifetimeRemaining -= deltaTime;
            Entity deployment = turret.Deployment;
            bool hasDeployment = !World.IsDisposed(deployment) && _deploymentStash.Has(deployment);
            if (turret.View == null || turret.LifetimeRemaining <= 0f || !OwnerCanDeploy(entity) || !hasDeployment ||
                _deploymentStash.Get(deployment).Generation != turret.Generation || !IsEnabled(deployment))
            {
                ExpireTurret(entity, ref turret, hasDeployment);
                return;
            }

            Vector3 position = _positionStash.Get(entity).Position;
            _occupied.Add(position);
            Entity target = _targetStash.Get(entity).Target;
            Vector3 direction = target != default && !World.IsDisposed(target) &&
                                _positionStash.Has(target) && !_deathStash.Has(target)
                ? _positionStash.Get(target).Position - turret.View.AimOrigin
                : turret.FallbackDirection;
            direction.z = 0f;
            turret.View.UpdateAim(direction, _statusStash.Get(entity).IsAttacking, deltaTime);
            turret.HasUpdatedAim = true;
        }

        private void ExpireTurret(Entity entity, ref TurretComponent turret, bool hasDeployment)
        {
            if (turret.View != null)
            {
                turret.View.gameObject.SetActive(false);
                Object.Destroy(turret.View.gameObject);
                turret.View = null;
            }

            if (hasDeployment)
                _deploymentStash.Get(turret.Deployment).ActiveCount--;

            _statusStash.Remove(entity);
            _targetStash.Remove(entity);
            _expired.Add(entity);
            _cleanupStash.Set(World.CreateEntity(), new CleanupRequest { Candidate = entity });
        }

        private void TryDeploy(Entity entity, float deltaTime)
        {
            AttackComponent attack = _attackStash.Get(entity);
            if (!IsEnabled(entity) || !OwnerCanDeploy(entity))
                return;

            attack.CurrentCooldown = Mathf.Max(0f, attack.CurrentCooldown - deltaTime);
            _attackStash.Get(entity).CurrentCooldown = attack.CurrentCooldown;

            ref TurretDeploymentComponent deployment = ref _deploymentStash.Get(entity);
            TurretWeaponConfig config = deployment.Config;
            if (attack.CurrentCooldown > 0f || deployment.ActiveCount >= Mathf.Max(1, config.MaxActiveTurrets))
                return;

            Vector3 ownerPosition = _positionStash.Get(_weaponStash.Get(entity).Owner).Position;
            if (!TryGetPlacement(ownerPosition, config.PlacementDistance, deployment.NextPlacement,
                    out Vector3 position, out int placement))
                return;

            TurretView view = TurretView.Create(config, _container, position);
            Entity deployed = _weapons.CreateDeployedTurret(entity, position);
            _turretStash.Set(deployed, new TurretComponent
            {
                Deployment = entity,
                Generation = deployment.Generation,
                LifetimeRemaining = Mathf.Max(0.01f, config.Lifetime),
                View = view
            });

            _occupied.Add(position);
            deployment.ActiveCount++;
            deployment.NextPlacement = (placement + 3) % TurretView.DirectionCount;
            _attackStash.Get(entity).CurrentCooldown = Mathf.Max(0.01f, config.DeploymentCooldown);
        }

        private bool TryGetPlacement(Vector3 origin, float distance, int preferred,
            out Vector3 position, out int placement)
        {
            for (int i = 0; i < TurretView.DirectionCount; i++)
            {
                placement = (preferred + i) % TurretView.DirectionCount;
                position = origin + TurretView.DirectionFor(placement) * distance;
                if ((_navigation == null || _navigation.IsWalkable(position)) && IsClear(position))
                    return true;
            }

            position = default;
            placement = default;
            return false;
        }

        private bool IsClear(Vector3 position)
        {
            foreach (Vector3 occupied in _occupied)
            {
                if ((position - occupied).sqrMagnitude < MinimumSpacing * MinimumSpacing)
                    return false;
            }

            return true;
        }

        private bool IsEnabled(Entity deployment) => _attackStash.Get(deployment).TargetTypes is { Length: > 0 };

        private bool OwnerCanDeploy(Entity weapon)
        {
            Entity owner = _weaponStash.Get(weapon).Owner;
            return owner != default && !World.IsDisposed(owner) && _positionStash.Has(owner) && !_deathStash.Has(owner);
        }

        private void CancelShots(bool includeLaunched)
        {
            foreach (Entity entity in _projectiles)
            {
                ref ProjectileComponent projectile = ref _projectileStash.Get(entity);
                if ((includeLaunched || !projectile.CollisionEnabled ||
                     projectile.WeaponType == WeaponType.ElectricTurret) && !projectile.CleanupRequested &&
                    _expired.Contains(projectile.Weapon))
                {
                    _projectileCleanup.Request(entity);
                    projectile.ViewActivated = false;
                }
            }
        }

        public void Dispose()
        {
            if (_turrets == null)
                return;

            _expired.Clear();
            foreach (Entity entity in _turrets)
            {
                if (World.IsDisposed(entity))
                    continue;

                if (_statusStash.Has(entity))
                {
                    ref TurretComponent turret = ref _turretStash.Get(entity);
                    bool hasDeployment = !World.IsDisposed(turret.Deployment) && _deploymentStash.Has(turret.Deployment);
                    ExpireTurret(entity, ref turret, hasDeployment);
                }

                _expired.Add(entity);
            }

            if (_expired.Count > 0)
                CancelShots(includeLaunched: true);
        }
    }
}
