using ECS.AttackComponents;
using ECS.CommonComponents;
using ECS.EnemyComponents;
using ECS.PlayerComponents;
using Scellecs.Morpeh;

namespace Gameplay
{
    public static class CommonFunctions
    {
        public static bool IsValidTarget(
            Entity entity,
            AttackTargetType[] targetTypes,
            Filter damageableFilter,
            Filter enemyFilter,
            Filter playerFilter)
        {
            foreach (AttackTargetType targetType in targetTypes)
            {
                switch (targetType)
                {
                    case AttackTargetType.DamageableItem:
                        if (damageableFilter.Has(entity))
                            return true;
                        break;
                    case AttackTargetType.Enemy:
                        if (enemyFilter.Has(entity))
                            return true;
                        break;
                    case AttackTargetType.Player:
                        if (playerFilter.Has(entity))
                            return true;
                        break;
                }
            }

            return false;
        }

        public static bool IsValidTarget(
            Entity entity,
            AttackTargetType[] targetTypes,
            Stash<DamageableTag> damageableFilter,
            Stash<EnemyTag> enemyFilter,
            Stash<PlayerTag> playerFilter)
        {
            foreach (AttackTargetType targetType in targetTypes)
            {
                switch (targetType)
                {
                    case AttackTargetType.DamageableItem:
                        if (damageableFilter.Has(entity))
                            return true;
                        break;
                    case AttackTargetType.Enemy:
                        if (enemyFilter.Has(entity))
                            return true;
                        break;
                    case AttackTargetType.Player:
                        if (playerFilter.Has(entity))
                            return true;
                        break;
                }
            }

            return false;
        }

        public static void CreateCleanupRequest(World world, Entity entity)
        {
            Entity request = world.CreateEntity();
            Stash<CleanupRequest> cleanupRequestStash = world.GetStash<CleanupRequest>();

            cleanupRequestStash.Set(request, new CleanupRequest
            {
                Candidate = entity,
            });
        }
    }
}