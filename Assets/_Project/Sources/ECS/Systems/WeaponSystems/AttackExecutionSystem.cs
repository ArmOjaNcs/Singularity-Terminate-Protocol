using ECS.AttackComponents;
using ECS.CommonComponents;
using ECS.EnemyComponents;
using ECS.PlayerComponents;
using ECS.ProjectileComponents;
using ECS.WeaponComponents;
using Gameplay.Enemy;
using Scellecs.Morpeh;
using ECS.ProjectileSystems;

namespace ECS.WeaponSystems
{
    public sealed class AttackExecutionSystem : ISystem
    {
        public World World { get; set; }

        private Filter _filter;
        private Filter _livingPlayerFilter;

        private Stash<WeaponComponent> _weaponStash;
        private Stash<AttackStatusComponent> _attackStatusStash;
        private Stash<ProjectileCreationRequestComponent> _creationRequestStash;
        private Stash<DeathComponent> _deathStash;
        private Stash<PlayerTag> _playerTagStash;
        private Stash<EnemyTag> _enemyTagStash;
        private Stash<AttackRequest> _attackRequestStash;
        private Stash<AttackComponent> _attackStash;
        private Stash<EnemyViewComponent> _enemyViewStash;

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

            _livingPlayerFilter = World.Filter
                .With<PlayerTag>()
                .Without<DeathComponent>()
                .Build();

            _weaponStash = World.GetStash<WeaponComponent>();
            _attackStatusStash = World.GetStash<AttackStatusComponent>();
            _creationRequestStash = World.GetStash<ProjectileCreationRequestComponent>();
            _deathStash = World.GetStash<DeathComponent>();
            _playerTagStash = World.GetStash<PlayerTag>();
            _enemyTagStash = World.GetStash<EnemyTag>();
            _attackRequestStash = World.GetStash<AttackRequest>();
            _attackStash = World.GetStash<AttackComponent>();
            _enemyViewStash = World.GetStash<EnemyViewComponent>();
        }

        public void OnUpdate(float deltaTime)
        {
            bool hasLivingPlayer = HasLivingPlayer();

            foreach (Entity weapon in _filter)
            {
                ref WeaponComponent weaponComponent = ref _weaponStash.Get(weapon);
                ref AttackStatusComponent attackStatus = ref _attackStatusStash.Get(weapon);

                if (!attackStatus.IsAttacking)
                    continue;

                if (_enemyTagStash.Has(weaponComponent.Owner) &&
                    !hasLivingPlayer)
                {
                    CancelEnemyAttack(weapon, weaponComponent.Owner,
                        ref attackStatus);
                    continue;
                }

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

        private bool HasLivingPlayer()
        {
            foreach (Entity player in _livingPlayerFilter)
                return true;

            return false;
        }

        private void CancelEnemyAttack(
            Entity weapon,
            Entity owner,
            ref AttackStatusComponent attackStatus)
        {
            _projectileFactory.Cancel(weapon);

            if (_creationRequestStash.Has(weapon))
                _creationRequestStash.Remove(weapon);

            if (_attackRequestStash.Has(weapon))
                _attackRequestStash.Remove(weapon);

            attackStatus.IsAttacking = false;
            attackStatus.AnimationFinished = true;
            attackStatus.AttackPerformed = false;
            attackStatus.CreationFinished = true;

            if (_attackStash.Has(weapon))
            {
                ref AttackComponent attack = ref _attackStash.Get(weapon);
                attack.CurrentCooldown = attack.AttackCooldown;
            }

            if (_enemyViewStash.Has(owner))
            {
                EnemyView view = _enemyViewStash.Get(owner).View;
                if (view != null)
                    view.CancelAttack();
            }
        }

        public void Dispose() { }
    }
}
