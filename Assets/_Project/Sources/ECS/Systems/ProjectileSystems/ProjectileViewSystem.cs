using ECS.CommonComponents;
using ECS.ProjectileComponents;
using Gameplay.View;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.ProjectileSystems
{
    public sealed class ProjectileViewSystem : ISystem
    {
        private Filter _filter;

        private Stash<PositionComponent> _positionStash;
        private Stash<RotationComponent> _rotationStash;
        private Stash<ProjectileViewComponent> _projectileViewStash;
        private Stash<ProjectileVisualFlipComponent> _visualFlipStash;
        private Stash<SwordSlashComponent> _swordSlashStash;
        private Stash<RipperTrajectoryComponent> _ripperTrajectoryStash;

        public World World { get; set; }

        public void OnAwake()
        {
            _filter = World.Filter
                .With<PositionComponent>()
                .With<RotationComponent>()
                .With<ProjectileViewComponent>()
                .Build();

            _positionStash = World.GetStash<PositionComponent>();
            _rotationStash = World.GetStash<RotationComponent>();
            _projectileViewStash =
                World.GetStash<ProjectileViewComponent>();
            _visualFlipStash =
                World.GetStash<ProjectileVisualFlipComponent>();
            _swordSlashStash = World.GetStash<SwordSlashComponent>();
            _ripperTrajectoryStash =
                World.GetStash<RipperTrajectoryComponent>();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (Entity entity in _filter)
            {
                ref PositionComponent position =
                    ref _positionStash.Get(entity);

                ref RotationComponent rotation =
                    ref _rotationStash.Get(entity);

                ref ProjectileViewComponent viewComponent =
                    ref _projectileViewStash.Get(entity);

                AnimatedView view = viewComponent.View;

                if (view == null)
                    continue;

                Transform viewTransform = view.transform;

                viewTransform.position = position.Position;
                viewTransform.rotation = Quaternion.Euler(
                    0f,
                    0f,
                    rotation.Angle);

                Vector3 scale = viewTransform.localScale;
                bool mirrorY =
                    (_visualFlipStash.Has(entity) &&
                     _visualFlipStash.Get(entity).FlipY) ||
                    (_swordSlashStash.Has(entity) &&
                     _swordSlashStash.Get(entity).MirrorY) ||
                    (_ripperTrajectoryStash.Has(entity) &&
                     _ripperTrajectoryStash.Get(entity).MirrorY);
                float ySign = mirrorY ? -1f : 1f;
                scale.y = Mathf.Abs(scale.y) * ySign;
                viewTransform.localScale = scale;
            }
        }

        public void Dispose()
        {
        }
    }
}
