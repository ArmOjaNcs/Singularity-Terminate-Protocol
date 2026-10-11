using ECS.CommonComponents;
using ECS.WeaponComponents;
using Scellecs.Morpeh;

namespace ECS.WeaponSystems
{
    public sealed class WeaponPositionSystem : ISystem
    {
        private Filter _filter;
        private Stash<WeaponComponent> _weaponStash;
        private Stash<PositionComponent> _positionStash;

        public World World { get; set; }

        public void OnAwake()
        {
            _filter = World.Filter
                .With<WeaponTag>()
                .With<WeaponComponent>()
                .With<PositionComponent>()
                .Build();

            _weaponStash = World.GetStash<WeaponComponent>();
            _positionStash = World.GetStash<PositionComponent>();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (Entity entity in _filter)
            {
                ref WeaponComponent weapon = ref _weaponStash.Get(entity);

                if (!_positionStash.Has(weapon.Owner))
                    continue;

                ref PositionComponent ownerPosition = ref _positionStash.Get(weapon.Owner);

                ref PositionComponent position = ref _positionStash.Get(entity);

                position.Position = ownerPosition.Position;
            }
        }

        public void Dispose()
        {
        }
    }
}