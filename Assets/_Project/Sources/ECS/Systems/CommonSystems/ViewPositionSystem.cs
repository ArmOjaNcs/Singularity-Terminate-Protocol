using ECS.CommonComponents;
using ECS.ViewComponents;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.CommonSystems
{
    public sealed class ViewPositionSystem : ISystem
    {
        private Filter _filter;
        private Stash<PositionComponent> _positionStash;
        private Stash<ViewComponent> _viewStash;

        public World World { get; set; }

        public void OnAwake()
        {
            _filter = World.Filter
                .With<PositionComponent>()
                .With<ViewComponent>()
                .Build();

<<<<<<<< HEAD:Assets/_Project/Sources/ECS/Systems/Common/ViewPositionSystem.cs
            _positionStash =
                World.GetStash<PositionComponent>();

            _viewStash =
                World.GetStash<ViewComponent>();
========
            _positionStash = World.GetStash<PositionComponent>();

            _viewStash = World.GetStash<ViewComponent>();
>>>>>>>> remotes/origin/WeaponDev:Assets/_Project/Sources/ECS/Systems/CommonSystems/ViewPositionSystem.cs
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (Entity entity in _filter)
            {
<<<<<<<< HEAD:Assets/_Project/Sources/ECS/Systems/Common/ViewPositionSystem.cs
                ref PositionComponent position =
                    ref _positionStash.Get(entity);

                ref ViewComponent viewComponent =
                    ref _viewStash.Get(entity);

                viewComponent.View.transform.position =
                    position.Position;
========
                ref PositionComponent position = ref _positionStash.Get(entity);

                ref ViewComponent viewComponent = ref _viewStash.Get(entity);
   
                viewComponent.View.transform.position = position.Position;
>>>>>>>> remotes/origin/WeaponDev:Assets/_Project/Sources/ECS/Systems/CommonSystems/ViewPositionSystem.cs
            }
        }

        public void Dispose()
        {
        }
    }
}