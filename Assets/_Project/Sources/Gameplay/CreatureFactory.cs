using Configs;
using ECS.CommonComponents;
using ECS.WeaponSystems;
using Scellecs.Morpeh;
using UnityEngine;
using WeaponConfigs;

namespace Gameplay.Creatures
{
    public sealed class CreatureFactory
    {
        private readonly WeaponFactory _weaponFactory;

        private readonly Stash<StatsComponent> _statsStash;
        private readonly Stash<HealthComponent> _healthStash;
        private readonly Stash<MovementComponent> _movementStash;
        private readonly Stash<PositionComponent> _positionStash;
        private readonly Stash<HitBoxComponent> _hitBoxStash;
        private readonly Stash<NavigationBoxComponent> _navigationBoxStash;

        public CreatureFactory(
            World world,
            WeaponFactory weaponFactory)
        {
            _weaponFactory = weaponFactory;

            _statsStash = world.GetStash<StatsComponent>();
            _healthStash = world.GetStash<HealthComponent>();
            _movementStash = world.GetStash<MovementComponent>();
            _positionStash = world.GetStash<PositionComponent>();
            _hitBoxStash = world.GetStash<HitBoxComponent>();
            _navigationBoxStash = world.GetStash<NavigationBoxComponent>();
        }

        public void CreateCommonComponents(
            Entity entity,
            CreatureConfig config,
            Vector3 position)
        {
            CreateStats(entity, config);
            CreateGameplayComponents(entity, config, position);
        }

        public void CreateWeapons(Entity entity, CreatureConfig config)
        {
            if (config.WeaponConfig == null)
                return;

            foreach (WeaponConfig weaponConfig in config.WeaponConfig)
            {
                if (weaponConfig == null)
                    continue;

                _weaponFactory.Create(weaponConfig, entity);
            }
        }

        private void CreateStats(Entity entity, CreatureConfig config)
        {
            BaseStats baseStats = config.Stats;

            _statsStash.Set(entity, new StatsComponent
            {
                CreatureType = config.CreatureType,
                MaxHealth = baseStats.MaxHealth,
                HealthRegeneration = baseStats.HealthRegeneration,
                Damage = baseStats.Damage,
                Luck = baseStats.Luck,
                Defence = baseStats.Defence,
                Speed = baseStats.Speed
            });

            _healthStash.Set(entity, new HealthComponent
            {
                Current = baseStats.MaxHealth
            });
        }

        private void CreateGameplayComponents(
            Entity entity,
            CreatureConfig config,
            Vector3 position)
        {
            _movementStash.Set(entity, new MovementComponent
            {
                Direction = Vector3.zero
            });

            _positionStash.Set(entity, new PositionComponent
            {
                Position = position
            });

            ColliderConfig hitBoxConfig = config.HitBox;
            ColliderConfig navigationBoxConfig =
                config.NavigationBox ?? hitBoxConfig;

            _hitBoxStash.Set(entity, new HitBoxComponent
            {
                Collider = CreateCollider(hitBoxConfig)
            });

            _navigationBoxStash.Set(entity, new NavigationBoxComponent
            {
                Collider = CreateCollider(navigationBoxConfig)
            });
        }

        private static ColliderComponent CreateCollider(ColliderConfig config)
        {
            return new ColliderComponent
            {
                Type = config.Type,
                Radius = config.Radius,
                Length = config.Length,
                Size = config.Size,
                Center = config.Center,
                Angle = 0f,
                CapsuleOrientation = config.CapsuleOrientation
            };
        }
    }
}
