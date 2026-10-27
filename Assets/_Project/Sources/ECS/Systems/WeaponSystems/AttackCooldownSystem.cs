using ECS.AttackComponents;
using ECS.CommonComponents;
using ECS.EnemyComponents;
using ECS.PlayerComponents;
using ECS.WeaponComponents;
using Scellecs.Morpeh;

namespace ECS.WeaponSystems
{
    public sealed class AttackCooldownSystem : ISystem
    {
        public World World { get; set; }

        private Filter _filter;
        private Filter _livingPlayerFilter;

        private Stash<WeaponComponent> _weaponStash;
        private Stash<AttackComponent> _attackStash;
        private Stash<AttackStatusComponent> _attackStatusStash;
        private Stash<TargetComponent> _targetStash;
        private Stash<AttackRequest> _attackRequestStash;
        private Stash<DeathComponent> _deathStash;
        private Stash<EnemyTag> _enemyTagStash;
        private Stash<PlayerTag> _playerTagStash;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<WeaponComponent>()
                .With<AttackComponent>()
                .With<AttackStatusComponent>()
                .With<TargetComponent>()
                .Build();

            _livingPlayerFilter = World.Filter
                .With<PlayerTag>()
                .Without<DeathComponent>()
                .Build();

            _weaponStash = World.GetStash<WeaponComponent>();
            _attackStash = World.GetStash<AttackComponent>();
            _attackStatusStash = World.GetStash<AttackStatusComponent>();
            _targetStash = World.GetStash<TargetComponent>();
            _attackRequestStash = World.GetStash<AttackRequest>();
            _deathStash = World.GetStash<DeathComponent>();
            _enemyTagStash = World.GetStash<EnemyTag>();
            _playerTagStash = World.GetStash<PlayerTag>();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (Entity weapon in _filter)
            {
                ref WeaponComponent weaponComponent =
                    ref _weaponStash.Get(weapon);

                if (_deathStash.Has(weaponComponent.Owner))
                    continue;

                if (_enemyTagStash.Has(weaponComponent.Owner) &&
                    !HasLivingPlayer())
                {
                    if (_attackRequestStash.Has(weapon))
                        _attackRequestStash.Remove(weapon);
                    continue;
                }

                ref AttackComponent attack =
                    ref _attackStash.Get(weapon);

                ref AttackStatusComponent attackStatus =
                    ref _attackStatusStash.Get(weapon);

                if (attackStatus.IsAttacking)
                    continue;

                if (attack.CurrentCooldown > 0f)
                {
                    attack.CurrentCooldown -= deltaTime;

                    if (attack.CurrentCooldown > 0f)
                        continue;

                    attack.CurrentCooldown = 0f;
                }

                if (_attackRequestStash.Has(weapon))
                    continue;

                ref TargetComponent target = ref _targetStash.Get(weapon);

                if (!IsValidTarget(target.Target, attack.TargetType))
                    continue;

                _attackRequestStash.Set(weapon, new AttackRequest());
                attack.CurrentCooldown = attack.AttackCooldown;
            }
        }

        private bool HasLivingPlayer()
        {
            foreach (Entity player in _livingPlayerFilter)
                return true;

            return false;
        }

        private bool IsValidTarget(Entity target, AttackTargetType targetType)
        {
            if (target == default)
                return false;

            if (_deathStash.Has(target))
                return false;

            return targetType switch
            {
                AttackTargetType.Enemy => _enemyTagStash.Has(target),
                AttackTargetType.Player => _playerTagStash.Has(target),
                _ => false
            };
        }

        public void Dispose() { }
    }
}
