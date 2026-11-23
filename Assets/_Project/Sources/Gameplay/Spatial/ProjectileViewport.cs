using UnityEngine;

namespace Gameplay.Spatial
{
    internal static class ProjectileViewport
    {
        public static bool TryGetBounds(out Rect bounds)
        {
            bounds = default;
            Camera camera = Camera.main;
            if (camera == null)
                return false;

            Transform transform = camera.transform;
            float depth = Vector3.Dot(-transform.position, transform.forward);
            Vector3 minimum = camera.ViewportToWorldPoint(new Vector3(0f, 0f, depth));
            Vector3 maximum = camera.ViewportToWorldPoint(new Vector3(1f, 1f, depth));
            bounds = Rect.MinMaxRect(minimum.x, minimum.y, maximum.x, maximum.y);
            return true;
        }

        public static bool TryGetImpact(
            Rect bounds, float radius, Vector3 start, Vector3 end, out Vector3 impact)
        {
            impact = default;
            float left = bounds.xMin + radius;
            float right = bounds.xMax - radius;
            float bottom = bounds.yMin + radius;
            float top = bounds.yMax - radius;
            if (left >= right || bottom >= top)
                return false;

            // A moving camera can put the start of this step outside the viewport.
            if (start.x < left || start.x > right || start.y < bottom || start.y > top)
            {
                impact = new Vector3(Mathf.Clamp(start.x, left, right),
                    Mathf.Clamp(start.y, bottom, top), start.z);
                return true;
            }

            float x = GetImpactFraction(start.x, end.x, left, right);
            float y = GetImpactFraction(start.y, end.y, bottom, top);
            float fraction = Mathf.Min(x, y);
            if (fraction > 1f)
                return false;

            impact = Vector3.Lerp(start, end, fraction);
            return true;
        }

        private static float GetImpactFraction(float start, float end, float min, float max)
        {
            if (end > min && end < max)
                return float.PositiveInfinity;

            if (end == start)
                return 0f;

            return (Mathf.Clamp(end, min, max) - start) / (end - start);
        }
    }
}
