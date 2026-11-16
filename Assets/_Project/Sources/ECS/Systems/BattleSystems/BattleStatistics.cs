using System.Collections.Generic;
using System;
using ECS.WeaponComponents;
using Scellecs.Morpeh;
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

        private readonly Dictionary<Entity, float> _directDamageByWeapon =
            new Dictionary<Entity, float>();

        private readonly Dictionary<WeaponEffectKey, float> _effectDamageByWeapon =
            new Dictionary<WeaponEffectKey, float>();

        private readonly ReactiveProperty<int> _totalKilled = new ReactiveProperty<int>(0);
        private readonly ReactiveProperty<int> _totalCredits = new ReactiveProperty<int>(0);

        public float TotalDamage { get; private set; }

        public float TotalDirectDamage { get; private set; }

        public float TotalEffectDamage { get; private set; }

        public float TotalBlockedDamage { get; private set; }

        public bool IsRecording { get; private set; } = true;

        public int TotalHits { get; private set; }

        public ReactiveProperty<int> TotalKilled => _totalKilled;

        public ReactiveProperty<int> TotalCredits => _totalCredits;

        public void Add(BattleStatisticComponent statistic)
        {
            if (!IsRecording)
                return;

            TotalDamage += statistic.Damage;
            TotalBlockedDamage += statistic.BlockedDamage;
            TotalHits++;

            AddDamage(_damageByCreature, statistic.Context.SourceType, statistic.Damage);
            AddDamage(_damageByWeapon, statistic.Context.WeaponType, statistic.Damage);

            if (statistic.Context.DamageType == DamageType.Direct)
            {
                TotalDirectDamage += statistic.Damage;
                AddDamage(_directDamageByWeapon, statistic.Context.Weapon, statistic.Damage);
            }
            else if (statistic.Context.DamageType == DamageType.Effect)
            {
                TotalEffectDamage += statistic.Damage;
                if (statistic.Context.EffectType != EffectType.None)
                {
                    AddDamage(_damageByEffect, statistic.Context.EffectType, statistic.Damage);
                    AddDamage(_effectDamageByWeapon,
                        new WeaponEffectKey(statistic.Context.Weapon, statistic.Context.EffectType),
                        statistic.Damage);
                }
            }

        }

        public void AddKilledEnemy()
        {
            if (!IsRecording)
                return;

            TotalKilled.Value++;
        }

        public void AddGainedCredits(int amount)
        {
            if (!IsRecording)
                return;

            TotalCredits.Value += amount;
        }

        public void SetRecording(bool isRecording)
        {
            IsRecording = isRecording;
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

        public float GetDirectDamage(Entity weapon)
        {
            return GetDamage(_directDamageByWeapon, weapon);
        }

        public float GetEffectDamage(Entity weapon, EffectType effect)
        {
            return GetDamage(_effectDamageByWeapon,
                new WeaponEffectKey(weapon, effect));
        }

        public void Clear()
        {
            TotalDamage = 0f;
            TotalDirectDamage = 0f;
            TotalEffectDamage = 0f;
            TotalBlockedDamage = 0f;
            TotalHits = 0;
            _totalKilled.Value = 0;
            _totalCredits.Value = 0;

            _damageByCreature.Clear();
            _damageByWeapon.Clear();
            _damageByEffect.Clear();
            _directDamageByWeapon.Clear();
            _effectDamageByWeapon.Clear();
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

        private struct WeaponEffectKey : IEquatable<WeaponEffectKey>
        {
            private readonly Entity _weapon;
            private readonly EffectType _effect;

            public WeaponEffectKey(Entity weapon, EffectType effect)
            {
                _weapon = weapon;
                _effect = effect;
            }

            public bool Equals(WeaponEffectKey other)
            {
                return _weapon == other._weapon && _effect == other._effect;
            }

            public override bool Equals(object obj)
            {
                return obj is WeaponEffectKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return (_weapon.GetHashCode() * 397) ^ (int)_effect;
            }
        }
    }
}
