using ECS.AttackComponents;
using ECS.ProjectileComponents;
using ECS.WeaponComponents;
using Scellecs.Morpeh;

namespace ECS.ProjectileSystems
{
    public sealed class ProjectileCreationSystem : ISystem
    {
        public World World { get; set; }

        private Filter _requestFilter;
        private Filter _creationFilter;

        private Stash<ProjectileCreationRequestComponent> _requestStash;
        private Stash<ProjectileCreationComponent> _creationStash;
        private Stash<AttackStatusComponent> _attackStatusStash;

        private readonly ProjectileFactory _projectileFactory;

        public ProjectileCreationSystem(ProjectileFactory projectileFactory)
        {
            _projectileFactory = projectileFactory;
        }

        public void OnAwake()
        {
            _requestFilter = World.Filter
                .With<WeaponComponent>()
                .With<ProjectileCreationRequestComponent>()
                .With<ProjectileCreationComponent>()
                .With<AttackStatusComponent>()
                .Build();

            _creationFilter = World.Filter
                .With<WeaponComponent>()
                .With<ProjectileCreationComponent>()
                .With<AttackStatusComponent>()
                .Build();

            _requestStash = World.GetStash<ProjectileCreationRequestComponent>();
            _creationStash = World.GetStash<ProjectileCreationComponent>();
            _attackStatusStash = World.GetStash<AttackStatusComponent>();
        }

        public void OnUpdate(float deltaTime)
        {
            foreach (Entity weapon in _requestFilter)
            {
                _requestStash.Remove(weapon);

                ref ProjectileCreationComponent creation =
                    ref _creationStash.Get(weapon);

                ref AttackStatusComponent attackStatus =
                    ref _attackStatusStash.Get(weapon);

                if (creation.IsInitialized || !attackStatus.IsAttacking)
                    continue;

                _projectileFactory.Begin(weapon);
            }

            foreach (Entity weapon in _creationFilter)
            {
                ref ProjectileCreationComponent creation =
                    ref _creationStash.Get(weapon);

                if (!creation.IsInitialized)
                    continue;

                ref AttackStatusComponent attackStatus =
                    ref _attackStatusStash.Get(weapon);

                if (attackStatus.CreationFinished)
                    continue;

                _projectileFactory.Update(weapon, deltaTime);
            }
        }

        public void Dispose() { }
    }
}