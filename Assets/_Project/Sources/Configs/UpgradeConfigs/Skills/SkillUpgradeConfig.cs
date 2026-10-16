using ECS.CommonComponents;
using ECS.PlayerComponents;
using PlayerConfigs;
using Scellecs.Morpeh;
using UnityEngine;

namespace UpgradeConfigs
{
    [CreateAssetMenu(fileName = "SkillUpgradeConfig", menuName = "Game/Upgrades/Skill Upgrade Config")]
    public class SkillUpgradeConfig : UpgradeConfig
    {
        public int MaxLevel = 5;
        public PlayerStats Stat;
        public float StatFactor = 5;

        public override void Apply(Entity entity, PlayerInventoryComponent inventory)
        {
            World world = entity.GetWorld();

            switch (Stat)
            {
                case PlayerStats.GatheringRadius:
                case PlayerStats.EvasionChance:
                    ApplyPlayerStatsUpgrade(entity, world);
                    break;
                default:
                    ApplyBaseStatsUpgrade(entity, world);
                    break;
            }

            if (inventory.ActiveUpgrades.TryGetValue(UpgradeId, out int currentLevel))
                inventory.ActiveUpgrades[UpgradeId] = currentLevel++;
            else
                inventory.ActiveUpgrades.Add(UpgradeId, 1);
        }

        private void ApplyPlayerStatsUpgrade(Entity playerEntity, World world)
        {
            var statsStash = world.GetStash<PlayerStatsComponent>();

            if (statsStash.Has(playerEntity))
            {
                ref var playerStats = ref statsStash.Get(playerEntity);

                switch (Stat)
                {
                    case PlayerStats.GatheringRadius:
                        playerStats.GatheringRadius += playerStats.GatheringRadius * (StatFactor / 100f);
                        break;
                    case PlayerStats.EvasionChance:
                        playerStats.EvasionChance += playerStats.EvasionChance * (StatFactor / 100f);
                        break;
                    default:
                        break;
                }
            }
        }

        private void ApplyBaseStatsUpgrade(Entity playerEntity, World world)
        {
            var statsStash = world.GetStash<StatsComponent>();

            if (statsStash.Has(playerEntity))
            {
                ref var playerStats = ref statsStash.Get(playerEntity);

                switch (Stat)
                {
                    case PlayerStats.MaxHealth:
                        playerStats.MaxHealth += playerStats.MaxHealth * (StatFactor / 100f);
                        break;
                    case PlayerStats.HealthRegeneration:
                        playerStats.HealthRegeneration += playerStats.HealthRegeneration * (StatFactor / 100f);
                        break;
                    case PlayerStats.Damage:
                        playerStats.Damage += playerStats.Damage * (StatFactor / 100f);
                        break;
                    case PlayerStats.Luck:
                        playerStats.Luck += playerStats.Luck * (StatFactor / 100f);
                        break;
                    case PlayerStats.Defence:
                        playerStats.Defence += playerStats.Defence * (StatFactor / 100f);
                        break;
                    case PlayerStats.Speed:
                        playerStats.Speed += playerStats.Speed * (StatFactor / 100f);
                        break;
                    default:
                        break;
                }
            }
        }
    }
}