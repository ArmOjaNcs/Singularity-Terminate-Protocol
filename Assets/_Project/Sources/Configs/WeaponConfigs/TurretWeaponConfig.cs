using Gameplay.View;
using UnityEngine;

namespace WeaponConfigs
{
    [CreateAssetMenu(fileName = "TurretWeapon", menuName = "Game/Weapon/Turret Config")]
    public sealed class TurretWeaponConfig : WeaponConfig
    {
        [Min(0.01f)] public float DeploymentCooldown = 4f;
        [Min(0.01f)] public float Lifetime = 10f;
        [Min(1)] public int MaxActiveTurrets = 2;
        [Min(0.1f)] public float PlacementDistance = 1.35f;
        [Min(0.01f)] public float TurnStepDuration = 0.085f;

        public Sprite BaseSprite;
        [Tooltip("Eight sprites in order: E, NE, N, NW, W, SW, S, SE.")]
        public Sprite[] HeadSprites;
        public float HeadHeight = 0.25f;
        [Tooltip("Height of the barrel axis above the turret's placement point.")]
        public float AimHeight = 0.66f;
        [Tooltip("Optional muzzle offsets from the head sprite pivot in E, NE, N, NW, W, SW, S, SE order.")]
        public Vector2[] MuzzleOffsets;

        internal bool HasViewSprites()
        {
            if (BaseSprite == null || HeadSprites == null || HeadSprites.Length != TurretView.DirectionCount)
                return false;

            foreach (Sprite sprite in HeadSprites)
            {
                if (sprite == null)
                    return false;
            }

            return true;
        }
    }
}
