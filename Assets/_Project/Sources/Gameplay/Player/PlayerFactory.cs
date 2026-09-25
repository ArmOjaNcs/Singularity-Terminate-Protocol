using ECS.CommonComponents;
using ECS.PlayerComponents;
using PlayerConfigs;
using Scellecs.Morpeh;
using UnityEngine;

namespace Gameplay.Player
{
    public sealed class PlayerFactory
    {
        private readonly World _world;

        private readonly Stash<PlayerTag> _playerTagStash;
        private readonly Stash<PlayerStatsComponent> _playerStatsStash;
        private readonly Stash<HealthComponent> _healthStash;
        private readonly Stash<MovementComponent> _movementStash;
        private readonly Stash<PositionComponent> _positionStash;
        private readonly Stash<NavigationRadiusComponent> _navigationRadiusStash;
        private readonly Stash<ViewComponent> _viewStash;

        public PlayerFactory(World world)
        {
            _world = world;

            _playerTagStash = world.GetStash<PlayerTag>();
            _playerStatsStash = world.GetStash<PlayerStatsComponent>();
            _healthStash = world.GetStash<HealthComponent>();
            _movementStash = world.GetStash<MovementComponent>();
            _positionStash = world.GetStash<PositionComponent>();
            _navigationRadiusStash = world.GetStash<NavigationRadiusComponent>();
            _viewStash = world.GetStash<ViewComponent>();
        }

        public Entity Create(PlayerConfig config, Vector3 position)
        {
            Entity entity = _world.CreateEntity();
            Entity entity2 = _world.CreateEntity();

            StatsComponent stats = new StatsComponent
            {
                MaxHealth = config.Stats.BaseStats.MaxHealth,
                HealthRegeneration = config.Stats.BaseStats.HealthRegeneration,
                Damage = config.Stats.BaseStats.Damage,
                Luck = config.Stats.BaseStats.Luck,
                Defence = config.Stats.BaseStats.Defence,
                Speed = config.Stats.BaseStats.Speed
            };

            _playerTagStash.Set(entity, new PlayerTag());

            _playerStatsStash.Set(entity, new PlayerStatsComponent
            {
                Stats = stats,
                MaxWeapons = config.Stats.MaxWeapons,
                GatheringRadius = config.Stats.GatheringRadius,
                EvasionChance = config.Stats.EvasionChance
            });

            _healthStash.Set(entity, new HealthComponent
            {
                Current = stats.MaxHealth
            }); 
            
            _healthStash.Set(entity2, new HealthComponent
            {
                Current = stats.MaxHealth
            });

            _movementStash.Set(entity, new MovementComponent
            {
                Direction = Vector3.zero
            });

            _positionStash.Set(entity, new PositionComponent
            {
                Position = position
            });

            _navigationRadiusStash.Set(entity, new NavigationRadiusComponent
            {
                Radius = config.Stats.BaseStats.NavigationRadius
            });

            GameObject playerObject = Object.Instantiate(config.Prefab);
            playerObject.transform.position = position;

            PlayerView view = playerObject.GetComponent<PlayerView>();

            if (view == null)
            {
                Debug.LogError(
                    $"Player prefab '{config.name}' does not contain PlayerView."
                );

                Object.Destroy(playerObject);
                _world.RemoveEntity(entity);

                return default;
            }

            _viewStash.Set(entity, new ViewComponent
            {
                View = view
            });

            return entity;
        }
    }
}