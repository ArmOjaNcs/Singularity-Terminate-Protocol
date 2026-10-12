using ECS.AttackComponents;
using ECS.CommonComponents;
using ECS.PlayerComponents;
using ECS.ProjectileComponents;
using ECS.WeaponComponents;
using Scellecs.Morpeh;
using ECS.ProjectileSystems;

namespace ECS.WeaponSystems
{
    public sealed class AttackExecutionSystem : ISystem
    {
        public World World { get; set; }

        private Filter _filter;

        private Stash<WeaponComponent> _weaponStash;
        private Stash<AttackStatusComponent> _attackStatusStash;
        private Stash<ProjectileCreationRequestComponent> _creationRequestStash;
        private Stash<DeathComponent> _deathStash;
        private Stash<PlayerTag> _playerTagStash;

        private readonly ProjectileFactory _projectileFactory;

        public AttackExecutionSystem(ProjectileFactory projectileFactory)
        {
            _projectileFactory = projectileFactory;
        }

        public void OnAwake()
        {
            _filter = World.Filter
                .With<WeaponComponent>()
                .With<AttackStatusComponent>()
                .Build();

            _weaponStash = World.GetStash<WeaponComponent>();
            _attackStatusStash = World.GetStash<AttackStatusComponent>();
            _creationRequestStash = World.GetStash<ProjectileCreationRequestComponent>();
            _deathStash = World.GetStash<DeathComponent>();
            _playerTagStash = World.GetStash<PlayerTag>();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (Entity weapon in _filter)
            {
                ref WeaponComponent weaponComponent = ref _weaponStash.Get(weapon);
                ref AttackStatusComponent attackStatus = ref _attackStatusStash.Get(weapon);

                if (!attackStatus.IsAttacking)
                    continue;

                if (_deathStash.Has(weaponComponent.Owner))
                {
                    _projectileFactory.Cancel(weapon);

                    if (_creationRequestStash.Has(weapon))
                        _creationRequestStash.Remove(weapon);

                    attackStatus.IsAttacking = false;
                    attackStatus.AnimationFinished = true;
                    attackStatus.AttackPerformed = false;
                    attackStatus.CreationFinished = true;

                    continue;
                }

                bool isPlayer = _playerTagStash.Has(weaponComponent.Owner);

                if (isPlayer && !attackStatus.AttackPerformed)
                {
                    attackStatus.AttackPerformed = true;
                    attackStatus.AnimationFinished = true;

                    if (!_creationRequestStash.Has(weapon))
                        _creationRequestStash.Add(weapon);
                }

                if (!attackStatus.CreationFinished)
                    continue;

                if (!attackStatus.AnimationFinished)
                    continue;

                attackStatus.IsAttacking = false;
                attackStatus.AttackPerformed = false;
            }
        }

        public void Dispose() { }
    }
}
