using System.Collections.Generic;
using ECS.AttackComponents;
using ECS.CommonComponents;
using ECS.ProjectileComponents;
using ECS.WeaponComponents;
using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.ProjectileSystems
{
    public sealed class ProjectileReleaseScheduler
    {
        internal const float BurstTimingTolerance = 0.00001f;

        private readonly ProjectileFactory _factory;
        private readonly ProjectileRelease _release;
        private readonly Stash<WeaponComponent> _weaponStash;
        private readonly Stash<AttackComponent> _attackStash;
        private readonly Stash<AttackSequenceComponent> _sequenceStash;
        private readonly Stash<PositionComponent> _positionStash;
        private readonly Stash<MovementComponent> _movementStash;
        private readonly Stash<ProjectileCreationComponent> _creationStash;
        private readonly Stash<AttackStatusComponent> _attackStatusStash;

        public ProjectileReleaseScheduler(World world, ProjectileFactory factory)
        {
            _factory = factory;
            _release = new ProjectileRelease(world, factory);
            _weaponStash = world.GetStash<WeaponComponent>();
            _attackStash = world.GetStash<AttackComponent>();
            _sequenceStash = world.GetStash<AttackSequenceComponent>();
            _positionStash = world.GetStash<PositionComponent>();
            _movementStash = world.GetStash<MovementComponent>();
            _creationStash = world.GetStash<ProjectileCreationComponent>();
            _attackStatusStash = world.GetStash<AttackStatusComponent>();
        }

        public void Begin(Entity weapon, float currentUpdateDeltaTime)
        {
            if (!_weaponStash.Has(weapon) ||
                !_attackStash.Has(weapon) ||
                !_positionStash.Has(weapon) ||
                !_creationStash.Has(weapon) ||
                !_attackStatusStash.Has(weapon))
            {
                return;
            }

            WeaponType weaponType = _weaponStash.Get(weapon).Type;

            if (!_factory.TryGetDefinition(weaponType, out _))
            {
                FinishCreation(weapon);
                return;
            }

            ref AttackComponent attack = ref _attackStash.Get(weapon);
            ref AttackSequenceComponent sequence =
                ref _sequenceStash.Get(weapon);
            ref ProjectileCreationComponent creation =
                ref _creationStash.Get(weapon);
            ref AttackStatusComponent attackStatus =
                ref _attackStatusStash.Get(weapon);

            bool usesSwordAttack = weaponType.UsesSwordAttack();
            int cyclesPerAttack = Mathf.Max(0, attack.CyclesPerAttack);
            if (weaponType == WeaponType.Axe || usesSwordAttack)
                cyclesPerAttack = Mathf.Min(cyclesPerAttack, CardinalSectors.Count);

            // CreationSystem also updates a newly started attack in this tick.
            creation = new ProjectileCreationComponent
            {
                ElapsedTime = weaponType is WeaponType.Armageddon6 or WeaponType.ShrapnelStorm
                    ? -currentUpdateDeltaTime
                    : 0f,
                RemainingCount = usesSwordAttack ? Mathf.Max(0, cyclesPerAttack - 1) : cyclesPerAttack,
                IsInitialized = true
            };

            sequence.SwordCycles?.Clear();
            sequence.PendingRicochetTargets?.Clear();
            sequence.VisitedTargets?.Clear();
            sequence = new AttackSequenceComponent
            {
                Id = sequence.Id + 1,
                IsActive = true,
                AssignedSectorCycle = -1,
                AssignedSectorIndex = -1,
                SwordCycles = usesSwordAttack ? sequence.SwordCycles ?? new List<SwordAttackCycleState>() : null,
                PendingRicochetTargets = sequence.PendingRicochetTargets ?? new List<Entity>(),
                VisitedTargets = sequence.VisitedTargets ?? new List<Entity>()
            };

            attackStatus.CreationFinished = false;

            if (cyclesPerAttack <= 0)
            {
                StopCreation(weapon);
                return;
            }

            if (weaponType == WeaponType.Volcano)
            {
                UpdateVolcano(weapon, 0f);
                if (creation.RemainingCount > 0)
                    creation.ElapsedTime += currentUpdateDeltaTime;
                return;
            }

            if (!CreateRelease(weapon))
                return;

            if (usesSwordAttack)
            {
                if (attack.CycleInterval <= 0f)
                {
                    while (creation.RemainingCount > 0)
                    {
                        creation.RemainingCount--;
                        if (!CreateRelease(weapon))
                            return;
                    }
                }
                else if (creation.RemainingCount > 0)
                {
                    creation.IsWaitingForCycle = true;
                    creation.ElapsedTime = 0f;
                }
                return;
            }

            creation.RemainingCount--;

            if (creation.RemainingCount <= 0)
            {
                StopCreation(weapon);
            }
        }

        public void Update(Entity weapon, float deltaTime)
        {
            if (!_creationStash.Has(weapon) ||
                !_attackStatusStash.Has(weapon) ||
                !_attackStash.Has(weapon))
            {
                return;
            }

            ref ProjectileCreationComponent creation =
                ref _creationStash.Get(weapon);
            ref AttackStatusComponent attackStatus =
                ref _attackStatusStash.Get(weapon);

            if (!creation.IsInitialized || attackStatus.CreationFinished)
                return;

            ref AttackComponent attack = ref _attackStash.Get(weapon);

            if (!_weaponStash.Has(weapon))
            {
                StopCreation(weapon);
                return;
            }

            WeaponType weaponType = _weaponStash.Get(weapon).Type;
            if (weaponType.UsesSwordAttack())
            {
                UpdateSwordCycles(weapon, deltaTime);
                return;
            }

            if (weaponType == WeaponType.Volcano)
            {
                UpdateVolcano(weapon, deltaTime);
                return;
            }

            if (!_factory.TryGetDefinition(weaponType, out _))
            {
                StopCreation(weapon);
                return;
            }

            if (creation.RemainingCount <= 0)
            {
                StopCreation(weapon);
                return;
            }

            float timingTolerance = weaponType is WeaponType.Armageddon6 or WeaponType.ShrapnelStorm
                ? BurstTimingTolerance
                : 0f;
            float interval = attack.CycleInterval;
            if (interval > 0f)
                creation.ElapsedTime += deltaTime;

            while (creation.RemainingCount > 0 &&
                   (interval <= 0f || creation.ElapsedTime + timingTolerance >= interval))
            {
                if (interval > 0f)
                    creation.ElapsedTime -= interval;
                if (!CreateRelease(weapon))
                    return;

                creation.RemainingCount--;
            }

            if (creation.RemainingCount <= 0 &&
                attack.CompletionPolicy != AttackCompletionPolicy.AfterTrajectories)
            {
                FinishCreation(weapon);
            }
        }

        public void Cancel(Entity weapon)
        {
            FinishCreation(weapon);
        }

        private void UpdateVolcano(Entity weapon, float deltaTime)
        {
            ref ProjectileCreationComponent creation = ref _creationStash.Get(weapon);
            if (creation.RemainingCount <= 0)
                return;

            ref AttackComponent attack = ref _attackStash.Get(weapon);
            if (attack.TargetTypes == null || attack.TargetTypes.Length == 0 ||
                !_positionStash.Has(weapon) ||
                !_factory.TryGetDefinition(WeaponType.Volcano, out ProjectileFactory.Definition definition) ||
                definition.Creator is not StraightProjectileCreator straightCreator)
            {
                FinishCreation(weapon);
                return;
            }

            Entity owner = _weaponStash.Get(weapon).Owner;
            Vector3 center = _movementStash.Has(owner)
                ? _movementStash.Get(owner).LastDirection
                : Vector3.zero;
            if (center.sqrMagnitude <= 0.0001f)
                center = Vector3.right;

            int shotCount = Mathf.Max(1, attack.ShotsPerCycle);
            float shotInterval = Mathf.Max(0f, attack.ShotInterval);
            float cycleInterval = Mathf.Max(0f, attack.CycleInterval);
            ref AttackSequenceComponent sequence = ref _sequenceStash.Get(weapon);

            // Count down to the next shot so frame overruns carry into the following one.
            creation.ElapsedTime -= deltaTime;
            while (creation.ElapsedTime <= BurstTimingTolerance && creation.RemainingCount > 0)
            {
                Vector3 direction = GetVolcanoDirection(
                    center, creation.ShotsCreatedInCycle, shotCount, attack.Pattern.SpreadAngle);
                Entity projectile = _factory.Create(
                    weapon, definition, direction, sequence.Id, sequence.ReleasesCreated);
                straightCreator.Launch(projectile, direction);
                if (attack.CompletionPolicy == AttackCompletionPolicy.AfterTrajectories)
                    sequence.OutstandingProjectiles++;

                creation.ShotsCreatedInCycle++;
                if (creation.ShotsCreatedInCycle < shotCount)
                {
                    creation.ElapsedTime += shotInterval;
                    continue;
                }

                creation.ShotsCreatedInCycle = 0;
                creation.RemainingCount--;
                creation.ElapsedTime += cycleInterval;
                sequence.ReleasesCreated++;
            }

            if (creation.RemainingCount <= 0)
                StopCreation(weapon);
        }

        private static Vector3 GetVolcanoDirection(
            Vector3 center, int shotIndex, int shotCount, float spreadAngle)
        {
            float progress = shotIndex / (float)Mathf.Max(1, shotCount - 1);
            float halfSpread = Mathf.Clamp(spreadAngle, 0f, 40f) * 0.5f;
            float offset = Mathf.Sin(progress * (2f * Mathf.PI)) * halfSpread;
            float angle = Mathf.Atan2(center.y, center.x) + offset * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
        }

        private void UpdateSwordCycles(Entity weapon, float deltaTime)
        {
            ref ProjectileCreationComponent creation =
                ref _creationStash.Get(weapon);
            if (!creation.IsWaitingForCycle)
                return;

            ref AttackComponent attack = ref _attackStash.Get(weapon);
            creation.ElapsedTime += deltaTime;
            if (attack.CycleInterval > 0f &&
                creation.ElapsedTime < attack.CycleInterval)
            {
                return;
            }

            bool comboRestart = creation.IsComboRestart;
            if (!comboRestart && _sequenceStash.Has(weapon))
            {
                ref AttackSequenceComponent sequence =
                    ref _sequenceStash.Get(weapon);
                sequence.LockedTarget = default;
            }

            if (!CreateRelease(weapon))
                return;

            if (!comboRestart)
                creation.RemainingCount--;

            creation.ElapsedTime = 0f;
            creation.IsWaitingForCycle = !comboRestart &&
                                         creation.RemainingCount > 0;
            creation.IsComboRestart = false;
        }

        private bool CreateRelease(Entity weapon)
        {
            if (_release.TryCreate(weapon))
                return true;

            StopCreation(weapon);
            return false;
        }

        private void FinishCreation(Entity weapon)
        {
            if (_creationStash.Has(weapon))
                _creationStash.Get(weapon) = default;

            if (_attackStatusStash.Has(weapon))
                _attackStatusStash.Get(weapon).CreationFinished = true;

            if (_sequenceStash.Has(weapon))
                _sequenceStash.Get(weapon).IsActive = false;
        }

        private void StopCreation(Entity weapon)
        {
            if (_attackStash.Has(weapon) &&
                _attackStash.Get(weapon).CompletionPolicy ==
                    AttackCompletionPolicy.AfterTrajectories)
            {
                if (_creationStash.Has(weapon))
                {
                    // The sequence system still owns completion while trajectories are active.
                    ref ProjectileCreationComponent creation = ref _creationStash.Get(weapon);
                    creation = new ProjectileCreationComponent { IsInitialized = creation.IsInitialized };
                }
                return;
            }

            FinishCreation(weapon);
        }
    }
}
