using ECS.AttackComponents;
using ECS.CommonComponents;
using ECS.ProjectileComponents;
using ECS.WeaponComponents;
using Gameplay.View;
using Scellecs.Morpeh;
using System.Collections.Generic;
using UnityEngine;

namespace ECS.ProjectileSystems
{
    public sealed class AttackSequenceSystem : ISystem
    {
        private Filter _projectileFilter;
        private Filter _sequenceFilter;

        private Stash<ArcTrajectoryComponent> _arcTrajectoryStash;
        private Stash<SwordThrustComponent> _thrustStash;
        private Stash<RipperTrajectoryComponent> _ripperTrajectoryStash;
        private Stash<ProjectileComponent> _projectileStash;
        private Stash<AttackSequenceComponent> _sequenceStash;
        private Stash<WeaponComponent> _weaponStash;
        private Stash<AttackComponent> _attackStash;
        private Stash<ProjectileCreationComponent> _creationStash;
        private Stash<AttackStatusComponent> _attackStatusStash;
        private Stash<TargetComponent> _targetStash;
        private Stash<DeathComponent> _deathStash;
        private Stash<PositionComponent> _positionStash;
        private Stash<CleanupRequest> _cleanupRequestStash;
        private Stash<ProjectileViewComponent> _viewStash;

        public World World { get; set; }

        public void OnAwake()
        {
            _projectileFilter = World.Filter
                .With<ProjectileComponent>()
                .Build();
            _sequenceFilter = World.Filter
                .With<WeaponComponent>()
                .With<AttackSequenceComponent>()
                .With<AttackComponent>()
                .With<ProjectileCreationComponent>()
                .With<AttackStatusComponent>()
                .Build();

            _arcTrajectoryStash = World.GetStash<ArcTrajectoryComponent>();
            _thrustStash = World.GetStash<SwordThrustComponent>();
            _ripperTrajectoryStash =
                World.GetStash<RipperTrajectoryComponent>();
            _projectileStash = World.GetStash<ProjectileComponent>();
            _sequenceStash = World.GetStash<AttackSequenceComponent>();
            _weaponStash = World.GetStash<WeaponComponent>();
            _attackStash = World.GetStash<AttackComponent>();
            _creationStash = World.GetStash<ProjectileCreationComponent>();
            _attackStatusStash = World.GetStash<AttackStatusComponent>();
            _targetStash = World.GetStash<TargetComponent>();
            _deathStash = World.GetStash<DeathComponent>();
            _positionStash = World.GetStash<PositionComponent>();
            _cleanupRequestStash = World.GetStash<CleanupRequest>();
            _viewStash = World.GetStash<ProjectileViewComponent>();
        }

        public void OnUpdate(float deltaTime)
        {
            ReportCompletedProjectiles();

            foreach (Entity weapon in _sequenceFilter)
            {
                ref AttackSequenceComponent sequence =
                    ref _sequenceStash.Get(weapon);
                Entity pendingTarget;

                AttackComponent attack = _attackStash.Get(weapon);
                if (_weaponStash.Get(weapon).Type == WeaponType.Sword &&
                    attack.CompletionPolicy ==
                        AttackCompletionPolicy.AfterTrajectories)
                {
                    UpdateSwordSeries(weapon, ref sequence);
                    continue;
                }

                if (!sequence.IsActive ||
                    attack.CompletionPolicy !=
                    AttackCompletionPolicy.AfterTrajectories)
                {
                    continue;
                }

                ref ProjectileCreationComponent creation =
                    ref _creationStash.Get(weapon);
                if (attack.RicochetPolicy != RicochetPolicyType.AttackSeries)
                {
                    if (creation.RemainingCount <= 0 &&
                        sequence.OutstandingProjectiles <= 0)
                    {
                        FinishSequence(weapon);
                    }

                    continue;
                }

                if (creation.RemainingCount > 0 ||
                    sequence.OutstandingProjectiles > 0)
                {
                    continue;
                }

                if (sequence.RicochetPending &&
                    sequence.RicochetsCount <
                    attack.MaxRicochets &&
                    TryGetLivingPendingTarget(
                        ref sequence,
                        out pendingTarget))
                {
                    BeginRicochetSequence(weapon, pendingTarget);
                }
                else
                {
                    FinishSequence(weapon);
                }
            }
        }

        private void UpdateSwordSeries(
            Entity weapon,
            ref AttackSequenceComponent sequence)
        {
            if (!sequence.IsActive ||
                _attackStash.Get(weapon).CompletionPolicy !=
                AttackCompletionPolicy.AfterTrajectories)
            {
                return;
            }

            ref ProjectileCreationComponent creation =
                ref _creationStash.Get(weapon);
            if (creation.IsWaitingForCycle)
            {
                return;
            }

            AttackComponent attack = _attackStash.Get(weapon);
            if (!sequence.SwordMainSeriesFinished)
            {
                if (creation.RemainingCount > 0)
                    return;

                if (sequence.OutstandingProjectiles > 0)
                    return;

                sequence.SwordMainSeriesFinished = true;
            }
            else if (sequence.OutstandingProjectiles > 0)
            {
                return;
            }

            if (sequence.RicochetPending &&
                sequence.RicochetsCount < attack.MaxRicochets &&
                TryGetLivingPendingSwordRicochet(
                    ref sequence,
                    out SwordAttackCycleState swordRicochet))
            {
                sequence.SwordRicochetOrigin =
                    swordRicochet.LastProjectileEndPosition;
                sequence.SwordStartSector = swordRicochet.SwordFacingSector;
                sequence.HasSwordStartSector = true;
                BeginRicochetSequence(
                    weapon,
                    swordRicochet.PendingRicochetTarget);
                return;
            }

            FinishSequence(weapon);
        }

        private void ReportCompletedProjectiles()
        {
            foreach (Entity projectileEntity in _projectileFilter)
            {
                ref ProjectileComponent projectile =
                    ref _projectileStash.Get(projectileEntity);
                if (projectile.AttackCompletionReported)
                    continue;

                bool trajectoryCompleted = false;
                bool cleanupAfterCompletion = false;
                if (_thrustStash.Has(projectileEntity))
                {
                    SwordThrustComponent thrust =
                        _thrustStash.Get(projectileEntity);
                    trajectoryCompleted = thrust.CurrentTime >= thrust.Duration;
                    cleanupAfterCompletion = true;
                }
                else if (_ripperTrajectoryStash.Has(projectileEntity))
                {
                    RipperTrajectoryComponent trajectory =
                        _ripperTrajectoryStash.Get(projectileEntity);
                    trajectoryCompleted =
                        trajectory.CurrentTime >= trajectory.Duration;
                    cleanupAfterCompletion = true;
                }
                else if (_arcTrajectoryStash.Has(projectileEntity))
                {
                    ArcTrajectoryComponent trajectory =
                        _arcTrajectoryStash.Get(projectileEntity);
                    trajectoryCompleted =
                        trajectory.CurrentTime >= trajectory.Duration;
                }

                if (!projectile.CleanupRequested && !trajectoryCompleted)
                    continue;

                if (trajectoryCompleted &&
                    projectile.WeaponType == WeaponType.Sword &&
                    _thrustStash.Has(projectileEntity) &&
                    _positionStash.Has(projectileEntity) &&
                    _sequenceStash.Has(projectile.Weapon) &&
                    _attackStash.Has(projectile.Weapon))
                {
                    ref AttackSequenceComponent swordSequence =
                        ref _sequenceStash.Get(projectile.Weapon);
                    if (swordSequence.IsActive &&
                        swordSequence.Id == projectile.SequenceId &&
                        _attackStash.Get(projectile.Weapon).CompletionPolicy ==
                            AttackCompletionPolicy.AfterTrajectories)
                    {
                        UpdateSwordCycleEndpoint(
                            ref swordSequence,
                            projectile,
                            _positionStash.Get(projectileEntity).Position);
                    }
                }

                projectile.AttackCompletionReported = true;
                if (_sequenceStash.Has(projectile.Weapon) &&
                    _attackStash.Has(projectile.Weapon))
                {
                    ref AttackSequenceComponent sequence =
                        ref _sequenceStash.Get(projectile.Weapon);
                    if (sequence.IsActive &&
                        sequence.Id == projectile.SequenceId &&
                        _attackStash.Get(projectile.Weapon).CompletionPolicy ==
                            AttackCompletionPolicy.AfterTrajectories)
                    {
                        sequence.OutstandingProjectiles =
                            System.Math.Max(0, sequence.OutstandingProjectiles - 1);
                    }
                }

                if (cleanupAfterCompletion)
                    RequestCleanup(projectileEntity);
            }
        }

        private static void UpdateSwordCycleEndpoint(
            ref AttackSequenceComponent sequence,
            ProjectileComponent projectile,
            Vector3 endPosition)
        {
            if (sequence.SwordCycles == null)
                return;

            int stateIndex = sequence.SwordCycles.FindIndex(
                state => state.SequenceId == projectile.SequenceId &&
                         state.CycleIndex == projectile.CycleIndex);
            if (stateIndex < 0)
                return;

            SwordAttackCycleState cycle = sequence.SwordCycles[stateIndex];
            if (cycle.HasLastProjectileEndPosition &&
                projectile.SwingIndex <= cycle.LastProjectileSwingIndex)
            {
                return;
            }

            cycle.LastProjectileSwingIndex = projectile.SwingIndex;
            cycle.LastProjectileEndPosition = endPosition;
            cycle.HasLastProjectileEndPosition = true;
            sequence.SwordCycles[stateIndex] = cycle;
        }

        private bool IsLivingTarget(Entity target)
        {
            return target != default &&
                   !World.IsDisposed(target) &&
                   _positionStash.Has(target) &&
                   !_deathStash.Has(target);
        }

        private bool TryGetLivingPendingTarget(
            ref AttackSequenceComponent sequence,
            out Entity target)
        {
            List<Entity> pendingTargets = sequence.PendingRicochetTargets;
            if (pendingTargets != null)
            {
                for (int i = 0; i < pendingTargets.Count; i++)
                {
                    if (!IsLivingTarget(pendingTargets[i]))
                        continue;

                    target = pendingTargets[i];
                    sequence.PendingTarget = target;
                    return true;
                }
            }

            if (IsLivingTarget(sequence.PendingTarget))
            {
                target = sequence.PendingTarget;
                return true;
            }

            target = default;
            return false;
        }

        private bool TryGetLivingPendingSwordRicochet(
            ref AttackSequenceComponent sequence,
            out SwordAttackCycleState swordRicochet)
        {
            List<SwordAttackCycleState> swordCycles = sequence.SwordCycles;
            if (swordCycles != null)
            {
                for (int i = 0; i < swordCycles.Count; i++)
                {
                    SwordAttackCycleState cycle = swordCycles[i];
                    if (!cycle.HasPendingRicochet)
                        continue;

                    if (!cycle.HasLastProjectileEndPosition ||
                        !IsLivingTarget(cycle.PendingRicochetTarget))
                    {
                        cycle.HasPendingRicochet = false;
                        cycle.PendingRicochetTarget = default;
                        swordCycles[i] = cycle;
                        continue;
                    }

                    Entity pendingTarget = cycle.PendingRicochetTarget;
                    cycle.HasPendingRicochet = false;
                    cycle.PendingRicochetTarget = default;
                    swordCycles[i] = cycle;
                    cycle.PendingRicochetTarget = pendingTarget;
                    swordRicochet = cycle;
                    sequence.RicochetPending =
                        HasPendingSwordRicochet(swordCycles);
                    return true;
                }
            }

            sequence.RicochetPending =
                swordCycles != null && HasPendingSwordRicochet(swordCycles);
            swordRicochet = default;
            return false;
        }

        private static bool HasPendingSwordRicochet(
            List<SwordAttackCycleState> swordCycles)
        {
            for (int i = 0; i < swordCycles.Count; i++)
            {
                if (swordCycles[i].HasPendingRicochet)
                    return true;
            }

            return false;
        }

        private void BeginRicochetSequence(Entity weapon, Entity nextTarget)
        {
            ref AttackSequenceComponent sequence =
                ref _sequenceStash.Get(weapon);
            ref ProjectileCreationComponent creation =
                ref _creationStash.Get(weapon);
            ref AttackStatusComponent attackStatus =
                ref _attackStatusStash.Get(weapon);
            AttackComponent attack = _attackStash.Get(weapon);
            int oldSequenceId = sequence.Id;
            bool isSword = _weaponStash.Get(weapon).Type == WeaponType.Sword;

            CancelProjectiles(weapon, oldSequenceId);

            sequence.Id++;
            sequence.RicochetsCount++;
            sequence.LockedTarget = nextTarget;
            sequence.UsedSectorMask = 0;
            sequence.AssignedSectorCycle = -1;
            sequence.AssignedSectorIndex = -1;
            sequence.AxeSweepClockwise = false;
            sequence.HasAxeSweepDirection = false;
            sequence.RicochetPending = false;
            sequence.PendingTarget = default;
            if (sequence.PendingRicochetTargets != null)
            {
                if (isSword)
                    sequence.PendingRicochetTargets.Remove(nextTarget);
                else
                    sequence.PendingRicochetTargets.Clear();
            }
            sequence.ReleasesCreated = 0;
            sequence.OutstandingProjectiles = 0;
            if (sequence.VisitedTargets == null)
                sequence.VisitedTargets = new System.Collections.Generic.List<Entity>();
            if (!sequence.VisitedTargets.Contains(nextTarget))
                sequence.VisitedTargets.Add(nextTarget);

            creation.IsInitialized = true;
            if (isSword)
            {
                sequence.SwordRicochetCycle = true;
                sequence.RicochetPending = sequence.SwordCycles != null &&
                    HasPendingSwordRicochet(sequence.SwordCycles);
                creation.IsWaitingForCycle = true;
                creation.IsComboRestart = true;
                creation.ElapsedTime = 0f;
                creation.RemainingCount = 0;
            }
            else
            {
                creation.RemainingCount = attack.CyclesPerAttack;
                creation.ElapsedTime = attack.CycleInterval;
            }
            attackStatus.IsAttacking = true;
            attackStatus.CreationFinished = false;

            if (_targetStash.Has(weapon))
                _targetStash.Get(weapon).Target = nextTarget;
        }

        private void FinishSequence(Entity weapon)
        {
            ref AttackSequenceComponent sequence =
                ref _sequenceStash.Get(weapon);
            ref ProjectileCreationComponent creation =
                ref _creationStash.Get(weapon);

            sequence.IsActive = false;
            sequence.SwordRicochetCycle = false;
            sequence.RicochetPending = false;
            sequence.PendingTarget = default;
            if (sequence.PendingRicochetTargets != null)
                sequence.PendingRicochetTargets.Clear();
            if (sequence.SwordCycles != null)
                sequence.SwordCycles.Clear();
            creation.IsInitialized = false;
            creation.RemainingCount = 0;
            creation.ElapsedTime = 0f;
            creation.IsWaitingForCycle = false;
            creation.IsComboRestart = false;
            _attackStatusStash.Get(weapon).CreationFinished = true;
        }

        private void CancelProjectiles(Entity weapon, int sequenceId)
        {
            foreach (Entity projectileEntity in _projectileFilter)
            {
                ProjectileComponent projectile =
                    _projectileStash.Get(projectileEntity);

                if (projectile.Weapon != weapon ||
                    projectile.SequenceId != sequenceId ||
                    projectile.CleanupRequested)
                {
                    continue;
                }

                RequestCleanup(projectileEntity);
            }
        }

        private void RequestCleanup(Entity projectileEntity)
        {
            ref ProjectileComponent projectile =
                ref _projectileStash.Get(projectileEntity);
            if (projectile.CleanupRequested)
                return;

            projectile.CleanupRequested = true;
            if (_viewStash.Has(projectileEntity))
            {
                AnimatedView view = _viewStash.Get(projectileEntity).View;
                if (view != null)
                    view.SetActive(false);
            }

            Entity request = World.CreateEntity();
            _cleanupRequestStash.Set(request, new CleanupRequest
            {
                Candidate = projectileEntity
            });
        }

        public void Dispose() { }
    }
}
