using Core.Game;
using ECS.AttackComponents;
using ECS.CommonComponents;
using ECS.ProjectileComponents;
using ECS.WeaponComponents;
using Gameplay.View;
using ProjectileConfigs;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.ProjectileSystems
{
    public sealed class AxeProjectileCreator : IProjectileCreator
    {
        private readonly World _world;
        private readonly ArcProjectileConfig _config;

        private readonly Stash<WeaponComponent> _weaponStash;
        private readonly Stash<AttackComponent> _attackStash;
        private readonly Stash<PositionComponent> _positionStash;
        private readonly Stash<TargetComponent> _targetStash;
        private readonly Stash<ProjectileComponent> _projectileStash;
        private readonly Stash<RotationComponent> _rotationStash;
        private readonly Stash<ArcTrajectoryComponent> _arcTrajectoryStash;
        private readonly Stash<ColliderComponent> _colliderStash;
        private readonly Stash<ColliderPositionComponent> _colliderPositionStash;
        private readonly Stash<AttackStatusComponent> _attackStatusStash;
        private readonly Stash<ProjectileCreationComponent> _creationStash;
        private readonly Stash<ProjectileViewComponent> _projectileViewStash;
 
        private readonly ObjectPool<AnimatedView> _axeViews;

        public AxeProjectileCreator(World world, ProjectileConfig config, Transform container)
        {
            _world = world;
            _config = config as ArcProjectileConfig;

            _weaponStash = world.GetStash<WeaponComponent>();
            _attackStash = world.GetStash<AttackComponent>();
            _positionStash = world.GetStash<PositionComponent>();
            _targetStash = world.GetStash<TargetComponent>();
            _projectileStash = world.GetStash<ProjectileComponent>();
            _rotationStash = world.GetStash<RotationComponent>();
            _arcTrajectoryStash = world.GetStash<ArcTrajectoryComponent>();
            _colliderStash = world.GetStash<ColliderComponent>();
            _colliderPositionStash = world.GetStash<ColliderPositionComponent>();
            _attackStatusStash = world.GetStash<AttackStatusComponent>();
            _creationStash = world.GetStash<ProjectileCreationComponent>();
            _projectileViewStash = world.GetStash<ProjectileViewComponent>();

            _axeViews = new ObjectPool<AnimatedView>(
                config.Prefab,
                config.PoolCapacity,
                container);
        }

        public void Begin(Entity weapon)
        {
            if (!_attackStatusStash.Has(weapon) || !_creationStash.Has(weapon))
                return;

            ref ProjectileCreationComponent creation = ref _creationStash.Get(weapon);
            ref AttackStatusComponent attackStatus = ref _attackStatusStash.Get(weapon);

            creation.ElapsedTime = 0f;
            creation.RemainingCount = 0;
            creation.IsInitialized = true;

            Create(weapon);

            creation.IsInitialized = false;
            attackStatus.CreationFinished = true;
        }

        public void Update(Entity weapon, float deltaTime) { }

        public void Cancel(Entity weapon)
        {
            if (_creationStash.Has(weapon))
            {
                ref ProjectileCreationComponent creation = ref _creationStash.Get(weapon);

                creation.ElapsedTime = 0f;
                creation.RemainingCount = 0;
                creation.IsInitialized = false;
            }

            if (_attackStatusStash.Has(weapon))
                _attackStatusStash.Get(weapon).CreationFinished = true;
        }

        private Entity Create(Entity weapon)
        {
            ref WeaponComponent weaponComponent = ref _weaponStash.Get(weapon);
            ref AttackComponent attack = ref _attackStash.Get(weapon);
            ref PositionComponent weaponPosition = ref _positionStash.Get(weapon);

            Entity projectile = _world.CreateEntity();
            Vector3 direction = GetDirection(weapon);
            Vector3 colliderOffset = _config.ColliderOffset;

            _projectileStash.Set(projectile, new ProjectileComponent
            {
                Weapon = weapon,
                WeaponType = weaponComponent.Type,
                Type = ProjectileType.Axe,
                Speed = attack.ProjectileSpeed,
                Lifetime = attack.ProjectileLifetime,
                CurrentLifetime = 0f,
                Damage = attack.Damage,
                Pierce = attack.Pierce,
                Direction = direction,
                TargetType = attack.TargetType,
                CleanupRequested = false
            });

            _positionStash.Set(projectile, new PositionComponent
            {
                Position = weaponPosition.Position + direction * attack.DistanceFromSource
            });

            _rotationStash.Set(projectile, new RotationComponent
            {
                Angle = 0f
            });

            _arcTrajectoryStash.Set(projectile, new ArcTrajectoryComponent
            {
                Direction = direction,
                ColliderOffset = colliderOffset,
                DistanceFromSource = attack.DistanceFromSource,
                Radius = _config.TrajectoryRadius,
                StartAngle = _config.StartAngle,
                EndAngle = _config.EndAngle,
                StartRotationAngle = -45f,
                EndRotationAngle = 45f,
                Duration = attack.AttackDuration,
                CurrentTime = 0f
            });

            _colliderStash.Set(projectile, new ColliderComponent
            {
                Type = _config.Collider.Type,
                Radius = _config.Collider.Radius,
                Length = _config.Collider.Length,
                Size = _config.Collider.Size,
                Angle = 0f, 
                CapsuleOrientation = _config.Collider.CapsuleOrientation,
            });

            _colliderPositionStash.Set(projectile, new ColliderPositionComponent
            {
                CenterPosition = _positionStash.Get(projectile).Position + colliderOffset
            });

            AnimatedView view = _axeViews.GetElement();
   
            if (view != null)
            {
                view.SetEntity(projectile);

                _projectileViewStash.Set(projectile, new ProjectileViewComponent
                {
                    View = view
                });

                view.SetActive(true);
            }

            return projectile;
        }

        private Vector3 GetDirection(Entity weapon)
        {
            if (!_targetStash.Has(weapon))
                return Vector3.right;

            Entity target = _targetStash.Get(weapon).Target;

            if (target == default || !_positionStash.Has(target))
                return Vector3.right;

            Vector3 weaponPosition = _positionStash.Get(weapon).Position;
            Vector3 targetPosition = _positionStash.Get(target).Position;
            Vector3 direction = targetPosition - weaponPosition;
            direction.z = 0f;

            if (direction.sqrMagnitude <= 0f)
                return Vector3.right;

            return direction.normalized;
        }
    }
}