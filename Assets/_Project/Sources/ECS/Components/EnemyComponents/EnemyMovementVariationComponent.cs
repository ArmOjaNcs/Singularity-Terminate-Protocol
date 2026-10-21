using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.EnemyComponents
{
    public struct EnemyMovementVariationComponent : IComponent
    {
        public float SpeedMultiplier;
        public Vector2 ApproachOffset;
    }
}