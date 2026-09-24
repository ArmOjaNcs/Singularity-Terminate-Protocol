using UnityEngine;

namespace Gameplay.CameraScripts
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float height = 15f;
        [SerializeField] private float aheadDistance = 3f;
        [SerializeField] private float smoothTime = 0.1f;

        private Vector3 velocity;
        private Vector3 previousTargetPosition;
        private Camera _camera;

        private ArenaBounds _arenaBounds;

        private float _minX;
        private float _maxX;
        private float _minZ;
        private float _maxZ;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        private void Start()
        {
            if (target != null)
            {
                previousTargetPosition = target.position;
            }

            CalculateBounds();
        }

        private void LateUpdate()
        {
            if (target == null)
                return;

            Vector3 movementDirection = GetMovementDirection();

            Vector3 targetPosition = target.position;

            targetPosition.y = height;

            targetPosition += movementDirection * aheadDistance;

            targetPosition = ClampPosition(targetPosition);

            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref velocity,
                smoothTime
            );
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;

            if (target != null)
            {
                previousTargetPosition = target.position;
            }
        }

        public void SetBounds(ArenaBounds bounds)
        {
            _arenaBounds = bounds;

            CalculateBounds();
        }

        private Vector3 GetMovementDirection()
        {
            Vector3 movement =
                target.position - previousTargetPosition;

            previousTargetPosition = target.position;

            movement.y = 0f;

            if (movement.sqrMagnitude < 0.0001f)
                return Vector3.zero;

            return movement.normalized;
        }

        private void CalculateBounds()
        {
            if (_arenaBounds == null || _camera == null)
                return;

            Bounds bounds = _arenaBounds.WorldBounds;

            float halfHeight = _camera.orthographicSize;
            float halfWidth = halfHeight * _camera.aspect;

            _minX = bounds.min.x + halfWidth;
            _maxX = bounds.max.x - halfWidth;

            _minZ = bounds.min.z + halfHeight;
            _maxZ = bounds.max.z - halfHeight;
        }

        private Vector3 ClampPosition(Vector3 position)
        {
            position.x = Mathf.Clamp(
                position.x,
                _minX,
                _maxX
            );

            position.z = Mathf.Clamp(
                position.z,
                _minZ,
                _maxZ
            );

            return position;
        }
    }
}