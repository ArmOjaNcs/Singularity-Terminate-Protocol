using ECS.AttackComponents;
using ECS.CommonComponents;
using ECS.EffectComponents;
using ECS.ProjectileComponents;
using ECS.WeaponComponents;
using EffectComponents;
using Scellecs.Morpeh;
using WeaponConfigs;

namespace ECS.WeaponSystems
{
    public sealed class WeaponFactory
    {
        private readonly World _world;

        private readonly Stash<WeaponTag> _weaponTagStash;
        private readonly Stash<WeaponComponent> _weaponStash;
        private readonly Stash<AttackComponent> _attackStash;
        private readonly Stash<AttackStatusComponent> _attackStatusStash;
        private readonly Stash<ProjectileCreationComponent> _projectileCreationStash;
        private readonly Stash<TargetComponent> _targetStash;
        private readonly Stash<PositionComponent> _positionStash;
        private readonly Stash<WeaponEffectsComponent> _weaponEffectsStash;

        public WeaponFactory(World world)
        {
            _world = world;

            _weaponTagStash = world.GetStash<WeaponTag>();
            _weaponStash = world.GetStash<WeaponComponent>();
            _attackStash = world.GetStash<AttackComponent>();
            _attackStatusStash = world.GetStash<AttackStatusComponent>();
            _projectileCreationStash = world.GetStash<ProjectileCreationComponent>();
            _targetStash = world.GetStash<TargetComponent>();
            _positionStash = world.GetStash<PositionComponent>();
            _weaponEffectsStash = world.GetStash<WeaponEffectsComponent>();
        }

        public void Create(WeaponConfig config, Entity owner)
        {
            Entity weapon = _world.CreateEntity();

            _weaponTagStash.Set(weapon, new WeaponTag());

            _weaponStash.Set(weapon, new WeaponComponent
            {
                Owner = owner,
                Level = 1,
                Type = config.Type
            });

            _attackStash.Set(weapon, new AttackComponent
            {
                ProjectileCount = config.AttackConfig.ProjectileCount,
                ShotsInAttack = config.AttackConfig.ShotsInAttack,
                Pierce = config.AttackConfig.Pierce,
                MaxRicochets = config.AttackConfig.MaxRicochets,

                AttackDuration = config.AttackConfig.AttackDuration,
                FireRate = config.AttackConfig.FireRate,
                Damage = config.AttackConfig.Damage,
                AttackCooldown = config.AttackConfig.AttackCooldown,
                CurrentCooldown = config.AttackConfig.AttackCooldown,
                TargetSearchRadius = config.AttackConfig.TargetSearchRadius,
                DistanceFromSource = config.AttackConfig.DistanceFromSource,
                AttackRadius = config.AttackConfig.AttackRadius,
                ProjectileSpeed = config.AttackConfig.ProjectileSpeed,
                ProjectileLifetime = config.AttackConfig.ProjectileLifetime,
                RicochetChance = config.AttackConfig.RicochetChance,
                ShotSpreadAngle = config.AttackConfig.ShotSpreadAngle,
                TargetType = config.AttackConfig.TargetType,
                HitPolicy = config.AttackConfig.HitPolicy,
                HitInterval = config.AttackConfig.HitInterval,
                AnimationPolicy = config.AttackConfig.AnimationPolicy
            });

            _attackStatusStash.Set(weapon, new AttackStatusComponent
            {
                IsAttacking = false,
                AnimationFinished = true,
                AttackPerformed = false,
                CreationFinished = true
            });

            _projectileCreationStash.Set(weapon, new ProjectileCreationComponent
            {
                ElapsedTime = 0f,
                RemainingCount = 0,
                IsInitialized = false
            });

            EffectCollection effects = new EffectCollection();

            if (config.Effects != null)
            {
                foreach (EffectConfigs.EffectConfig effectConfig in config.Effects)
                {
                    IEffectRuntime effect = EffectRuntimeFactory.Create(effectConfig);

                    if (effect != null)
                        effects.Add(effect);
                }
            }

            _weaponEffectsStash.Set(weapon, new WeaponEffectsComponent
            {
                Effects = effects
            });

            _targetStash.Set(weapon, new TargetComponent());
            _positionStash.Set(weapon, new PositionComponent());
        }
    }
}
