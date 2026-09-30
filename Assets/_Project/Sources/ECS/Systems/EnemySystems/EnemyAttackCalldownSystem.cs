using ECS.AttackComponents;
using ECS.CommonComponents;
using ECS.EnemyComponents;
using Scellecs.Morpeh;

namespace ECS.EnemySystems
{
    public sealed class EnemyAttackCalldownSystem : ISystem
    {
        public World World { get; set; }

        private Filter _filter;

        private Stash<AttackComponent> _attackStash;
        private Stash<TargetComponent> _targetStash;
        private Stash<PositionComponent> _positionStash;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<EnemyTag>()
                .With<AttackComponent>()
                .With<TargetComponent>()
                .With<PositionComponent>()
                .Build();

            _attackStash = World.GetStash<AttackComponent>();
            _targetStash = World.GetStash<TargetComponent>();
            _positionStash = World.GetStash<PositionComponent>();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (Entity entity in _filter)
            {
                ref AttackComponent attack = ref _attackStash.Get(entity);

                if (attack.IsAttacking)
                    continue;

                if (attack.CurrentCooldown > 0f)
                {
                    attack.CurrentCooldown -= deltaTime;
                    continue;
                }

                ref TargetComponent target = ref _targetStash.Get(entity);

                if (target.Target == default)
                    continue;

                if (!_positionStash.Has(target.Target))
                    continue;

                ref PositionComponent position = ref _positionStash.Get(entity);
                ref PositionComponent targetPosition = ref _positionStash.Get(target.Target);

                float distance = (targetPosition.Position - position.Position).sqrMagnitude;
                float attackRadiusSqr = attack.AttackRadius * attack.AttackRadius;

                if (distance > attackRadiusSqr)
                    continue;

                attack.IsAttacking = true;
                attack.CurrentCooldown = attack.AttackCooldown;
            }
        }

        public void Dispose()
        {
        }
    }
}