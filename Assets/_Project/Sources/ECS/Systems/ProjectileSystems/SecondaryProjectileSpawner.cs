using Core.Game;
using ECS.CommonComponents;
using ECS.ProjectileComponents;
using ECS.WeaponComponents;
using Gameplay.Spatial;
using Gameplay.View;
using ProjectileConfigs;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.ProjectileSystems
{
    public sealed class SecondaryProjectileSpawner
    {
        private readonly ProjectileFactory _factory;
        private readonly ObjectPool<AnimatedView> _armageddonSubmunitionViews;
        private readonly ObjectPool<AnimatedView> _shrapnelFragmentViews;
        private readonly ObjectPool<AnimatedView> _shrapnelImpactViews;
        private readonly Stash<WeaponComponent> _weaponStash;
        private readonly Stash<PositionComponent> _positionStash;
        private readonly Stash<ProjectileComponent> _projectileStash;
        private readonly Stash<HitBoxComponent> _hitBoxStash;
        private readonly Stash<RotationComponent> _rotationStash;
        private readonly Stash<ProjectileViewComponent> _projectileViewStash;
        private readonly Stash<ShrapnelFragmentComponent> _shrapnelFragmentStash;

        public SecondaryProjectileSpawner(World world, ProjectileFactory factory)
        {
            _factory = factory;
            _weaponStash = world.GetStash<WeaponComponent>();
            _positionStash = world.GetStash<PositionComponent>();
            _projectileStash = world.GetStash<ProjectileComponent>();
            _hitBoxStash = world.GetStash<HitBoxComponent>();
            _rotationStash = world.GetStash<RotationComponent>();
            _projectileViewStash = world.GetStash<ProjectileViewComponent>();
            _shrapnelFragmentStash = world.GetStash<ShrapnelFragmentComponent>();

            if (factory.TryGetDefinition(WeaponType.Armageddon6, out ProjectileFactory.Definition armageddon) &&
                armageddon.Config is ArmageddonProjectileConfig cluster)
            {
                _armageddonSubmunitionViews = factory.CreateViewPool(cluster.SubmunitionPrefab, cluster.PoolCapacity);
            }

            if (factory.TryGetDefinition(WeaponType.ShrapnelStorm, out ProjectileFactory.Definition shrapnel) &&
                shrapnel.Config is ShrapnelStormProjectileConfig storm)
            {
                _shrapnelFragmentViews = factory.CreateViewPool(storm.FragmentPrefab, storm.PoolCapacity);
                _shrapnelImpactViews = factory.CreateViewPool(storm.ImpactPrefab, storm.PoolCapacity);
            }
        }

        internal void SpawnArmageddonSubmunitions(
            Entity grenade,
            GrenadeFlightComponent flight,
            ColliderComponent explosionCollider)
        {
            const int count = 3;
            const float scale = 0.5f;
            ProjectileComponent data = _projectileStash.Get(grenade);
            if (!_weaponStash.Has(data.Weapon) ||
                !_factory.TryGetDefinition(data.WeaponType, out ProjectileFactory.Definition definition) ||
                definition.Config is not ArmageddonProjectileConfig config ||
                definition.Creator is not GrenadeLauncherProjectileCreator creator)
            {
                return;
            }

            Vector3 start = flight.LandingPosition;
            Vector3 forward = data.Direction;
            ColliderComponent collider = ColliderScaler.Scale(explosionCollider, scale);
            data.CurrentLifetime = 0f;
            data.Lifetime = 0f;
            data.Damage *= config.SubmunitionDamageMultiplier;
            data.CollisionEnabled = false;
            data.AttackCompletionReported = true;

            for (int i = 0; i < count; i++)
            {
                data.Direction = Quaternion.Euler(0f, 0f, i * (360f / count)) * forward;
                Entity entity = _factory.Create(data, start, collider,
                    flight.ClusterProjectileScale * scale, _armageddonSubmunitionViews);
                creator.Launch(entity, start, start + data.Direction * config.SubmunitionDistance,
                    config.ArcOffset * scale);
            }
        }

        internal bool TryLaunchArmageddonGrenade(Entity grenade)
        {
            return _factory.TryGetDefinition(WeaponType.Armageddon6, out ProjectileFactory.Definition definition) &&
                   definition.Creator is GrenadeLauncherProjectileCreator creator &&
                   creator.TryLaunchClusterGrenade(grenade);
        }

        internal void SpawnShrapnelImpact(Entity charge, Entity hitTarget)
        {
            const int fragmentCount = 4;
            const float fragmentLifetime = 2f;
            ProjectileComponent parent = _projectileStash.Get(charge);
            if (!_factory.TryGetDefinition(parent.WeaponType, out ProjectileFactory.Definition definition) ||
                definition.Config is not ShrapnelStormProjectileConfig config)
                return;

            Vector3 position = _positionStash.Get(charge).Position;
            position.z = 0f;
            float parentRadius = _hitBoxStash.Get(charge).Collider.Radius;
            float scale = config.Collider.Radius > 0f ? parentRadius / config.Collider.Radius : 1f;
            if (_shrapnelImpactViews != null)
            {
                var impact = new ProjectileComponent
                {
                    Weapon = parent.Weapon,
                    WeaponType = parent.WeaponType,
                    Lifetime = config.ImpactDuration,
                    AttackCompletionReported = true,
                    ViewActivated = true,
                };
                CreateShrapnelProjectile(impact, position, default, scale, _shrapnelImpactViews);
            }

            var fragment = new ProjectileComponent
            {
                Weapon = parent.Weapon,
                WeaponType = parent.WeaponType,
                Speed = parent.Speed * 0.8f,
                Lifetime = fragmentLifetime,
                Damage = parent.Damage / fragmentCount,
                TargetTypes = parent.TargetTypes,
                HitInterval = 0.1f,
                HitPolicy = HitPolicyType.PeriodicPerTarget,
                CollisionEnabled = true,
                AttackCompletionReported = true,
                ViewActivated = true,
            };
            var collider = new ColliderComponent { Type = ColliderType.Circle, Radius = parentRadius * 0.9f };
            for (int i = 0; i < fragmentCount; i++)
            {
                fragment.Direction = Quaternion.Euler(0f, 0f, 45f + i * (360f / fragmentCount)) * parent.Direction;
                Entity entity = CreateShrapnelProjectile(fragment, position, collider, scale, _shrapnelFragmentViews);
                _shrapnelFragmentStash.Set(entity, new ShrapnelFragmentComponent
                {
                    SourceTarget = hitTarget,
                    PreviousPosition = position,
                });
            }
        }

        private Entity CreateShrapnelProjectile(
            ProjectileComponent data,
            Vector3 position,
            ColliderComponent collider,
            float scale,
            ObjectPool<AnimatedView> pool)
        {
            Entity entity = _factory.Create(data, position, collider, scale, pool);
            float angle = Mathf.Atan2(data.Direction.y, data.Direction.x) * Mathf.Rad2Deg;
            _rotationStash.Set(entity, new RotationComponent { Angle = angle });
            if (_projectileViewStash.Has(entity))
            {
                _projectileViewStash.Get(entity).View.transform.SetPositionAndRotation(
                    position, Quaternion.Euler(0f, 0f, angle));
            }

            return entity;
        }
    }
}
