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
        private readonly Stash<ColliderComponent> _colliderStash;

        public CreatureFactory(
            World world,
            WeaponFactory weaponFactory)
        {
            _weaponFactory = weaponFactory;

            _statsStash = world.GetStash<StatsComponent>();
            _healthStash = world.GetStash<HealthComponent>();
            _movementStash = world.GetStash<MovementComponent>();
            _positionStash = world.GetStash<PositionComponent>();
            _colliderStash = world.GetStash<ColliderComponent>();
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

            _colliderStash.Set(entity, new ColliderComponent
            {
                Type = config.Collider.Type,
                Radius = config.Collider.Radius,
                Length = config.Collider.Length,
                Size = config.Collider.Size,
                Angle = 0f,
                CapsuleOrientation = config.Collider.CapsuleOrientation
            });
        }
    }
}