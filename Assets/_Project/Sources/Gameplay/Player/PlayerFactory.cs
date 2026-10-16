using System.Collections.Generic;
using Configs;
using ECS.CommonComponents;
using ECS.PlayerComponents;
using ECS.PlayerSystems;
using ECS.ViewComponents;
using ECS.WeaponComponents;
using ECS.WeaponSystems;
using Gameplay.View;
using PlayerConfigs;
using Scellecs.Morpeh;
using UniRx;
using UnityEngine;
using UpgradeConfigs;
using static UnityEngine.EventSystems.EventTrigger;

namespace Gameplay.Player
{
    public sealed class PlayerFactory
    {
        private const int InitialFirstLevelUpXP = 100;

        private readonly World _world;
        private readonly WeaponFactory _weaponFactory;

        private readonly Stash<PlayerTag> _playerTagStash;
        private readonly Stash<StatsComponent> _statsStash;
        private readonly Stash<PlayerStatsComponent> _playerStatsStash;
        private readonly Stash<HealthComponent> _healthStash;
        private readonly Stash<MovementComponent> _movementStash;
        private readonly Stash<PlayerInputComponent> _inputStash;
        private readonly Stash<PositionComponent> _positionStash;
        private readonly Stash<ColliderComponent> _colliderStash;

        private readonly Stash<PlayerExperienceComponent> _xpStash;
        private readonly Stash<AddExperienceEvent> _addXpStash;

        private readonly Stash<LooterComponent> _looterStash;
        private readonly Stash<PlayerInventoryComponent> _inventoryStash;

        private readonly Stash<ViewComponent> _viewStash;
        private readonly Stash<AnimatedCreatureComponent> _animatedCreatureStash;
        private readonly Stash<PlayerViewComponent> _playerViewStash;

        private List<NewWeaponUpgradeConfig> _weaponUpgradeConfigs;
        private List<NewTurretUpgradeConfig> _turretUpgradeConfigs;

        public PlayerFactory(
            World world,
            WeaponFactory weaponFactory,
            List<NewWeaponUpgradeConfig> weaponUpgradeConfigs,
            List<NewTurretUpgradeConfig> turretUpgradeConfigs)
        {
            _world = world;
            _weaponFactory = weaponFactory;
            _weaponUpgradeConfigs = weaponUpgradeConfigs;
            _turretUpgradeConfigs = turretUpgradeConfigs;

            _playerTagStash = world.GetStash<PlayerTag>();
            _statsStash = world.GetStash<StatsComponent>();
            _playerStatsStash = world.GetStash<PlayerStatsComponent>();
            _healthStash = world.GetStash<HealthComponent>();
            _movementStash = world.GetStash<MovementComponent>();
            _inputStash = world.GetStash<PlayerInputComponent>();
            _positionStash = world.GetStash<PositionComponent>();
            _colliderStash = world.GetStash<ColliderComponent>();

            _viewStash = world.GetStash<ViewComponent>();
            _animatedCreatureStash = world.GetStash<AnimatedCreatureComponent>();
            _playerViewStash = world.GetStash<PlayerViewComponent>();

            _xpStash = world.GetStash<PlayerExperienceComponent>();
            _addXpStash = world.GetStash<AddExperienceEvent>();

            _looterStash = world.GetStash<LooterComponent>();
            _inventoryStash = world.GetStash<PlayerInventoryComponent>();
        }

        public GameObject Create(PlayerConfig config, Vector3 position)
        {
            Entity entity = _world.CreateEntity();

            CreateStats(entity, config);
            CreateGameplayComponents(entity, config, position);

            GameObject playerObject = CreateView(entity, config, position);

            if (playerObject == null)
                return null;

            CreateWeapon(entity, config);

            return playerObject;
        }

        private void CreateStats(Entity entity, PlayerConfig config)
        {
            BaseStats baseStats = config.Stats;

            StatsComponent stats = new StatsComponent
            {
                CreatureType = config.CreatureType,
                MaxHealth = baseStats.MaxHealth,
                HealthRegeneration = baseStats.HealthRegeneration,
                Damage = baseStats.Damage,
                Luck = baseStats.Luck,
                Defence = baseStats.Defence,
                Speed = baseStats.Speed,
            };

            _statsStash.Set(entity, stats);

            _playerStatsStash.Set(
                entity,
                new PlayerStatsComponent
                {
                    GatheringRadius = config.GatheringRadius,
                    EvasionChance = config.EvasionChance,
                });

            _healthStash.Set(entity, new HealthComponent
            {
                Current = stats.MaxHealth,
            });
        }

        private void CreateGameplayComponents(Entity entity, PlayerConfig config, Vector3 position)
        {
            _playerTagStash.Set(entity, new PlayerTag());

            _movementStash.Set(entity, new MovementComponent
            {
                Direction = Vector3.zero,
            });

            _inputStash.Set(entity, new PlayerInputComponent());

            _positionStash.Set(entity, new PositionComponent
            {
                Position = position,
            });

            _colliderStash.Set(
                entity,
                new ColliderComponent
                {
                    Type = config.Collider.Type,
                    Radius = config.Collider.Radius,
                    Length = config.Collider.Length,
                    Size = config.Collider.Size,
                    Angle = 0f,
                    CapsuleOrientation = config.Collider.CapsuleOrientation,
                });

            _xpStash.Set(entity, new PlayerExperienceComponent
            {
                CurrentXP = new ReactiveProperty<int>(0),
                NextLevelXP = new ReactiveProperty<int>(InitialFirstLevelUpXP),
                CurrentLevel = new ReactiveProperty<int>(1),
            });
            _addXpStash.Set(entity, new AddExperienceEvent { Amount = 0 });
            _looterStash.Set(entity, new LooterComponent { PickupRadius = config.GatheringRadius });
            InitializeInventory(config, entity);
        }

        private void InitializeInventory(PlayerConfig config, Entity entity)
        {
            WeaponType type = config.WeaponConfig.Type;
            Dictionary<string, int> initialUpgrades = new Dictionary<string, int>();
            int currentTurrets = 0;
            int currentWeapons = 0;
            var initialWeapon = _weaponUpgradeConfigs.Find((item) => item.Type == type);
            var initialTurret = _turretUpgradeConfigs.Find((item) => item.Type == type);

            if (initialWeapon != null)
            {
                initialUpgrades.Add(initialWeapon.UpgradeId, 1);
                currentWeapons++;
            }

            if (initialTurret != null)
            {
                initialUpgrades.Add(initialTurret.UpgradeId, 1);
                currentTurrets++;
            }

            _inventoryStash.Set(entity, new PlayerInventoryComponent
            {
                ActiveUpgrades = initialUpgrades,
                CurrentTurretsAmount = currentTurrets,
                MaxTurrets = config.MaxTurrets,
                CurrentWeaponsAmount = currentWeapons,
                MaxWeapons = config.MaxWeapons,
                MaxSkills = config.MaxSkills,
            });
        }

        private GameObject CreateView(Entity entity, PlayerConfig config, Vector3 position)
        {
            GameObject playerObject = Object.Instantiate(config.Prefab);
            playerObject.transform.position = position;

            AnimatedCreature view = playerObject.GetComponent<AnimatedCreature>();

            if (view == null)
            {
                Object.Destroy(playerObject);
                _world.RemoveEntity(entity);

                return null;
            }

            view.SetEntity(entity);

            _viewStash.Set(entity, new ViewComponent
            {
                View = view
            });

            _animatedCreatureStash.Set(entity, new AnimatedCreatureComponent
            {
                View = view
            });

            _playerViewStash.Set(entity, new PlayerViewComponent
            {
                View = view
            });

            return playerObject;
        }

        private void CreateWeapon(Entity entity, PlayerConfig config)
        {
            if (config.WeaponConfig == null)
                return;

            _weaponFactory.Create(config.WeaponConfig, entity);
        }
    }
}