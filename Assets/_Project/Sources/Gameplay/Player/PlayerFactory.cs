using System.Collections.Generic;
using Configs;
using ECS.CommonComponents;
using ECS.PlayerComponents;
using ECS.PlayerSystems;
using ECS.ViewComponents;
using ECS.WeaponComponents;
using ECS.WeaponSystems;
using Gameplay.Creatures;
using Gameplay.View;
using PlayerConfigs;
using Scellecs.Morpeh;
using UniRx;
using UnityEngine;
using UpgradeConfigs;
using WeaponConfigs;
using static UnityEngine.EventSystems.EventTrigger;

namespace Gameplay.Player
{
    public sealed class PlayerFactory
    {
        private const int InitialFirstLevelUpXP = 100;

        private readonly World _world;
        private readonly CreatureFactory _creatureFactory;

        private readonly Stash<PlayerTag> _playerTagStash;
        private readonly Stash<PlayerStatsComponent> _playerStatsStash;
        private readonly Stash<PlayerInputComponent> _inputStash;
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
            _creatureFactory = new CreatureFactory(world, weaponFactory);
            _weaponUpgradeConfigs = weaponUpgradeConfigs;
            _turretUpgradeConfigs = turretUpgradeConfigs;

            _playerTagStash = world.GetStash<PlayerTag>();
            _playerStatsStash = world.GetStash<PlayerStatsComponent>();
            _inputStash = world.GetStash<PlayerInputComponent>();
            _xpStash = world.GetStash<PlayerExperienceComponent>();
            _addXpStash = world.GetStash<AddExperienceEvent>();
            _looterStash = world.GetStash<LooterComponent>();

            _viewStash = world.GetStash<ViewComponent>();
            _animatedCreatureStash = world.GetStash<AnimatedCreatureComponent>();
            _playerViewStash = world.GetStash<PlayerViewComponent>();
            _inventoryStash = world.GetStash<PlayerInventoryComponent>();
        }

        public GameObject Create(PlayerConfig config, Vector3 position)
        {
            if (config == null ||
                config.Prefab == null ||
                config.HitBox == null)
                return null;

            Entity entity = _world.CreateEntity();

            _creatureFactory.CreateCommonComponents(
                entity,
                config,
                position);

            CreatePlayerComponents(entity, config);

            GameObject playerObject = CreateView(entity, config, position);

            if (playerObject == null)
            {
                _world.RemoveEntity(entity);
                return null;
            }

            _creatureFactory.CreateWeapons(entity, config);

            return playerObject;
        }

        private void CreatePlayerComponents(
            Entity entity,
            PlayerConfig config)
        {
            _playerTagStash.Set(entity, new PlayerTag());

            _playerStatsStash.Set(
                entity,
                new PlayerStatsComponent
                {
                    GatheringRadius = config.GatheringRadius,
                    EvasionChance = config.EvasionChance,
                });

            _inputStash.Set(entity, new PlayerInputComponent());

            _xpStash.Set(entity, new PlayerExperienceComponent
            {
                CurrentXP = new ReactiveProperty<int>(0),
                NextLevelXP = new ReactiveProperty<int>(
                    InitialFirstLevelUpXP),
                CurrentLevel = new ReactiveProperty<int>(1),
            });

            _addXpStash.Set(entity, new AddExperienceEvent
            {
                Amount = 0,
            });

            _looterStash.Set(entity, new LooterComponent
            {
                PickupRadius = config.GatheringRadius,
            });
            InitializeInventory(config, entity);
        }

        private void InitializeInventory(PlayerConfig config, Entity entity)
        {
            WeaponConfig[] weapons = config.WeaponConfig;
            Dictionary<string, int> initialUpgrades = new Dictionary<string, int>();
            int currentTurrets = 0;
            int currentWeapons = 0;

            foreach (var weapon in weapons)
            {
                if (weapon == null)
                {
                    Debug.LogError("Player config contains an empty weapon reference.");
                    continue;
                }

                var initialWeapon = _weaponUpgradeConfigs.Find((item) => item.Type == weapon.Type);

                currentWeapons++;

                if (initialWeapon == null)
                {
                    Debug.LogError(
                        $"No base weapon upgrade config found for player weapon type {weapon.Type}. " +
                        "The player will still spawn, but this weapon will not be registered in the upgrade inventory.");
                    continue;
                }

                initialUpgrades[initialWeapon.UpgradeId] = 1;
            }

            _inventoryStash.Set(entity, new PlayerInventoryComponent
            {
                ActiveUpgrades = new ReactiveDictionary<string, int>(initialUpgrades),
                CurrentTurretsAmount = currentTurrets,
                MaxTurrets = new ReactiveProperty<int>(config.MaxTurrets),
                CurrentWeaponsAmount = currentWeapons,
                MaxWeapons = new ReactiveProperty<int>(config.MaxWeapons),
                MaxItems = new ReactiveProperty<int>(config.MaxSkills),
            });
        }

        private GameObject CreateView(Entity entity, PlayerConfig config, Vector3 position)
        {
            GameObject playerObject = Object.Instantiate(config.Prefab);
            playerObject.transform.position = position;

            AnimatedCreature view =
                playerObject.GetComponent<AnimatedCreature>();

            if (view == null)
            {
                Object.Destroy(playerObject);
                return null;
            }

            view.SetEntity(entity);

            _viewStash.Set(entity, new ViewComponent
            {
                View = view,
            });

            _animatedCreatureStash.Set(
                entity,
                new AnimatedCreatureComponent
                {
                    View = view,
                });

            _playerViewStash.Set(
                entity,
                new PlayerViewComponent
                {
                    View = view,
                });

            return playerObject;
        }
    }
}
