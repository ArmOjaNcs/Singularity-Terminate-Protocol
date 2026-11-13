using Configs;
using UnityEngine;
using UnityEngine.Serialization;

public class ColliderEntityConfig : ScriptableObject
{
    [FormerlySerializedAs("Collider")]
    public ColliderConfig HitBox;
    public ColliderConfig NavigationBox;
}
