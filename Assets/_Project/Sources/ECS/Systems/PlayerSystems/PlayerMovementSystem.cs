using ECS.CommonComponents;
using ECS.PlayerComponents;
using Gameplay.Navigation;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.PlayerSystems
{
    public sealed class PlayerMovementSystem : ISystem
    {
        public World World { get; set; }

        private readonly NavigationGrid _navigationGrid;

        private Filter _filter;

        private Stash<MovementComponent> _movementStash;
        private Stash<PositionComponent> _positionStash;
        private Stash<PlayerStatsComponent> _statsStash;
        private Stash<NavigationRadiusComponent> _radiusStash;

        public PlayerMovementSystem(
            NavigationGrid navigationGrid)
        {
            _navigationGrid = navigationGrid;
        }

        public void OnAwake()
        {
            _filter = World.Filter
                .With<PlayerTag>()
                .With<MovementComponent>()
                .With<PositionComponent>()
                .With<PlayerStatsComponent>()
                .With<NavigationRadiusComponent>()
                .Build();

            _movementStash =
                World.GetStash<MovementComponent>();

            _positionStash =
                World.GetStash<PositionComponent>();

            _statsStash =
                World.GetStash<PlayerStatsComponent>();

            _radiusStash =
                World.GetStash<NavigationRadiusComponent>();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (var entity in _filter)
            {
                ref var movement =
                    ref _movementStash.Get(entity);

                ref var position =
                    ref _positionStash.Get(entity);

                ref var stats =
                    ref _statsStash.Get(entity);

                ref var radius =
                    ref _radiusStash.Get(entity);

                Vector3 delta =
                    movement.Direction *
                    stats.Stats.Speed *
                    deltaTime;

                if (delta.sqrMagnitude <= 0f)
                    continue;

                TryMove(
                    ref position.Position,
                    delta,
                    radius.Radius
                );
            }
        }

        private void TryMove(
            ref Vector3 position,
            Vector3 delta,
            float radius)
        {
            Vector3 target = position + delta;

            if (_navigationGrid.IsWalkable(
                target,
                radius))
            {
                position = target;
                return;
            }

            Vector3 xTarget = new Vector3(
                target.x,
                position.y,
                position.z
            );

            if (_navigationGrid.IsWalkable(
                xTarget,
                radius))
            {
                position = xTarget;
            }

            Vector3 zTarget = new Vector3(
                position.x,
                position.y,
                target.z
            );

            if (_navigationGrid.IsWalkable(
                zTarget,
                radius))
            {
                position = zTarget;
            }
        }

        public void Dispose()
        {
        }
    }
}