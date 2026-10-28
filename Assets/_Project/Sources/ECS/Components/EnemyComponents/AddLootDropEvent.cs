using Scellecs.Morpeh;
using UnityEngine;

namespace ECS.EnemyComponents
{
    public struct AddLootDropEvent : IComponent
    {
        public Vector3 Position;
        public CreatureType EnemyType;
    }
}