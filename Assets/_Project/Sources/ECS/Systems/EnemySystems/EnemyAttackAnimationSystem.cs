using System.Collections.Generic;
using ECS.AttackComponents;
using ECS.EnemyComponents;
using ECS.ProjectileComponents;
using ECS.WeaponComponents;
using Gameplay.Enemy;
using Scellecs.Morpeh;

namespace ECS.EnemySystems
{
    public sealed class EnemyAttackAnimationSystem : ISystem
    {
        public World World { get; set; }

        private Filter _enemyFilter;
        private Filter _weaponFilter;

        private Stash<EnemyViewComponent> _enemyViewStash;
        private Stash<WeaponComponent> _weaponStash;
        private Stash<AttackComponent> _attackStash;
        private Stash<AttackStatusComponent> _attackStatusStash;
        private Stash<ProjectileCreationRequestComponent> _creationRequestStash;

        private readonly HashSet<EnemyView> _subscribedViews = new();

        public void OnAwake()
        {
            _enemyFilter = World.Filter
                .With<EnemyTag>()
                .With<EnemyViewComponent>()
                .Build();

            _weaponFilter = World.Filter
                .With<WeaponComponent>()
                .With<AttackStatusComponent>()
                .Build();

            _enemyViewStash = World.GetStash<EnemyViewComponent>();
            _weaponStash = World.GetStash<WeaponComponent>();
            _attackStash = World.GetStash<AttackComponent>();
            _attackStatusStash = World.GetStash<AttackStatusComponent>();
            _creationRequestStash = World.GetStash<ProjectileCreationRequestComponent>();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (Entity enemy in _enemyFilter)
            {
                if (!TryGetWeapon(enemy, out Entity weapon))
                {
                    if (_enemyViewStash.Has(enemy))
                        Unsubscribe(_enemyViewStash.Get(enemy).View);
                    continue;
                }

                ref AttackStatusComponent attackStatus = ref _attackStatusStash.Get(weapon);

                if (!attackStatus.IsAttacking || attackStatus.AnimationFinished)
                {
                    if (_enemyViewStash.Has(enemy))
                        Unsubscribe(_enemyViewStash.Get(enemy).View);
                    continue;
                }

                if (_attackStash.Get(weapon).AnimationPolicy == AttackAnimationPolicyType.Immediate)
                {
                    if (_enemyViewStash.Has(enemy))
                        Unsubscribe(_enemyViewStash.Get(enemy).View);
                    if (!attackStatus.AttackPerformed)
                    {
                        attackStatus.AttackPerformed = true;
                        attackStatus.AnimationFinished = true;
                        if (!_creationRequestStash.Has(weapon))
                            _creationRequestStash.Add(weapon);
                    }
                    continue;
                }

                EnemyView view = _enemyViewStash.Get(enemy).View;

                if (view == null)
                    continue;

                if (!_subscribedViews.Add(view))
                    continue;

                view.OnAttackPerformed += OnAttackPerformed;
                view.OnAttackFinished += OnAttackFinished;

                view.SetAttack();
            }
        }

        private void OnAttackPerformed(EnemyView view)
        {
            Entity enemy = view.Entity;

            if (!TryGetWeapon(enemy, out Entity weapon))
                return;

            ref AttackStatusComponent attackStatus =
                ref _attackStatusStash.Get(weapon);

            if (!attackStatus.IsAttacking || attackStatus.AttackPerformed)
                return;

            attackStatus.AttackPerformed = true;

            if (!_creationRequestStash.Has(weapon))
                _creationRequestStash.Add(weapon);
        }

        private void OnAttackFinished(EnemyView view)
        {
            Entity enemy = view.Entity;

            if (TryGetWeapon(enemy, out Entity weapon))
            {
                ref AttackStatusComponent attackStatus =
                    ref _attackStatusStash.Get(weapon);

                attackStatus.AnimationFinished = true;
            }

            Unsubscribe(view);
        }

        private bool TryGetWeapon(Entity owner, out Entity weapon)
        {
            foreach (Entity entity in _weaponFilter)
            {
                ref WeaponComponent weaponComponent =
                    ref _weaponStash.Get(entity);

                if (weaponComponent.Owner != owner)
                    continue;

                weapon = entity;
                return true;
            }

            weapon = default;
            return false;
        }

        private void Unsubscribe(EnemyView view)
        {
            if (!_subscribedViews.Remove(view))
                return;

            view.OnAttackPerformed -= OnAttackPerformed;
            view.OnAttackFinished -= OnAttackFinished;
        }

        public void Dispose()
        {
            foreach (EnemyView view in _subscribedViews)
            {
                if (view == null)
                    continue;

                view.OnAttackPerformed -= OnAttackPerformed;
                view.OnAttackFinished -= OnAttackFinished;
            }

            _subscribedViews.Clear();
        }
    }
}