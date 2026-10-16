using System;
using System.Linq;
using ECS.AttackComponents;
using ECS.PlayerComponents;
using ECS.WeaponComponents;
using Scellecs.Morpeh;
using UnityEngine;

namespace UpgradeConfigs
{
    [CreateAssetMenu(fileName = "WeaponStatsUpgradeConfig", menuName = "Game/Upgrades/Weapon Stats Upgrade Config")]
    public class WeaponUpgradeConfig : UpgradeConfig
    {
        public WeaponType Type;
        public WeaponStats Stat;
        public int StatsFactor = 5;

        public override void Apply(Entity entity, PlayerInventoryComponent inventory)
        {
            World world = entity.GetWorld();

            var weaponStash = world.GetStash<WeaponComponent>();
            var weaponFilter = world.Filter.With<WeaponComponent>().Build();

            foreach (var weaponEntity in weaponFilter)
            {
                ref var weapon = ref weaponStash.Get(weaponEntity);

                if (weapon.Owner == entity)
                {
                    ApplyWeaponStatsUpgrade(weaponEntity, world);

                    string baseWeaponId = RequiredUpgradeIds[0].UpgradeId;

                    if (inventory.ActiveUpgrades.TryGetValue(baseWeaponId, out int currentLevel))
                        inventory.ActiveUpgrades[baseWeaponId] = currentLevel++;
                    else
                        throw new ArgumentNullException("Не найдено значение базового оружия, укажите его первым в списке требуемых улучшений");

                    inventory.ActiveUpgrades.Add(UpgradeId, 1);
                }
            }
        }

        private void ApplyWeaponStatsUpgrade(Entity weaponEntity, World world)
        {
            var attackStash = world.GetStash<AttackComponent>();
            ref var attack = ref attackStash.Get(weaponEntity);

            switch (Stat)
            {
                case WeaponStats.ProjectileCount:
                    attack.ProjectileCount += StatsFactor;
                    break;
                case WeaponStats.Pierce:
                    attack.Pierce += StatsFactor;
                    break;
                case WeaponStats.AttackDuration:
                    attack.AttackDuration += (float)Math.Round(attack.AttackDuration * (StatsFactor / 100f), 2);
                    break;
                case WeaponStats.FireRate:
                    attack.FireRate += (float)Math.Round(attack.FireRate * (StatsFactor / 100f), 2);
                    break;
                case WeaponStats.Damage:
                    attack.Damage += (float)Math.Round(attack.Damage * (StatsFactor / 100f), 2);
                    break;
                case WeaponStats.AttackCooldown:
                    attack.AttackCooldown -= (float)Math.Round(attack.AttackCooldown * (StatsFactor / 100f), 2);
                    break;
                case WeaponStats.DistanceFromSource:
                    attack.DistanceFromSource += (float)Math.Round(attack.DistanceFromSource * (StatsFactor / 100f), 2);
                    break;
                case WeaponStats.AttackRadius:
                    attack.AttackRadius += (float)Math.Round(attack.AttackRadius * (StatsFactor / 100f), 2);
                    break;
                case WeaponStats.TargetSearchRadius:
                    attack.TargetSearchRadius += (float)Math.Round(attack.TargetSearchRadius * (StatsFactor / 100f), 2);
                    break;
                case WeaponStats.ProjectileSpeed:
                    attack.ProjectileSpeed += (float)Math.Round(attack.ProjectileSpeed * (StatsFactor / 100f), 2);
                    break;
                case WeaponStats.ProjectileLifetime:
                    attack.ProjectileLifetime += (float)Math.Round(attack.ProjectileLifetime * (StatsFactor / 100f), 2);
                    break;
                default:
                    break;
            }
        }
    }
}