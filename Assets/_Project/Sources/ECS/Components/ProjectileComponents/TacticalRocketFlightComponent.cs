using Scellecs.Morpeh;

namespace ECS.ProjectileComponents
{
    public struct TacticalRocketFlightComponent : IComponent
    {
        public Entity Target;
        public float LaunchDistanceRemaining;
        public float LaunchSpeed;
        public float LockTimeRemaining;
        public float RetargetDelay;
        public float SearchRadiusSquared;
        public float ExplosionDuration;
    }
}
