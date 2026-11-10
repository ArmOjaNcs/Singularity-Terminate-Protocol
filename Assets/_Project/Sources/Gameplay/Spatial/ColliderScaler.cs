using ECS.CommonComponents;
using UnityEngine;

namespace Gameplay.Spatial
{
    public static class ColliderScaler
    {
        public static ColliderComponent Scale(ColliderComponent collider, float multiplier)
        {
            multiplier = Mathf.Max(0f, multiplier);
            collider.Radius *= multiplier;
            collider.Length *= multiplier;
            collider.Size *= multiplier;
            collider.Center *= multiplier;
            return collider;
        }
    }
}
