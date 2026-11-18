using ECS.CommonComponents;
using ECS.ProjectileComponents;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.TrajectorySystems
{
    public sealed class SwordThrustSystem : ISystem
    {
        public World World { get; set; }

        private Filter _filter;
        private Stash<SwordThrustComponent> _thrustStash;
        private Stash<ProjectileComponent> _projectileStash;
        private Stash<PositionComponent> _positionStash;
        private Stash<RotationComponent> _rotationStash;
        private Stash<HitBoxComponent> _hitBoxStash;
        private Stash<PositionComponent> _ownerPositionStash;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<SwordThrustComponent>()
                .With<ProjectileComponent>()
                .With<PositionComponent>()
                .With<RotationComponent>()
                .With<HitBoxComponent>()
                .Build();
            _thrustStash = World.GetStash<SwordThrustComponent>();
            _projectileStash = World.GetStash<ProjectileComponent>();
            _positionStash = World.GetStash<PositionComponent>();
            _rotationStash = World.GetStash<RotationComponent>();
            _hitBoxStash = World.GetStash<HitBoxComponent>();
            _ownerPositionStash = World.GetStash<PositionComponent>();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (Entity entity in _filter)
            {
                ref SwordThrustComponent thrust =
                    ref _thrustStash.Get(entity);
                ref ProjectileComponent projectile =
                    ref _projectileStash.Get(entity);

                if (projectile.CleanupRequested)
                    continue;

                thrust.CurrentTime += Mathf.Max(0f, deltaTime);
                if (thrust.CurrentTime < 0f)
                {
                    projectile.CollisionEnabled = false;
                    continue;
                }

                projectile.CollisionEnabled = true;
                float progress = thrust.Duration <= 0f
                    ? 1f
                    : Mathf.Clamp01(thrust.CurrentTime / thrust.Duration);
                Vector3 ownerOffset = Vector3.zero;
                if (thrust.FollowOwner &&
                    _ownerPositionStash.Has(thrust.Owner))
                {
                    ownerOffset =
                        _ownerPositionStash.Get(thrust.Owner).Position;
                }

                Vector3 startPosition = thrust.StartPosition + ownerOffset;
                Vector3 endPosition = thrust.EndPosition + ownerOffset;
                Vector3 segment = endPosition - startPosition;
                Vector3 direction = segment.sqrMagnitude > 0.0001f
                    ? segment.normalized
                    : Vector3.right;

                _positionStash.Get(entity).Position =
                    Vector3.Lerp(startPosition, endPosition, progress);

                float angle = Mathf.Atan2(direction.y, direction.x) *
                              Mathf.Rad2Deg;
                _rotationStash.Get(entity).Angle = angle;
                ref ColliderComponent collider =
                    ref _hitBoxStash.Get(entity).Collider;
                collider.Angle = angle;
            }
        }

        public void Dispose() { }
    }
}
