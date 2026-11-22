using ECS.AttackComponents;
using ECS.CommonComponents;
using ECS.EnemyComponents;
using ECS.PlayerComponents;
using ECS.WeaponComponents;
using Gameplay;
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
        private Stash<AttackSequenceComponent> _sequenceStash;
        private Stash<TargetComponent> _targetStash;
        private Stash<AttackRequest> _attackRequestStash;
        private Stash<DeathComponent> _deathStash;
        private Stash<EnemyTag> _enemyTagStash;
        private Stash<PlayerTag> _playerTagStash;
        private Stash<DamageableTag> _damageableTagStash;

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
            _sequenceStash = World.GetStash<AttackSequenceComponent>();
            _targetStash = World.GetStash<TargetComponent>();
            _attackRequestStash = World.GetStash<AttackRequest>();
            _deathStash = World.GetStash<DeathComponent>();
            _enemyTagStash = World.GetStash<EnemyTag>();
            _playerTagStash = World.GetStash<PlayerTag>();
            _damageableTagStash = World.GetStash<DamageableTag>();
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

                bool swordMainSeriesFinished =
                    weaponComponent.Type.UsesSwordAttack() &&
                    _sequenceStash.Has(weapon) &&
                    _sequenceStash.Get(weapon).SwordMainSeriesFinished;

                if (attackStatus.IsAttacking && !swordMainSeriesFinished)
                    continue;

                if (attack.CurrentCooldown > 0f)
                {
                    attack.CurrentCooldown -= deltaTime;

                    if (attack.CurrentCooldown > 0f)
                        continue;

                    attack.CurrentCooldown = 0f;
                }

                if (attackStatus.IsAttacking)
                    continue;

                if (_attackRequestStash.Has(weapon))
                    continue;

                ref TargetComponent target = ref _targetStash.Get(weapon);

                Entity targetEntity = target.Target;
                bool hasValidTarget = targetEntity != default &&
                                      !World.IsDisposed(targetEntity) &&
                                      !_deathStash.Has(targetEntity) &&
                                      CommonFunctions.IsValidTarget(
                                          targetEntity,
                                          attack.TargetTypes,
                                          _damageableTagStash,
                                          _enemyTagStash,
                                          _playerTagStash);

                // A fallback weapon can start its attack cycle without a target.
                // TargetTypes is empty while a weapon is disabled in the test bench,
                // so the fallback cannot accidentally re-enable it.
                bool canUseRandomDirectionFallback =
                    attack.Pattern.RandomDirectionFallback &&
                    attack.TargetTypes != null &&
                    attack.TargetTypes.Length > 0;

                if (!hasValidTarget && !canUseRandomDirectionFallback)
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

        public void Dispose() { }
    }
}
