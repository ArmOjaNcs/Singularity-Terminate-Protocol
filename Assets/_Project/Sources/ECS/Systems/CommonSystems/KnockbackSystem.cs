using ECS.CommonComponents;
using Gameplay;
using Gameplay.Navigation;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.CommonSystems
{
    public sealed class KnockbackSystem : ISystem
    {
        private const float KnockbackDuration = 0.2f;

        private readonly MovementResolver _movementResolver;

        public World World { get; set; }

        private Filter _requestFilter;
        private Filter _filter;

        private Stash<KnockbackComponent> _knockbackStash;
        private Stash<MovementLockComponent> _movementLockStash;
        private Stash<PositionComponent> _positionStash;
        private Stash<ColliderComponent> _colliderStash;
        private Stash<KnockbackRequest> _requestStash;

        public KnockbackSystem(NavigationGrid navigationGrid)
        {
            _movementResolver = new MovementResolver(navigationGrid);
        }

        public void OnAwake()
        {
            _filter = World.Filter
                .With<KnockbackComponent>()
                .With<PositionComponent>()
                .With<ColliderComponent>()
                .Build();

            _requestFilter = World.Filter
                .With<KnockbackRequest>()
                .Build();

            _knockbackStash = World.GetStash<KnockbackComponent>();
            _movementLockStash = World.GetStash<MovementLockComponent>();
            _positionStash = World.GetStash<PositionComponent>();
            _colliderStash = World.GetStash<ColliderComponent>();
            _requestStash = World.GetStash<KnockbackRequest>();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (Entity requestEntity in _requestFilter)
            {
                ref KnockbackRequest request =
                    ref _requestStash.Get(requestEntity);

                if (request.Target == default ||
                    !_positionStash.Has(request.Target) ||
                    !_colliderStash.Has(request.Target))
                {
                    World.RemoveEntity(requestEntity);
                    continue;
                }

                Vector3 direction = request.Direction;
                direction.z = 0f;

                if (direction.sqrMagnitude <= 0f)
                {
                    World.RemoveEntity(requestEntity);
                    continue;
                }

                _knockbackStash.Set(
                    request.Target,
                    new KnockbackComponent
                    {
                        Direction = direction.normalized,
                        Force = request.Force,
                        Duration = KnockbackDuration,
                        Elapsed = 0f
                    });

                _movementLockStash.Set(
                    request.Target,
                    new MovementLockComponent());

                World.RemoveEntity(requestEntity);
            }

            foreach (Entity entity in _filter)
            {
                ref KnockbackComponent knockback =
                    ref _knockbackStash.Get(entity);

                ref PositionComponent position =
                    ref _positionStash.Get(entity);

                ref ColliderComponent collider =
                    ref _colliderStash.Get(entity);

                if (knockback.Duration <= 0f ||
                    knockback.Elapsed >= knockback.Duration)
                {
                    RemoveKnockback(entity);
                    continue;
                }

                float progress = Mathf.Clamp01(
                    knockback.Elapsed / knockback.Duration);

                float strength = 1f -
                    Mathf.SmoothStep(0f, 1f, progress);

                Vector3 delta =
                    knockback.Direction *
                    knockback.Force *
                    strength *
                    deltaTime;

                _movementResolver.Move(
                    ref position.Position,
                    delta,
                    collider);

                knockback.Elapsed += deltaTime;

                if (knockback.Elapsed >= knockback.Duration)
                    RemoveKnockback(entity);
            }
        }

        private void RemoveKnockback(Entity entity)
        {
            _knockbackStash.Remove(entity);
            _movementLockStash.Remove(entity);
        }

        public void Dispose()
        {
        }
    }
}