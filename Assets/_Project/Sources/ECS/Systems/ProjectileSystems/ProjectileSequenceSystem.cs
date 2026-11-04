using ECS.AttackComponents;
using ECS.CommonComponents;
using ECS.ProjectileComponents;
using ECS.WeaponComponents;
using Gameplay.View;
using Scellecs.Morpeh;

namespace ECS.ProjectileSystems
{
    public sealed class ProjectileSequenceSystem : ISystem
    {
        private Filter _slashFilter;
        private Filter _ripperFilter;
        private Filter _sequenceFilter;

        private Stash<SwordSlashComponent> _slashStash;
        private Stash<RipperTrajectoryComponent> _ripperTrajectoryStash;
        private Stash<ArcTrajectoryComponent> _trajectoryStash;
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
            _slashFilter = World.Filter
                .With<SwordSlashComponent>()
                .With<ArcTrajectoryComponent>()
                .With<ProjectileComponent>()
                .Build();
            _ripperFilter = World.Filter
                .With<RipperTrajectoryComponent>()
                .With<ProjectileComponent>()
                .Build();
            _sequenceFilter = World.Filter
                .With<WeaponComponent>()
                .With<AttackSequenceComponent>()
                .With<AttackComponent>()
                .With<ProjectileCreationComponent>()
                .With<AttackStatusComponent>()
                .Build();

            _slashStash = World.GetStash<SwordSlashComponent>();
            _ripperTrajectoryStash =
                World.GetStash<RipperTrajectoryComponent>();
            _trajectoryStash = World.GetStash<ArcTrajectoryComponent>();
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
            ReportCompletedSlashes();
            ReportCompletedRipperSweeps();

            foreach (Entity weapon in _sequenceFilter)
            {
                ref AttackSequenceComponent sequence =
                    ref _sequenceStash.Get(weapon);

                if (!sequence.IsActive ||
                    _attackStash.Get(weapon).ProjectileSequencePolicy !=
                    ProjectileSequencePolicy.ComboOnTarget)
                {
                    continue;
                }

                if (!IsLivingTarget(sequence.LockedTarget))
                {
                    if (_weaponStash.Get(weapon).Type == WeaponType.Ripper)
                    {
                        ref ProjectileCreationComponent ripperCreation =
                            ref _creationStash.Get(weapon);
                        if (ripperCreation.RemainingCount <= 0 &&
                            sequence.OutstandingProjectiles <= 0)
                        {
                            FinishCombo(weapon);
                        }

                        continue;
                    }

                    if (sequence.RicochetPending &&
                        IsLivingTarget(sequence.PendingTarget) &&
                        sequence.RicochetsCount <
                        _attackStash.Get(weapon).MaxRicochets)
                    {
                        BeginNextCombo(weapon, sequence.PendingTarget);
                    }
                    else
                    {
                        AbortCombo(weapon);
                    }

                    continue;
                }

                ref ProjectileCreationComponent creation =
                    ref _creationStash.Get(weapon);
                if (creation.RemainingCount > 0 ||
                    sequence.OutstandingProjectiles > 0)
                {
                    continue;
                }

                if (sequence.RicochetPending &&
                    IsLivingTarget(sequence.PendingTarget) &&
                    sequence.RicochetsCount <
                    _attackStash.Get(weapon).MaxRicochets)
                {
                    BeginNextCombo(weapon, sequence.PendingTarget);
                }
                else
                {
                    FinishCombo(weapon);
                }
            }
        }

        private void ReportCompletedSlashes()
        {
            foreach (Entity projectileEntity in _slashFilter)
            {
                ref SwordSlashComponent slash =
                    ref _slashStash.Get(projectileEntity);
                if (slash.CompletionReported)
                    continue;

                ProjectileComponent projectile =
                    _projectileStash.Get(projectileEntity);
                ArcTrajectoryComponent trajectory =
                    _trajectoryStash.Get(projectileEntity);
                if (trajectory.CurrentTime < trajectory.Duration)
                    continue;

                slash.CompletionReported = true;
                if (_sequenceStash.Has(projectile.Weapon))
                {
                    ref AttackSequenceComponent sequence =
                        ref _sequenceStash.Get(projectile.Weapon);
                    if (sequence.IsActive && sequence.Id == slash.SequenceId)
                        sequence.OutstandingProjectiles =
                            System.Math.Max(0, sequence.OutstandingProjectiles - 1);
                }

                RequestCleanup(projectileEntity);
            }
        }

        private void ReportCompletedRipperSweeps()
        {
            foreach (Entity projectileEntity in _ripperFilter)
            {
                ref RipperTrajectoryComponent trajectory =
                    ref _ripperTrajectoryStash.Get(projectileEntity);
                if (trajectory.CompletionReported ||
                    trajectory.CurrentTime < trajectory.Duration)
                {
                    continue;
                }

                trajectory.CompletionReported = true;
                ProjectileComponent projectile =
                    _projectileStash.Get(projectileEntity);

                if (_sequenceStash.Has(projectile.Weapon))
                {
                    ref AttackSequenceComponent sequence =
                        ref _sequenceStash.Get(projectile.Weapon);
                    if (sequence.IsActive &&
                        sequence.Id == trajectory.SequenceId &&
                        trajectory.SequencePolicy ==
                        ProjectileSequencePolicy.ComboOnTarget)
                    {
                        sequence.OutstandingProjectiles =
                            System.Math.Max(
                                0,
                                sequence.OutstandingProjectiles - 1);
                    }
                }

                RequestCleanup(projectileEntity);
            }
        }

        private bool IsLivingTarget(Entity target)
        {
            return target != default &&
                   !World.IsDisposed(target) &&
                   _positionStash.Has(target) &&
                   !_deathStash.Has(target);
        }

        private void BeginNextCombo(Entity weapon, Entity nextTarget)
        {
            ref AttackSequenceComponent sequence =
                ref _sequenceStash.Get(weapon);
            ref ProjectileCreationComponent creation =
                ref _creationStash.Get(weapon);
            ref AttackStatusComponent attackStatus =
                ref _attackStatusStash.Get(weapon);
            AttackComponent attack = _attackStash.Get(weapon);
            int oldSequenceId = sequence.Id;

            CancelProjectiles(weapon, oldSequenceId);

            sequence.Id++;
            sequence.RicochetsCount++;
            sequence.LockedTarget = nextTarget;
            sequence.RicochetPending = false;
            sequence.PendingTarget = default;
            sequence.ReleasesCreated = 0;
            sequence.OutstandingProjectiles = 0;
            if (sequence.VisitedTargets == null)
                sequence.VisitedTargets = new System.Collections.Generic.List<Entity>();
            if (!sequence.VisitedTargets.Contains(nextTarget))
                sequence.VisitedTargets.Add(nextTarget);

            creation.IsInitialized = true;
            creation.RemainingCount = attack.ProjectileCount;
            creation.ElapsedTime = attack.FireRate;
            attackStatus.IsAttacking = true;
            attackStatus.CreationFinished = false;

            if (_targetStash.Has(weapon))
                _targetStash.Get(weapon).Target = nextTarget;
        }

        private void FinishCombo(Entity weapon)
        {
            ref AttackSequenceComponent sequence =
                ref _sequenceStash.Get(weapon);
            ref ProjectileCreationComponent creation =
                ref _creationStash.Get(weapon);

            sequence.IsActive = false;
            sequence.RicochetPending = false;
            sequence.PendingTarget = default;
            creation.IsInitialized = false;
            creation.RemainingCount = 0;
            creation.ElapsedTime = 0f;
            _attackStatusStash.Get(weapon).CreationFinished = true;
        }

        private void AbortCombo(Entity weapon)
        {
            AttackSequenceComponent sequence = _sequenceStash.Get(weapon);
            CancelProjectiles(weapon, sequence.Id);
            FinishCombo(weapon);
        }

        private void CancelProjectiles(Entity weapon, int sequenceId)
        {
            foreach (Entity projectileEntity in _slashFilter)
            {
                ProjectileComponent projectile =
                    _projectileStash.Get(projectileEntity);
                SwordSlashComponent slash = _slashStash.Get(projectileEntity);

                if (projectile.Weapon != weapon ||
                    slash.SequenceId != sequenceId ||
                    projectile.CleanupRequested)
                {
                    continue;
                }

                RequestCleanup(projectileEntity);
            }

            foreach (Entity projectileEntity in _ripperFilter)
            {
                ProjectileComponent projectile =
                    _projectileStash.Get(projectileEntity);
                RipperTrajectoryComponent trajectory =
                    _ripperTrajectoryStash.Get(projectileEntity);

                if (projectile.Weapon != weapon ||
                    trajectory.SequenceId != sequenceId ||
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
