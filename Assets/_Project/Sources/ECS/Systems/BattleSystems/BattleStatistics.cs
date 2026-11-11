using System.Collections.Generic;
using ECS.WeaponComponents;
using UniRx;

namespace ECS.CommonComponents
{
    public sealed class BattleStatistics
    {
        private readonly Dictionary<CreatureType, float> _damageByCreature =
            new Dictionary<CreatureType, float>();

        private readonly Dictionary<WeaponType, float> _damageByWeapon =
            new Dictionary<WeaponType, float>();

        private readonly Dictionary<EffectType, float> _damageByEffect =
            new Dictionary<EffectType, float>();

        private readonly ReactiveProperty<int> _totalKilled = new ReactiveProperty<int>(0);
        private readonly ReactiveProperty<int> _totalCredits = new ReactiveProperty<int>(0);

        public float TotalDamage { get; private set; }

        public float TotalBlockedDamage { get; private set; }

        public int TotalHits { get; private set; }

        public ReactiveProperty<int> TotalKilled => _totalKilled;

        public ReactiveProperty<int> TotalCredits => _totalCredits;

        public void Add(BattleStatisticComponent statistic)
        {
            TotalDamage += statistic.Damage;
            TotalBlockedDamage += statistic.BlockedDamage;
            TotalHits++;

            AddDamage(_damageByCreature, statistic.Context.SourceType, statistic.Damage);
            AddDamage(_damageByWeapon, statistic.Context.WeaponType, statistic.Damage);

            if (statistic.Context.EffectType != EffectType.None)
                AddDamage(_damageByEffect, statistic.Context.EffectType, statistic.Damage);
        }

        public void AddKilledEnemy()
        {
            TotalKilled.Value++;
        }

        public void AddGainedCredits(int amount)
        {
            TotalCredits.Value += amount;
        }

        public float GetDamage(CreatureType type)
        {
            return GetDamage(_damageByCreature, type);
        }

        public float GetDamage(WeaponType type)
        {
            return GetDamage(_damageByWeapon, type);
        }

        public float GetDamage(EffectType type)
        {
            return GetDamage(_damageByEffect, type);
        }

        public void Clear()
        {
            TotalDamage = 0f;
            TotalBlockedDamage = 0f;
            TotalHits = 0;

            _damageByCreature.Clear();
            _damageByWeapon.Clear();
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
    }
}
