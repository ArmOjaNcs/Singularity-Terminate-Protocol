using ECS.ProjectileComponents;
using ECS.WeaponComponents;
using System.Collections.Generic;

namespace ECS.DamageComponents
{
    public sealed class DamageStatistics
    {
        public float TotalDamage { get; private set; }
        public float TotalBlockedDamage { get; private set; }
        public int TotalHits { get; private set; }

        private readonly Dictionary<CreatureType, float> _damageByCreature =
            new Dictionary<CreatureType, float>();

        private readonly Dictionary<WeaponType, float> _damageByWeapon =
            new Dictionary<WeaponType, float>();

        private readonly Dictionary<ProjectileType, float> _damageByProjectile =
            new Dictionary<ProjectileType, float>();

        public void Add(DamageStatisticComponent statistic)
        {
            TotalDamage += statistic.Damage;
            TotalBlockedDamage += statistic.BlockedDamage;
            TotalHits++;

            AddDamage(
                _damageByCreature,
                statistic.Context.SourceType,
                statistic.Damage);

            AddDamage(
                _damageByWeapon,
                statistic.Context.WeaponType,
                statistic.Damage);

            AddDamage(
                _damageByProjectile,
                statistic.Context.ProjectileType,
                statistic.Damage);
        }

        public float GetDamage(CreatureType type)
        {
            return GetDamage(_damageByCreature, type);
        }

        public float GetDamage(WeaponType type)
        {
            return GetDamage(_damageByWeapon, type);
        }

        public float GetDamage(ProjectileType type)
        {
            return GetDamage(_damageByProjectile, type);
        }

        private void AddDamage<T>(Dictionary<T, float> dictionary, T key, float damage)
        {
            if (dictionary.TryGetValue(key, out float currentDamage))
                dictionary[key] = currentDamage + damage;
            else
                dictionary.Add(key, damage);
        }

        private float GetDamage<T>(Dictionary<T, float> dictionary, T key)
        {
            return dictionary.TryGetValue(key, out float damage) ? damage : 0f;
        }

        public void Clear()
        {
            TotalDamage = 0f;
            TotalBlockedDamage = 0f;
            TotalHits = 0;

            _damageByCreature.Clear();
            _damageByWeapon.Clear();
            _damageByProjectile.Clear();
        }
    }
}