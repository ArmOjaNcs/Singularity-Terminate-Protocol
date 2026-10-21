using Gameplay.Navigation;
using ECS.CommonComponents;
using UnityEngine;

namespace Gameplay
{
    public sealed class MovementResolver
    {
        private const int SlideAttempts = 8;
        private const float SlideStepFactor = 0.05f;
        private const int BinarySearchIterations = 8;

        private readonly NavigationGrid _navigationGrid;

        public MovementResolver(NavigationGrid navigationGrid)
        {
            _navigationGrid = navigationGrid;
        }

        public void Move(
            ref Vector3 position,
            Vector3 delta,
            ColliderComponent collider)
        {
            if (delta.sqrMagnitude <= 0f)
                return;

            Vector3 target = position + delta;

            // 1. Обычное движение.
            if (_navigationGrid.IsWalkable(target, collider))
            {
                position = target;
                return;
            }

            // 2. Пробуем двигаться вдоль каждой оси.
            Vector3 xDelta = new Vector3(delta.x, 0f, 0f);
            Vector3 yDelta = new Vector3(0f, delta.y, 0f);

            Vector3 xPosition = FindFurthestWalkablePosition(
                position,
                xDelta,
                collider);

            Vector3 yPosition = FindFurthestWalkablePosition(
                position,
                yDelta,
                collider);

            float xProgress = Mathf.Abs(xPosition.x - position.x);
            float yProgress = Mathf.Abs(yPosition.y - position.y);

            if (xProgress > 0f || yProgress > 0f)
            {
                if (xProgress >= yProgress && xProgress > 0f)
                    position = xPosition;
                else
                    position = yPosition;

                return;
            }

            // 3. Если обе оси заблокированы, пробуем обойти угол.
            TrySlide(ref position, delta, collider);
        }

        private bool TrySlide(
            ref Vector3 position,
            Vector3 delta,
            ColliderComponent collider)
        {
            Vector3 direction = delta.normalized;

            Vector3 perpendicular = new Vector3(
                -direction.y,
                direction.x,
                0f);

            float slideStep = _navigationGrid.CellSize * SlideStepFactor;

            Vector3 bestPosition = position;
            float bestProgress = 0f;

            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 sideDirection = perpendicular * side;
                Vector3 sideProbe = position + sideDirection * slideStep;

                if (!_navigationGrid.IsWalkable(sideProbe, collider))
                    continue;

                for (int i = 1; i <= SlideAttempts; i++)
                {
                    float offset = slideStep * i;

                    Vector3 adjustedPosition =
                        position + sideDirection * offset;

                    if (!_navigationGrid.IsWalkable(
                            adjustedPosition,
                            collider))
                    {
                        break;
                    }

                    Vector3 adjustedTarget = adjustedPosition + delta;

                    if (!_navigationGrid.IsWalkable(
                            adjustedTarget,
                            collider))
                    {
                        continue;
                    }

                    float progress = Vector3.Dot(
                        adjustedTarget - position,
                        direction);

                    if (progress <= bestProgress)
                        continue;

                    bestProgress = progress;
                    bestPosition = adjustedTarget;
                }
            }

            if (bestProgress <= 0f)
                return false;

            position = bestPosition;
            return true;
        }

        private Vector3 FindFurthestWalkablePosition(
            Vector3 position,
            Vector3 delta,
            ColliderComponent collider)
        {
            if (delta.sqrMagnitude <= 0f)
                return position;

            if (_navigationGrid.IsWalkable(position + delta, collider))
                return position + delta;

            float min = 0f;
            float max = 1f;

            for (int i = 0; i < BinarySearchIterations; i++)
            {
                float middle = (min + max) * 0.5f;
                Vector3 candidate = position + delta * middle;

                if (_navigationGrid.IsWalkable(candidate, collider))
                    min = middle;
                else
                    max = middle;
            }

            return position + delta * min;
        }
    }
}