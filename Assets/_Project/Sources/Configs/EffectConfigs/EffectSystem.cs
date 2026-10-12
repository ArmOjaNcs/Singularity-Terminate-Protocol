using ECS.CommonComponents;
using ECS.EffectComponents;
using ECS.WeaponComponents;
using EffectComponents;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.CommonSystems
{
    public sealed class EffectSystem : ISystem
    {
        public World World { get; set; }

        private Filter _filter;

        private Stash<EffectRequest> _effectRequestStash;
        private Stash<WeaponEffectsComponent> _weaponEffectsStash;
        private Stash<WeaponComponent> _weaponStash;
        private Stash<PositionComponent> _positionStash;
        private Stash<KnockbackRequest> _knockbackRequestStash;

        public void OnAwake()
        {
            _filter = World.Filter
                .With<EffectRequest>()
                .Build();

            _effectRequestStash =
                World.GetStash<EffectRequest>();

            _weaponEffectsStash =
                World.GetStash<WeaponEffectsComponent>();

            _weaponStash =
                World.GetStash<WeaponComponent>();

            _positionStash =
                World.GetStash<PositionComponent>();

            _knockbackRequestStash =
                World.GetStash<KnockbackRequest>();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (Entity requestEntity in _filter)
            {
                ref EffectRequest request =
                    ref _effectRequestStash.Get(requestEntity);

                Entity weapon =
                    request.Context.Weapon;

                if (weapon == default)
                {
                    World.RemoveEntity(requestEntity);
                    continue;
                }

                if (!_weaponEffectsStash.Has(weapon))
                {
                    World.RemoveEntity(requestEntity);
                    continue;
                }

                EffectCollection effects = _weaponEffectsStash.Get(weapon).Effects;

                if (effects.TryGet<KnockbackEffectRuntime>(
                        out KnockbackEffectRuntime knockback))
                {
                    CreateKnockbackRequest(request, knockback);
                }

                World.RemoveEntity(requestEntity);
            }
        }

        private void CreateKnockbackRequest(
            EffectRequest request,
            KnockbackEffectRuntime knockback)
        {
            Entity weapon =
                request.Context.Weapon;

            if (!_weaponStash.Has(weapon))
                return;

            Entity owner =
                _weaponStash.Get(weapon).Owner;

            if (!_positionStash.Has(owner) ||
                !_positionStash.Has(request.Target))
                return;

            Vector3 sourcePosition =
                _positionStash.Get(owner).Position;

            Vector3 targetPosition =
                _positionStash.Get(request.Target).Position;

            Vector3 direction =
                targetPosition - sourcePosition;

            direction.z = 0f;

            if (direction.sqrMagnitude <= 0f)
                return;

            direction.Normalize();

            Entity knockbackRequestEntity =
                World.CreateEntity();

            _knockbackRequestStash.Set(
                knockbackRequestEntity,
                new KnockbackRequest
                {
                    Source = owner,
                    Target = request.Target,
                    Direction = direction,
                    Force = knockback.Force
                });
        }

        public void Dispose() { }
    }
}