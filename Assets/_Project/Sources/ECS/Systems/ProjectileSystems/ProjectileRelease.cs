using System.Collections.Generic;
using ECS.AttackComponents;
using ECS.CommonComponents;
using ECS.ProjectileComponents;
using ECS.WeaponComponents;
using Gameplay.View;
using ProjectileConfigs;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.ProjectileSystems
{
    internal sealed class ProjectileRelease
    {
        private readonly World _world;
        private readonly ProjectileFactory _factory;
        private readonly Stash<WeaponComponent> _weaponStash;
        private readonly Stash<AttackComponent> _attackStash;
        private readonly Stash<AttackSequenceComponent> _sequenceStash;
        private readonly Stash<RipperSequenceComponent> _ripperSequenceStash;
        private readonly Stash<PositionComponent> _positionStash;
        private readonly Stash<ProjectileComponent> _projectileStash;
        private readonly Stash<ProjectileCreationComponent> _creationStash;
        private readonly Stash<TargetComponent> _targetStash;
        private readonly Stash<DeathComponent> _deathStash;
        private readonly Stash<TurretComponent> _turretStash;

        public ProjectileRelease(World world, ProjectileFactory factory)
        {
            _world = world;
            _factory = factory;
            _weaponStash = world.GetStash<WeaponComponent>();
            _attackStash = world.GetStash<AttackComponent>();
            _sequenceStash = world.GetStash<AttackSequenceComponent>();
            _ripperSequenceStash = world.GetStash<RipperSequenceComponent>();
            _positionStash = world.GetStash<PositionComponent>();
            _projectileStash = world.GetStash<ProjectileComponent>();
            _creationStash = world.GetStash<ProjectileCreationComponent>();
            _targetStash = world.GetStash<TargetComponent>();
            _deathStash = world.GetStash<DeathComponent>();
            _turretStash = world.GetStash<TurretComponent>();
        }

        public bool TryCreate(Entity weapon)
        {
            if (!_weaponStash.Has(weapon) ||
                !_attackStash.Has(weapon) ||
                !_positionStash.Has(weapon))
            {
                return false;
            }

            WeaponType weaponType = _weaponStash.Get(weapon).Type;
            if (!_factory.TryGetDefinition(weaponType, out ProjectileFactory.Definition definition))
            {
                return false;
            }

            ref AttackComponent attack = ref _attackStash.Get(weapon);

            ref AttackSequenceComponent sequence =
                ref _sequenceStash.Get(weapon);
            ref ProjectileCreationComponent creation =
                ref _creationStash.Get(weapon);

            Entity target;
            if (attack.RicochetPolicy == RicochetPolicyType.AttackSeries &&
                sequence.LockedTarget != default)
            {
                target = sequence.LockedTarget;
            }
            else if (!attack.Pattern.SelectTargetPerCycle &&
                     sequence.AttackTarget != default)
            {
                target = sequence.AttackTarget;
            }
            else if (!attack.Pattern.SelectTargetPerCycle &&
                     sequence.HasFallbackDirection)
            {
                target = default;
            }
            else
            {
                target = _targetStash.Has(weapon)
                    ? _targetStash.Get(weapon).Target
                    : default;
            }

            TurretView turret = _turretStash.Has(weapon) ? _turretStash.Get(weapon).View : null;
            Vector3 direction;
            if (turret != null)
            {
                direction = turret.AimDirection;
            }
            else if (!TryGetDirection(weapon, target, out direction))
            {
                if (attack.Pattern.RandomDirectionFallback)
                {
                    if (!attack.Pattern.SelectTargetPerCycle &&
                        sequence.HasFallbackDirection)
                    {
                        direction = sequence.FallbackDirection;
                    }
                    else
                    {
                        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                        direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);

                        if (!attack.Pattern.SelectTargetPerCycle)
                        {
                            sequence.FallbackDirection = direction;
                            sequence.HasFallbackDirection = true;
                        }
                    }
                }
                else
                {
                    bool hasCachedRipperDirection =
                        weaponType.UsesRipperAttack() &&
                        _ripperSequenceStash.Has(weapon) &&
                        _ripperSequenceStash.Get(weapon).IsInitialized &&
                        _ripperSequenceStash.Get(weapon).SequenceId == sequence.Id;

                    if (!hasCachedRipperDirection)
                        return false;

                    direction = _ripperSequenceStash.Get(weapon).AttackDirection;
                }
            }

            if (weaponType.UsesSwordAttack())
            {
                int sector = GetSwordSector(ref sequence, direction);
                direction = CardinalSectors.ToDirection(sector);
                RegisterSwordCycle(ref sequence, sequence.Id, sequence.ReleasesCreated, sector);
            }

            int projectileCount = Mathf.Max(1, attack.ShotsPerCycle);
            Entity[] projectiles = new Entity[projectileCount];

            if (attack.RicochetPolicy == RicochetPolicyType.AttackSeries &&
                sequence.LockedTarget == default)
            {
                sequence.LockedTarget = target;
            }

            Vector3? origin = turret != null ? turret.ShotOrigin : null;
            for (int i = 0; i < projectiles.Length; i++)
            {
                projectiles[i] = _factory.Create(
                    weapon,
                    definition,
                    direction,
                    sequence.Id,
                    sequence.ReleasesCreated,
                    origin);
            }

            definition.Creator?.Create(weapon, projectiles);

            float shotInterval = GetShotInterval(weaponType, attack);
            if (turret != null)
            {
                if (weaponType == WeaponType.ElectricTurret)
                    turret.PlayDischarge(attack.ProjectileLifetime);
                else
                    turret.PlayBurst(shotInterval, projectiles.Length);
            }

            for (int i = 1; i < projectiles.Length; i++)
            {
                float startDelay = shotInterval * i;
                if (startDelay <= 0f || !_projectileStash.Has(projectiles[i]))
                    continue;

                ref ProjectileComponent shot = ref _projectileStash.Get(projectiles[i]);
                shot.CurrentLifetime = -startDelay;
                shot.CollisionEnabled = false;
            }

            if (definition.Config is not ArmageddonProjectileConfig &&
                target != default &&
                !sequence.VisitedTargets.Contains(target))
                sequence.VisitedTargets.Add(target);

            if (target != default &&
                !attack.Pattern.SelectTargetPerCycle &&
                sequence.AttackTarget == default)
            {
                sequence.AttackTarget = target;
            }

            sequence.ReleasesCreated++;
            if (weaponType.UsesSwordAttack() &&
                creation.RemainingCount > 0 &&
                !creation.IsComboRestart)
            {
                sequence.LockedTarget = default;
            }
            if (attack.CompletionPolicy ==
                AttackCompletionPolicy.AfterTrajectories)
            {
                sequence.OutstandingProjectiles += projectiles.Length;
            }

            return true;
        }

        private static float GetShotInterval(WeaponType type, in AttackComponent attack)
        {
            if (type.UsesRipperAttack())
                return Mathf.Max(attack.ShotInterval, attack.Trajectory.Duration * RipperProjectileCreator.MotionDurationMultiplier);

            return type == WeaponType.Axe
                ? Mathf.Max(attack.ShotInterval, attack.Trajectory.Duration)
                : attack.ShotInterval;
        }

        private bool TryGetDirection(
            Entity weapon,
            Entity target,
            out Vector3 direction)
        {
            direction = Vector3.zero;

            if (weapon == default ||
                _world.IsDisposed(weapon) ||
                !_positionStash.Has(weapon) ||
                target == default ||
                _world.IsDisposed(target) ||
                !_positionStash.Has(target) ||
                _deathStash.Has(target))
            {
                return false;
            }

            direction =
                _positionStash.Get(target).Position -
                _positionStash.Get(weapon).Position;

            direction.z = 0f;

            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = Vector3.zero;
                return false;
            }

            direction.Normalize();
            return true;
        }

        private static int GetSwordSector(
            ref AttackSequenceComponent sequence,
            Vector3 targetDirection)
        {
            if (sequence.SwordRicochetCycle)
            {
                int offset = Random.Range(0, CardinalSectors.Count);
                return (sequence.SwordStartSector + offset) % CardinalSectors.Count;
            }

            int preferredSector = CardinalSectors.FromDirection(targetDirection);
            int sector;
            if (!sequence.HasSwordStartSector)
            {
                sector = preferredSector;
                sequence.SwordStartSector = sector;
                sequence.HasSwordStartSector = true;
            }
            else
            {
                sector = CardinalSectors.FindAvailable(
                    preferredSector,
                    sequence.SwordUsedSectorMask);
                if (sector < 0)
                    sector = preferredSector;
            }

            sequence.SwordUsedSectorMask |= 1 << sector;
            return sector;
        }

        private static void RegisterSwordCycle(
            ref AttackSequenceComponent sequence,
            int sequenceId,
            int cycleIndex,
            int swordFacingSector)
        {
            if (sequence.SwordCycles == null)
                sequence.SwordCycles = new List<SwordAttackCycleState>();

            int stateIndex = sequence.SwordCycles.FindIndex(
                state => state.SequenceId == sequenceId &&
                         state.CycleIndex == cycleIndex);
            if (stateIndex >= 0)
                return;

            sequence.SwordCycles.Add(new SwordAttackCycleState
            {
                SequenceId = sequenceId,
                CycleIndex = cycleIndex,
                SwordFacingSector = swordFacingSector,
                LastProjectileSwingIndex = -1
            });
        }
    }
}
