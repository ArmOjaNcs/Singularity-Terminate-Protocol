using ECS.AttackComponents;
using ECS.CommonComponents;
using ECS.EnemyComponents;
using ECS.PlayerComponents;
using ECS.ProjectileComponents;
using Scellecs.Morpeh;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Gameplay
{
    public sealed class EcsDebugVisualizer : MonoBehaviour
    {
        [Header("Colliders")]
        public bool ShowColliders = true;
        public Color ColliderColor = Color.cyan;

        [Header("Target Search Radius")]
        public bool ShowTargetSearchRadius = true;
        public Color SearchRadiusColor = Color.green;

        [Header("Current Targets")]
        public bool ShowTargets = true;
        public Color TargetColor = Color.yellow;

        [Header("Display")]
        public bool ShowOnlySelected;

        private World _world;

        private Filter _colliderFilter;
        private Filter _attackFilter;
        private Filter _targetFilter;
        private Filter _enemyFilter;
        private Filter _playerFilter;

        private Stash<ColliderComponent> _colliderStash;
        private Stash<ColliderPositionComponent> _colliderPositionStash;
        private Stash<PositionComponent> _positionStash;
        private Stash<AttackComponent> _attackStash;
        private Stash<TargetComponent> _targetStash;

        private bool _initialized;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            _world = World.Default;

            if (_world == null)
                return;

            _colliderStash = _world.GetStash<ColliderComponent>();
            _colliderPositionStash =
                _world.GetStash<ColliderPositionComponent>();
            _positionStash = _world.GetStash<PositionComponent>();
            _attackStash = _world.GetStash<AttackComponent>();
            _targetStash = _world.GetStash<TargetComponent>();

            _colliderFilter = _world.Filter
                .With<ColliderComponent>()
                .Build();

            _attackFilter = _world.Filter
                .With<AttackComponent>()
                .With<PositionComponent>()
                .Build();

            _targetFilter = _world.Filter
                .With<TargetComponent>()
                .With<PositionComponent>()
                .With<AttackComponent>()
                .Build();

            _enemyFilter = _world.Filter
                .With<EnemyTag>()
                .Without<DeathComponent>()
                .Build();

            _playerFilter = _world.Filter
                .With<PlayerTag>()
                .Without<DeathComponent>()
                .Build();

            _initialized = true;
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying)
                return;

#if UNITY_EDITOR
            if (ShowOnlySelected &&
                Selection.activeGameObject != gameObject)
            {
                return;
            }
#else
            if (ShowOnlySelected)
                return;
#endif

            if (!_initialized)
            {
                Initialize();

                if (!_initialized)
                    return;
            }

            if (ShowColliders)
                DrawColliders();

            if (ShowTargetSearchRadius)
                DrawSearchRadii();

            if (ShowTargets)
                DrawTargets();
        }

        private void DrawColliders()
        {
            foreach (Entity entity in _colliderFilter)
            {
                ref ColliderComponent collider =
                    ref _colliderStash.Get(entity);

                Vector3 center;

                if (_colliderPositionStash.Has(entity))
                {
                    center = _colliderPositionStash
                        .Get(entity).CenterPosition;
                }
                else if (_positionStash.Has(entity))
                {
                    center = _positionStash.Get(entity).Position;
                }
                else
                {
                    continue;
                }

                Gizmos.color = ColliderColor;
                DrawCollider(center, collider);
            }
        }

        private void DrawCollider(
            Vector3 center,
            ColliderComponent collider)
        {
            Matrix4x4 previousMatrix = Gizmos.matrix;

            float angle = collider.Angle;

            if (collider.Type == ColliderType.Capsule &&
                collider.CapsuleOrientation == CapsuleOrientation.Y)
            {
                angle += 90f;
            }

            Gizmos.matrix = Matrix4x4.TRS(
                center,
                Quaternion.Euler(0f, 0f, angle),
                Vector3.one);

            switch (collider.Type)
            {
                case ColliderType.Circle:
                    Gizmos.DrawWireSphere(
                        Vector3.zero,
                        Mathf.Max(0f, collider.Radius));
                    break;

                case ColliderType.Box:
                    Gizmos.DrawWireCube(
                        Vector3.zero,
                        new Vector3(
                            Mathf.Max(0f, collider.Size.x),
                            Mathf.Max(0f, collider.Size.y),
                            0f));
                    break;

                case ColliderType.Capsule:
                    DrawCapsule(collider);
                    break;

                case ColliderType.Sector:
                    DrawSector(collider);
                    break;
            }

            Gizmos.matrix = previousMatrix;
        }

        private void DrawCapsule(ColliderComponent collider)
        {
            float radius = Mathf.Max(0f, collider.Radius);
            float length = Mathf.Max(radius * 2f, collider.Length);

            float halfSegment = length * 0.5f - radius;

            Vector3 first = Vector3.left * halfSegment;
            Vector3 second = Vector3.right * halfSegment;

            Gizmos.DrawWireSphere(first, radius);
            Gizmos.DrawWireSphere(second, radius);

            Gizmos.DrawLine(
                first + Vector3.up * radius,
                second + Vector3.up * radius);

            Gizmos.DrawLine(
                first - Vector3.up * radius,
                second - Vector3.up * radius);
        }

        private void DrawSector(ColliderComponent collider)
        {
            float radius = Mathf.Max(0f, collider.Radius);

            float halfAngle = Mathf.Clamp(
                collider.Size.x * 0.5f,
                0f,
                90f);

            const int segments = 32;

            Vector3 previousPoint = SectorPoint(-halfAngle, radius);

            for (int i = 1; i <= segments; i++)
            {
                float angle = Mathf.Lerp(
                    -halfAngle,
                    halfAngle,
                    i / (float)segments);

                Vector3 currentPoint = SectorPoint(angle, radius);

                Gizmos.DrawLine(previousPoint, currentPoint);

                previousPoint = currentPoint;
            }

            Gizmos.DrawLine(
                Vector3.zero,
                SectorPoint(-halfAngle, radius));

            Gizmos.DrawLine(
                Vector3.zero,
                SectorPoint(halfAngle, radius));
        }

        private Vector3 SectorPoint(float angle, float radius)
        {
            float radians = angle * Mathf.Deg2Rad;

            return new Vector3(
                Mathf.Cos(radians) * radius,
                Mathf.Sin(radians) * radius,
                0f);
        }

        private void DrawSearchRadii()
        {
            Gizmos.color = SearchRadiusColor;

            foreach (Entity entity in _attackFilter)
            {
                ref AttackComponent attack =
                    ref _attackStash.Get(entity);

                ref PositionComponent position =
                    ref _positionStash.Get(entity);

                if (attack.TargetSearchRadius <= 0f)
                    continue;

                Gizmos.DrawWireSphere(
                    position.Position,
                    attack.TargetSearchRadius);
            }
        }

        private void DrawTargets()
        {
            Gizmos.color = TargetColor;

            foreach (Entity entity in _targetFilter)
            {
                ref TargetComponent target =
                    ref _targetStash.Get(entity);

                ref AttackComponent attack =
                    ref _attackStash.Get(entity);

                Entity targetEntity = target.Target;

                if (targetEntity == default)
                    continue;

                try
                {
                    bool validTarget = attack.TargetType switch
                    {
                        AttackTargetType.Enemy =>
                            _enemyFilter.Has(targetEntity),

                        AttackTargetType.Player =>
                            _playerFilter.Has(targetEntity),

                        _ => false
                    };

                    if (!validTarget ||
                        !_positionStash.Has(targetEntity))
                    {
                        continue;
                    }

                    ref PositionComponent source =
                        ref _positionStash.Get(entity);

                    Vector3 destination = GetColliderPosition(
                        targetEntity);

                    Gizmos.DrawLine(
                        source.Position,
                        destination);
                }
                catch (InvalidHasOperationException)
                {
                    // ÷ель уже удалена из ECS.
                }
            }
        }

        private Vector3 GetColliderPosition(Entity entity)
        {
            if (_colliderPositionStash.Has(entity))
            {
                return _colliderPositionStash
                    .Get(entity)
                    .CenterPosition;
            }

            return _positionStash.Get(entity).Position;
        }
    }
}