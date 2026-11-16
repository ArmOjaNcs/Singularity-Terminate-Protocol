using Configs;
using ECS.CommonComponents;
using ECS.EnemyComponents;
using ECS.ViewComponents;
using ECS.WeaponSystems;
using Gameplay.Creatures;
using Gameplay.View;
using Scellecs.Morpeh;
using UnityEngine;

namespace Gameplay.WeaponTesting
{
    /// <summary>Mannequin adapter that builds its ECS collider and stats from a creature config.</summary>
    public sealed class WeaponTestDummy : MonoBehaviour
    {
        [SerializeField] private WeaponTestDummyConfig _config;

        private World _world;
        private Entity _entity;
        private bool _created;
        private float _maxHealth;
        private float _defence;
        private float _recoveryTimer;
        private Vector3 _homePosition;
        private AnimatedCreature _animatedView;

        public Entity Entity => _entity;
        public WeaponTestDummyConfig Config => _config;

        public void Configure(
            WeaponTestDummyConfig config,
            float maxHealth,
            float defence)
        {
            _config = config;
            _maxHealth = Mathf.Max(1f, maxHealth);
            _defence = Mathf.Clamp(defence, 0f, 100f);
        }

        private void Start()
        {
            if (_config == null || _config.HitBox == null)
            {
                Debug.LogError("WeaponTestDummy requires a config with a HitBox.", this);
                return;
            }

            _homePosition = transform.position;
            _world = World.Default;
            _entity = _world.CreateEntity();
            _created = true;

            var creatureFactory = new CreatureFactory(
                _world,
                new WeaponFactory(_world));
            creatureFactory.CreateCommonComponents(_entity, _config, transform.position);
            _world.GetStash<EnemyTag>().Set(_entity, new EnemyTag());

            _maxHealth = _maxHealth > 0f ? _maxHealth : Mathf.Max(1f, _config.Stats.MaxHealth);
            _defence = Mathf.Clamp(_defence, 0f, 100f);
            ApplyStatsAndRestore();

            _animatedView = GetComponent<AnimatedCreature>();
            if (_animatedView != null)
            {
                _animatedView.SetEntity(_entity);
                _world.GetStash<ViewComponent>().Set(_entity,
                    new ViewComponent { View = _animatedView });
                _world.GetStash<AnimatedCreatureComponent>().Set(_entity,
                    new AnimatedCreatureComponent { View = _animatedView });
            }
        }

        private void Update()
        {
            if (!_created)
                return;

            Stash<DeathComponent> deathStash = _world.GetStash<DeathComponent>();
            if (deathStash.Has(_entity))
            {
                _recoveryTimer += Time.deltaTime;
                float recoveryDelay = Mathf.Max(0f, _config.RecoveryDelay);
                if (_recoveryTimer < recoveryDelay)
                    return;

                _recoveryTimer = 0f;
                deathStash.Remove(_entity);
                _world.GetStash<HealthComponent>().Get(_entity).Current = _maxHealth;
            }

            if (!_world.GetStash<KnockbackComponent>().Has(_entity))
            {
                ref PositionComponent position = ref _world.GetStash<PositionComponent>().Get(_entity);
                position.Position = Vector3.MoveTowards(
                    position.Position,
                    _homePosition,
                    Mathf.Max(0f, _config.ReturnSpeed) * Time.deltaTime);
            }

            if (_animatedView == null)
                transform.position = _world.GetStash<PositionComponent>().Get(_entity).Position;
        }

        public void SetTestStats(float maxHealth, float defence)
        {
            _maxHealth = Mathf.Max(1f, maxHealth);
            _defence = Mathf.Clamp(defence, 0f, 100f);
            _recoveryTimer = 0f;
            if (_created)
                ApplyStatsAndRestore();
        }

        private void ApplyStatsAndRestore()
        {
            ref StatsComponent stats = ref _world.GetStash<StatsComponent>().Get(_entity);
            stats.MaxHealth = _maxHealth;
            stats.Defence = _defence;
            _world.GetStash<HealthComponent>().Get(_entity).Current = _maxHealth;
            if (_world.GetStash<DeathComponent>().Has(_entity))
                _world.GetStash<DeathComponent>().Remove(_entity);
        }

        private void OnDestroy()
        {
            if (_created && _world != null)
                _world.RemoveEntity(_entity);
        }
    }
}
