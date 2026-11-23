using ECS.CommonComponents;
using ECS.ProjectileComponents;
using Gameplay.Spatial;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.TrajectorySystems
{
    public sealed class ShrapnelFragmentFlightSystem : ISystem
    {
        public World World { get; set; }

        private Filter _fragments;
        private Stash<ShrapnelFragmentComponent> _fragmentStash;
        private Stash<ProjectileComponent> _projectileStash;
        private Stash<PositionComponent> _positionStash;
        private Stash<RotationComponent> _rotationStash;
        private Stash<HitBoxComponent> _hitBoxStash;

        public void OnAwake()
        {
            _fragments = World.Filter.With<ShrapnelFragmentComponent>()
                .With<ProjectileComponent>().With<PositionComponent>()
                .With<RotationComponent>().With<HitBoxComponent>().Build();
            _fragmentStash = World.GetStash<ShrapnelFragmentComponent>();
            _projectileStash = World.GetStash<ProjectileComponent>();
            _positionStash = World.GetStash<PositionComponent>();
            _rotationStash = World.GetStash<RotationComponent>();
            _hitBoxStash = World.GetStash<HitBoxComponent>();
        }

        public void OnUpdate(float deltaTime)
        {
            if (_fragments.IsEmpty())
                return;

            bool hasViewport = ProjectileViewport.TryGetBounds(out Rect viewport);

            foreach (Entity entity in _fragments)
            {
                ref ProjectileComponent projectile = ref _projectileStash.Get(entity);
                if (projectile.CleanupRequested)
                    continue;

                ref Vector3 position = ref _positionStash.Get(entity).Position;
                _fragmentStash.Get(entity).PreviousPosition = position;
                if (hasViewport)
                {
                    float radius = _hitBoxStash.Get(entity).Collider.Radius;
                    float travel = projectile.Speed * deltaTime;
                    position.x = ReflectAxis(position.x, ref projectile.Direction.x, travel,
                        viewport.xMin + radius, viewport.xMax - radius);
                    position.y = ReflectAxis(position.y, ref projectile.Direction.y, travel,
                        viewport.yMin + radius, viewport.yMax - radius);
                }
                else
                {
                    position += projectile.Direction * (projectile.Speed * deltaTime);
                }

                _rotationStash.Get(entity).Angle =
                    Mathf.Atan2(projectile.Direction.y, projectile.Direction.x) * Mathf.Rad2Deg;
            }
        }

        private static float ReflectAxis(
            float position, ref float direction, float travel, float min, float max)
        {
            float span = max - min;
            if (span <= 0f)
                return (min + max) * 0.5f;

            // Clamp to the current viewport before moving so a panning camera
            // cannot leave a shard behind. Fold overflow back at each edge.
            float offset = Mathf.Clamp(position, min, max) - min + direction * travel;
            float phase = Mathf.Repeat(offset, span * 2f);
            if (phase > span)
            {
                direction = -direction;
                return max - (phase - span);
            }

            if (phase == span)
                direction = -Mathf.Abs(direction);
            else if (phase == 0f)
                direction = Mathf.Abs(direction);

            return min + phase;
        }

        public void Dispose() { }
    }
}
