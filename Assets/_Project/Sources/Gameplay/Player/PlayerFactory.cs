using ECS.PlayerComponents;
using ECS.PlayerSystems;
using ECS.ViewComponents;
using ECS.WeaponSystems;
using Gameplay.Creatures;
using Gameplay.View;
using PlayerConfigs;
using Scellecs.Morpeh;
using UniRx;
using UnityEngine;

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

        private readonly Stash<ViewComponent> _viewStash;
        private readonly Stash<AnimatedCreatureComponent> _animatedCreatureStash;
        private readonly Stash<PlayerViewComponent> _playerViewStash;

        public PlayerFactory(World world, WeaponFactory weaponFactory)
        {
            _world = world;
            _creatureFactory = new CreatureFactory(world, weaponFactory);

            _playerTagStash = world.GetStash<PlayerTag>();
            _playerStatsStash = world.GetStash<PlayerStatsComponent>();
            _inputStash = world.GetStash<PlayerInputComponent>();
            _xpStash = world.GetStash<PlayerExperienceComponent>();
            _addXpStash = world.GetStash<AddExperienceEvent>();
            _looterStash = world.GetStash<LooterComponent>();

            _viewStash = world.GetStash<ViewComponent>();
            _animatedCreatureStash = world.GetStash<AnimatedCreatureComponent>();
            _playerViewStash = world.GetStash<PlayerViewComponent>();
        }

        public GameObject Create(PlayerConfig config, Vector3 position)
        {
            if (config == null || config.Prefab == null)
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
                    MaxWeapons = config.MaxWeapons,
                    GatheringRadius = config.GatheringRadius,
                    EvasionChance = config.EvasionChance
                });

            _inputStash.Set(entity, new PlayerInputComponent());

            _xpStash.Set(entity, new PlayerExperienceComponent
            {
                CurrentXP = new ReactiveProperty<int>(0),
                NextLevelXP = new ReactiveProperty<int>(
                    InitialFirstLevelUpXP),
                CurrentLevel = new ReactiveProperty<int>(1)
            });

            _addXpStash.Set(entity, new AddExperienceEvent
            {
                Amount = 0
            });

            _looterStash.Set(entity, new LooterComponent
            {
                PickupRadius = config.GatheringRadius
            });
        }

        private GameObject CreateView(
            Entity entity,
            PlayerConfig config,
            Vector3 position)
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
                View = view
            });

            _animatedCreatureStash.Set(
                entity,
                new AnimatedCreatureComponent
                {
                    View = view
                });

            _playerViewStash.Set(
                entity,
                new PlayerViewComponent
                {
                    View = view
                });

            return playerObject;
        }
    }
}