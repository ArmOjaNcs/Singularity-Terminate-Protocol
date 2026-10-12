using ECS.AttackComponents;
using ECS.CommonComponents;
using ECS.WeaponComponents;
using Scellecs.Morpeh;

namespace ECS.WeaponSystems
{
    public sealed class AttackSystem : ISystem
    {
        public World World { get; set; }

        private Filter _filter;

        private Stash<WeaponComponent> _weaponStash;
        private Stash<AttackStatusComponent> _attackStatusStash;
        private Stash<AttackRequest> _attackRequestStash;
        private Stash<DeathComponent> _deathStash;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<WeaponComponent>()
                .With<AttackStatusComponent>()
                .With<AttackRequest>()
                .Build();

            _weaponStash = World.GetStash<WeaponComponent>();
            _attackStatusStash = World.GetStash<AttackStatusComponent>();
            _attackRequestStash = World.GetStash<AttackRequest>();
            _deathStash = World.GetStash<DeathComponent>();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (Entity weapon in _filter)
            {
                ref WeaponComponent weaponComponent = ref _weaponStash.Get(weapon);

                if (_deathStash.Has(weaponComponent.Owner))
                {
                    _attackRequestStash.Remove(weapon);
                    continue;
                }

                ref AttackStatusComponent attackStatus = ref _attackStatusStash.Get(weapon);

                if (attackStatus.IsAttacking)
                {
                    _attackRequestStash.Remove(weapon);
                    continue;
                }

                attackStatus.IsAttacking = true;
                attackStatus.AnimationFinished = false;
                attackStatus.AttackPerformed = false;
                attackStatus.CreationFinished = false;

                _attackRequestStash.Remove(weapon);
            }
        }

        public void Dispose() { }
    }
}
