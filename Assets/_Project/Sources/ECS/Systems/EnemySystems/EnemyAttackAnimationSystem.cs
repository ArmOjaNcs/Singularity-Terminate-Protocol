using ECS.AttackComponents;
using ECS.EnemyComponents;
using Gameplay.Enemy;
using Scellecs.Morpeh;

namespace ECS.EnemySystems
{
    public sealed class EnemyAttackAnimationSystem : ISystem
    {
        public World World { get; set; }

        private Filter _filter;

        private Stash<AttackComponent> _attackStash;
        private Stash<EnemyViewComponent> _enemyViewStash;
        private Stash<AttackRequest> _attackRequestStash;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<EnemyTag>()
                .With<AttackComponent>()
                .With<EnemyViewComponent>()
                .Build();

            _attackStash = World.GetStash<AttackComponent>();
            _enemyViewStash = World.GetStash<EnemyViewComponent>();
            _attackRequestStash = World.GetStash<AttackRequest>();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (Entity entity in _filter)
            {
                ref AttackComponent attack = ref _attackStash.Get(entity);

                if (!attack.IsAttacking)
                    continue;

                EnemyView view = _enemyViewStash.Get(entity).View;

                view.OnAttackPerformed -= OnAttackPerformed;
                view.OnAttackPerformed += OnAttackPerformed;

                view.OnAttackFinished -= OnAttackFinished;
                view.OnAttackFinished += OnAttackFinished;

                view.SetAttack();
            }
        }

        private void OnAttackPerformed(EnemyView view)
        {
            Entity entity = view.Entity;

            if (!_attackStash.Has(entity))
                return;

            if (_attackRequestStash.Has(entity))
                return;

            _attackRequestStash.Add(entity);

            ref AttackRequest request =
                ref _attackRequestStash.Get(entity);

            request.Source = entity;
        }

        private void OnAttackFinished(EnemyView view)
        {
            Entity entity = view.Entity;

            if (!_attackStash.Has(entity))
                return;

            ref AttackComponent attack =
                ref _attackStash.Get(entity);

            attack.IsAttacking = false;

            view.OnAttackPerformed -= OnAttackPerformed;
            view.OnAttackFinished -= OnAttackFinished;
        }

        public void Dispose()
        {
        }
    }
}