using ECS.CommonComponents;
using Scellecs.Morpeh;
using UnityEngine;
using Gameplay.Navigation;
using ECS.EnemyComponents;

namespace ECS.EnemySystems
{
    public sealed class EnemyPanicSystem : ISystem
    {
        private const int DestinationAttempts = 12;
        private const float MinimumDestinationDistance = 0.2f;
        private const float DestinationReachDistance = 0.15f;

        private readonly NavigationGrid _navigationGrid;

        public World World { get; set; }

        private Filter _requestFilter;
        private Filter _panicFilter;
        private Stash<PanicRequest> _requestStash;
        private Stash<EnemyPanicMovementComponent> _panicStash;
        private Stash<PositionComponent> _positionStash;
        private Stash<NavigationBoxComponent> _navigationBoxStash;
        private Stash<MovementLockComponent> _movementLockStash;
        private Stash<TwistStatusComponent> _twistStatusStash;
        private Stash<DeathComponent> _deathStash;

        public EnemyPanicSystem(NavigationGrid navigationGrid)
        {
            _navigationGrid = navigationGrid;
        }

        public void OnAwake()
        {
            _requestFilter = World.Filter
                .With<PanicRequest>()
                .Build();

            _panicFilter = World.Filter
                .With<EnemyPanicMovementComponent>()
                .With<PositionComponent>()
                .Build();

            _requestStash = World.GetStash<PanicRequest>();
            _panicStash = World.GetStash<EnemyPanicMovementComponent>();
            _positionStash = World.GetStash<PositionComponent>();
            _navigationBoxStash = World.GetStash<NavigationBoxComponent>();
            _movementLockStash = World.GetStash<MovementLockComponent>();
            _twistStatusStash = World.GetStash<TwistStatusComponent>();
            _deathStash = World.GetStash<DeathComponent>();
        }

        public void OnUpdate(float deltaTime)
        {
            UpdateActivePanics(deltaTime);
            ProcessRequests();
        }

        private void UpdateActivePanics(float deltaTime)
        {
            float frameTime = Mathf.Max(0f, deltaTime);

            foreach (Entity enemy in _panicFilter)
            {
                ref EnemyPanicMovementComponent panic =
                    ref _panicStash.Get(enemy);

                if (!panic.IsActive)
                    continue;

                // Knockback and other movement locks take priority over panic.
                if (_movementLockStash.Has(enemy))
                    continue;

                if (IsTwistActive(enemy))
                    continue;

                if (_deathStash.Has(enemy) ||
                    !_positionStash.Has(enemy) ||
                    Vector2.Distance(
                        _positionStash.Get(enemy).Position,
                        panic.Destination) <= DestinationReachDistance)
                {
                    panic.IsActive = false;
                    continue;
                }

                panic.RemainingDuration -= frameTime;
                if (panic.RemainingDuration <= 0f)
                    panic.IsActive = false;
            }
        }

        private void ProcessRequests()
        {
            foreach (Entity requestEntity in _requestFilter)
            {
                PanicRequest request = _requestStash.Get(requestEntity);

                if (request.Target != default &&
                    !World.IsDisposed(request.Target) &&
                    !IsTwistActive(request.Target) &&
                    !_deathStash.Has(request.Target) &&
                    _positionStash.Has(request.Target) &&
                    _navigationBoxStash.Has(request.Target) &&
                    !_movementLockStash.Has(request.Target) &&
                    !IsPanicActive(request.Target) &&
                    TryChooseDestination(request, out Vector3 destination))
                {
                    _panicStash.Set(request.Target,
                        new EnemyPanicMovementComponent
                        {
                            IsActive = true,
                            Destination = destination,
                            SpeedMultiplier = Mathf.Max(1f, request.SpeedMultiplier),
                            RemainingDuration = Mathf.Max(0.1f, request.Duration)
                        });
                }

                World.RemoveEntity(requestEntity);
            }
        }

        private bool IsPanicActive(Entity enemy)
        {
            return _panicStash.Has(enemy) &&
                   _panicStash.Get(enemy).IsActive;
        }

        private bool IsTwistActive(Entity enemy)
        {
            if (_twistStatusStash.Has(enemy) == false)
                return false;

            var instances = _twistStatusStash.Get(enemy).Instances;
            if (instances == null)
                return false;

            foreach (TwistInstance instance in instances)
            {
                if (instance.RemainingDuration > 0f)
                    return true;
            }

            return false;
        }

        private bool TryChooseDestination(
            PanicRequest request,
            out Vector3 destination)
        {
            Vector3 origin = _positionStash.Get(request.Target).Position;
            ColliderComponent collider =
                _navigationBoxStash.Get(request.Target).Collider;
            float maxDistance = Mathf.Max(0f, request.Distance);

            for (int i = 0; i < DestinationAttempts; i++)
            {
                Vector2 direction = Random.insideUnitCircle;
                if (direction.sqrMagnitude <= 0.0001f)
                    continue;

                direction.Normalize();
                float distance = Random.Range(
                    Mathf.Min(MinimumDestinationDistance, maxDistance),
                    maxDistance);
                Vector3 candidate = origin + (Vector3)(direction * distance);

                if (!_navigationGrid.IsWalkable(candidate, collider))
                    continue;

                destination = candidate;
                return true;
            }

            destination = origin;
            return false;
        }

        public void Dispose() { }
    }
}
