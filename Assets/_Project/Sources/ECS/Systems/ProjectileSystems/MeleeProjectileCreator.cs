using ECS.AttackComponents;
using ECS.CommonComponents;
using ECS.ProjectileComponents;
using ECS.WeaponComponents;
using ProjectileConfigs;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.ProjectileSystems
{
    public sealed class MeleeProjectileCreator : IProjectileCreator
    {
        private readonly World _world;
        private readonly ProjectileConfig _config;

        private readonly Stash<WeaponComponent> _weaponStash;
        private readonly Stash<AttackComponent> _attackStash;
        private readonly Stash<PositionComponent> _positionStash;
        private readonly Stash<TargetComponent> _targetStash;
        private readonly Stash<ProjectileComponent> _projectileStash;
        private readonly Stash<ColliderComponent> _colliderStash;
        private readonly Stash<AttackStatusComponent> _attackStatusStash;
        private readonly Stash<ProjectileCreationComponent> _creationStash;

        public MeleeProjectileCreator(World world, ProjectileConfig config)
        {
            _world = world;
            _config = config;

            _weaponStash = world.GetStash<WeaponComponent>();
            _attackStash = world.GetStash<AttackComponent>();
            _positionStash = world.GetStash<PositionComponent>();
            _targetStash = world.GetStash<TargetComponent>();
            _projectileStash = world.GetStash<ProjectileComponent>();
            _colliderStash = world.GetStash<ColliderComponent>();
            _attackStatusStash = world.GetStash<AttackStatusComponent>();
            _creationStash = world.GetStash<ProjectileCreationComponent>();
        }

        public void Begin(Entity weapon)
        {
            if (!_attackStash.Has(weapon) ||
                !_attackStatusStash.Has(weapon) ||
                !_creationStash.Has(weapon))
                return;

            ref AttackComponent attack = ref _attackStash.Get(weapon);
            ref AttackStatusComponent attackStatus = ref _attackStatusStash.Get(weapon);
            ref ProjectileCreationComponent creation = ref _creationStash.Get(weapon);

            creation.ElapsedTime = 0f;
            creation.RemainingCount = 0;
            creation.IsInitialized = true;

            attackStatus.CreationFinished = false;
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

            Entity projectile = _world.CreateEntity();
            Vector3 direction = GetDirection(weapon);

            _projectileStash.Set(projectile, new ProjectileComponent
            {
                Weapon = weapon,
                WeaponType = weaponComponent.Type,
                Type = _config.Type,
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
                Position = _positionStash.Get(weapon).Position + direction * attack.DistanceFromSource
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

            return projectile;
        }

        private Vector3 GetDirection(Entity weapon)
        {
            if (!_targetStash.Has(weapon))
                return Vector3.right;

            Entity target = _targetStash.Get(weapon).Target;

            if (target == default || !_positionStash.Has(target))
                return Vector3.right;

            Vector3 sourcePosition = _positionStash.Get(weapon).Position;
            Vector3 targetPosition = _positionStash.Get(target).Position;
            Vector3 direction = targetPosition - sourcePosition;
            direction.z = 0f;

            if (direction.sqrMagnitude <= 0f)
                return Vector3.right;

            return direction.normalized;
        }
    }
}