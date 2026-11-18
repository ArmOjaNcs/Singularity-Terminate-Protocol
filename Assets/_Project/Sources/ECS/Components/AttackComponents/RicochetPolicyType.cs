namespace ECS.AttackComponents
{
    public enum RicochetPolicyType
    {
        // The attack's projectiles never ricochet.
        None = 0,

        // Each projectile starts and resolves its own ricochet path.
        PerProjectile = 1,

        // The same guided projectile retargets and steers after a hit.
        GuidedProjectile = 2,

        // Hits queue a ricochet for the whole attack series to play afterward.
        AttackSeries = 3
    }
}
