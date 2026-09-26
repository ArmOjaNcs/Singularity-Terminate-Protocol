using ECS.CommonComponents;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.CommonSystems
{
    public sealed class AttackSystem : ISystem
    {
        public World World { get; set; }

        private Filter _filter;

        private Stash<AttackComponent> _attackStash;
        private Stash<TargetComponent> _targetStash;
        private Stash<PositionComponent> _positionStash;
        private Stash<HealthComponent> _healthStash;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<AttackComponent>()
                .With<TargetComponent>()
                .With<PositionComponent>()
                .Build();

            _attackStash =
                World.GetStash<AttackComponent>();

            _targetStash =
                World.GetStash<TargetComponent>();

            _positionStash =
                World.GetStash<PositionComponent>();

            _healthStash =
                World.GetStash<HealthComponent>();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (Entity entity in _filter)
            {
                ref AttackComponent attack =
                    ref _attackStash.Get(entity);

                if (attack.CurrentCooldown > 0f)
                {
                    attack.CurrentCooldown -= deltaTime;
                    continue;
                }

                ref TargetComponent target =
                    ref _targetStash.Get(entity);

                if (target.Target == default)
                    continue;

                if (!_healthStash.Has(target.Target))
                    continue;

                ref PositionComponent position =
                    ref _positionStash.Get(entity);

                ref PositionComponent targetPosition =
                    ref _positionStash.Get(target.Target);

                float distance =
                    (targetPosition.Position - position.Position)
                    .sqrMagnitude;

                float attackRadiusSqr =
                    attack.AttackRadius * attack.AttackRadius;

                if (distance > attackRadiusSqr)
                    continue;

                ref HealthComponent health =
                    ref _healthStash.Get(target.Target);

                health.Current -= attack.Damage;

                attack.CurrentCooldown =
                    attack.AttackCooldown;

                Debug.Log(
                    $"{entity} attacked {target.Target} " +
                    $"for {attack.Damage} damage. " +
                    $"Target HP: {health.Current}");
            }
        }

        public void Dispose()
        {
        }
    }
}