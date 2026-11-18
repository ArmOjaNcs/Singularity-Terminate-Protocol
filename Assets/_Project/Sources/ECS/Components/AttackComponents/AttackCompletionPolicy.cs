namespace ECS.AttackComponents
{
    public enum AttackCompletionPolicy
    {
        // Creation is complete once the configured projectiles are emitted.
        AfterCreation = 0,

        // Creation is complete after emitted attack trajectories finish.
        AfterTrajectories = 1
    }
}
