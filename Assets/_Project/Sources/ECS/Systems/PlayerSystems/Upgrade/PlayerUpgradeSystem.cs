using System;
using ECS.AttackComponents;
using ECS.CommonComponents;
using ECS.PlayerComponents;
using ECS.WeaponComponents;
using ECS.WeaponSystems;
using PlayerConfigs;
using Scellecs.Morpeh;
using UI;
using UniRx;
using UpgradeConfigs;

namespace ECS.PlayerSystems
{
    public sealed class PlayerUpgradeSystem : ISystem
    {
        private WeaponFactory _factory;

        private Filter _eventFilter;
        private Filter _playerFilter;

        private Stash<UpgradeSelectedEvent> _eventStash;
        private Stash<PlayerInventoryComponent> _inventoryStash;

        public PlayerUpgradeSystem(WeaponFactory factory, EquipPanel equipPanel)
        {
            _factory = factory;
        }

        public World World { get; set; }

        public void OnAwake()
        {
            _eventFilter = World.Filter.With<UpgradeSelectedEvent>().Build();
            _playerFilter = World.Filter.With<PlayerInventoryComponent>().Build();

            _eventStash = World.GetStash<UpgradeSelectedEvent>();
            _inventoryStash = World.GetStash<PlayerInventoryComponent>();
        }

        public void OnUpdate(float deltaTime)
        {
            if (_eventFilter.IsEmpty())
                return;

            foreach (var eventEntity in _eventFilter)
            {
                ref var eventData = ref _eventStash.Get(eventEntity);
                UpgradeConfig upgrade = eventData.SelectedUpgrade;

                foreach (var playerEntity in _playerFilter)
                {
                    ref var inventory = ref _inventoryStash.Get(playerEntity);

                    ApplyUpgrade(playerEntity, ref inventory, upgrade, eventData.BaseWeaponId, eventData.BaseWeaponType);
                }

                World.RemoveEntity(eventEntity);
            }
        }

        public void Dispose()
        {
        }

        private void ApplyUpgrade(
            Entity playerEntity,
            ref PlayerInventoryComponent inventory,
            UpgradeConfig upgrade,
            string baseWeaponId = null,
            WeaponType baseWeaponType = WeaponType.MeleeDefault)
        {
            switch (upgrade.UpgradeType)
            {
                case UpgradeType.NewWeapon:
                    ApplyNewWeapon(playerEntity, ref inventory, upgrade);
                    break;
                case UpgradeType.NewTurret:
                    ApplyNewTurret(playerEntity, ref inventory, upgrade);
                    break;
                case UpgradeType.WeaponStats:
                case UpgradeType.TurretStats:
                    ApplyWeaponUpgrade(playerEntity, ref inventory, upgrade, baseWeaponId, baseWeaponType);
                    break;
                case UpgradeType.Item:
                    ApplySkillUpgrade(playerEntity, ref inventory, upgrade);
                    break;
                default:
                    break;
            }
        }

        private void ApplyNewWeapon(Entity playerEntity, ref PlayerInventoryComponent inventory, UpgradeConfig upgrade)
        {
            if (upgrade is not NewWeaponUpgradeConfig config)
                throw new ArgumentException("Неверный тип апгрейда");

            if (!_factory.Create(config.Config, playerEntity))
                return;

            inventory.ActiveWeapons.Add(config.UpgradeId, 1);
        }

        private void ApplyNewTurret(Entity playerEntity, ref PlayerInventoryComponent inventory, UpgradeConfig upgrade)
        {
            if (upgrade is not NewTurretUpgradeConfig config)
                throw new ArgumentException("Неверный тип апгрейда");

            if (!_factory.Create(config.Config, playerEntity))
                return;

            inventory.ActiveTurrets.Add(config.UpgradeId, 1);
        }

        private void ApplyWeaponUpgrade(
            Entity playerEntity,
            ref PlayerInventoryComponent inventory,
            UpgradeConfig upgrade,
            string baseWeaponId,
            WeaponType baseWeaponType)
        {
            if (upgrade is not WeaponStatUpgradeConfig config || string.IsNullOrEmpty(baseWeaponId))
                throw new ArgumentException("Неверный тип апгрейда");

            var weaponStash = World.GetStash<WeaponComponent>();
            var weaponFilter = World.Filter.With<WeaponComponent>().Build();

            foreach (var weaponEntity in weaponFilter)
            {
                ref var weapon = ref weaponStash.Get(weaponEntity);

                if (weapon.Owner == playerEntity && weapon.Type == baseWeaponType)
                {
                    ApplyWeaponStatsUpgrade(weaponEntity, config);

                    if (upgrade.UpgradeType == UpgradeType.WeaponStats)
                        IncreaseLevel(inventory.ActiveWeapons, baseWeaponId);

                    if (upgrade.UpgradeType == UpgradeType.TurretStats)
                        IncreaseLevel(inventory.ActiveTurrets, baseWeaponId);

                    break;
                }
            }
        }

        private void IncreaseLevel(ReactiveDictionary<string, int> dictionary, string baseWeaponId)
        {
            if (dictionary.TryGetValue(baseWeaponId, out int currentLevel))
                dictionary[baseWeaponId] = currentLevel + 1;
            else
                throw new ArgumentNullException("Не найдено значение базового оружия, укажите его первым в списке требуемых улучшений");
        }

        private void ApplyWeaponStatsUpgrade(Entity weaponEntity, WeaponStatUpgradeConfig config)
        {
            var attackStash = World.GetStash<AttackComponent>();
            ref var attack = ref attackStash.Get(weaponEntity);
            int amount = config.Amount;

            switch (config.Stat)
            {
                case WeaponStats.ProjectileCount:
                    attack.ProjectileCount += amount;
                    break;
                case WeaponStats.Pierce:
                    attack.Pierce += amount;
                    break;
                case WeaponStats.AttackDuration:
                    attack.AttackDuration += (float)Math.Round(attack.AttackDuration * (amount / 100f), 2);
                    break;
                case WeaponStats.FireRate:
                    attack.FireRate += (float)Math.Round(attack.FireRate * (amount / 100f), 2);
                    break;
                case WeaponStats.Damage:
                    attack.Damage += amount;
                    break;
                case WeaponStats.AttackCooldown:
                    attack.AttackCooldown -= (float)Math.Round(attack.AttackCooldown * (amount / 100f), 2);
                    break;
                case WeaponStats.DistanceFromSource:
                    attack.DistanceFromSource += (float)Math.Round(attack.DistanceFromSource * (amount / 100f), 2);
                    break;
                case WeaponStats.RicochetTargetSearchRadius:
                    attack.RicochetTargetSearchRadius += (float)Math.Round(attack.RicochetTargetSearchRadius * (amount / 100f), 2);
                    break;
                case WeaponStats.TargetSearchRadius:
                    attack.TargetSearchRadius += (float)Math.Round(attack.TargetSearchRadius * (amount / 100f), 2);
                    break;
                case WeaponStats.ProjectileSpeed:
                    attack.ProjectileSpeed += (float)Math.Round(attack.ProjectileSpeed * (amount / 100f), 2);
                    break;
                case WeaponStats.ProjectileLifetime:
                    attack.ProjectileLifetime += (float)Math.Round(attack.ProjectileLifetime * (amount / 100f), 2);
                    break;
                case WeaponStats.ProjectileScale:
                    attack.ProjectileScale += (float)Math.Round(attack.ProjectileScale * (amount / 100f), 2);
                    break;
                case WeaponStats.Homing:
                    attack.HomingEnabled = true;
                    break;
                default:
                    break;
            }

            attackStash.Set(weaponEntity, attack);
        }

        private void ApplySkillUpgrade(Entity playerEntity, ref PlayerInventoryComponent inventory, UpgradeConfig upgrade)
        {
            if (upgrade is not SkillUpgradeConfig config)
                throw new ArgumentException("Неверный тип апгрейда");

            string upgradeId = config.UpgradeId;

            switch (config.Stat)
            {
                case PlayerStats.GatheringRadius:
                case PlayerStats.EvasionChance:
                    ApplyPlayerStatsUpgrade(playerEntity, config);
                    break;
                default:
                    ApplyBaseStatsUpgrade(playerEntity, config);
                    break;
            }

            if (inventory.ActiveSkills.TryGetValue(upgradeId, out int currentLevel))
                inventory.ActiveSkills[upgradeId] = currentLevel + 1;
            else
                inventory.ActiveSkills.Add(upgradeId, 1);
        }

        private void ApplyBaseStatsUpgrade(Entity playerEntity, SkillUpgradeConfig config)
        {
            var statsStash = World.GetStash<StatsComponent>();
            float statFactor = config.StatFactor;

            if (statsStash.Has(playerEntity))
            {
                ref var playerStats = ref statsStash.Get(playerEntity);

                switch (config.Stat)
                {
                    case PlayerStats.MaxHealth:
                        playerStats.MaxHealth += playerStats.MaxHealth * (statFactor / 100f);
                        break;
                    case PlayerStats.HealthRegeneration:
                        playerStats.HealthRegeneration += playerStats.HealthRegeneration * (statFactor / 100f);
                        break;
                    case PlayerStats.Damage:
                        playerStats.Damage += playerStats.Damage * (statFactor / 100f);
                        break;
                    case PlayerStats.Luck:
                        playerStats.Luck += playerStats.Luck * (statFactor / 100f);
                        break;
                    case PlayerStats.Defence:
                        playerStats.Defence += playerStats.Defence * (statFactor / 100f);
                        break;
                    case PlayerStats.Speed:
                        playerStats.Speed += playerStats.Speed * (statFactor / 100f);
                        break;
                    default:
                        break;
                }
            }
        }

        private void ApplyPlayerStatsUpgrade(Entity playerEntity, SkillUpgradeConfig config)
        {
            var statsStash = World.GetStash<PlayerStatsComponent>();

            if (statsStash.Has(playerEntity))
            {
                ref var playerStats = ref statsStash.Get(playerEntity);

                switch (config.Stat)
                {
                    case PlayerStats.GatheringRadius:
                        playerStats.GatheringRadius += (float)Math.Round(playerStats.GatheringRadius * (config.StatFactor / 100f), 2);
                        break;
                    case PlayerStats.EvasionChance:
                        playerStats.EvasionChance += (float)Math.Round(playerStats.EvasionChance * (config.StatFactor / 100f), 2);
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
