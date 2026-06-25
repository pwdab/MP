using System.Collections.Generic;
using UnityEngine;

namespace MP.Gameplay.Stats
{
    public sealed class StatsComponent : MonoBehaviour
    {
        [SerializeField] private EntityStatsDefinition baseStats;
        private EntityRuntimeStats currentStats = new();

        public EntityStatsDefinition BaseStats => baseStats;
        public EntityRuntimeStats Stats
        {
            get
            {
                EnsureInitialized();
                return currentStats;
            }
        }

        public float MaxHealth => Stats.MaxHealth;
        public float Defense => Stats.Defense;
        public float AttackPower => Stats.AttackPower;
        public float AttackSpeed => Stats.AttackSpeed;
        public float AutoAttackRange => Stats.AutoAttackRange;
        public float AutoProjectileRange => Stats.AutoProjectileRange;
        public float ManualProjectileRange => Stats.ManualProjectileRange;
        public float MoveSpeed => Stats.MoveSpeed;
        public float RespawnDelay => Stats.RespawnDelay;

        private void Awake()
        {
            InitializeFromBaseStats();
        }

        public void InitializeFromBaseStats()
        {
            if (baseStats == null)
            {
                throw new System.InvalidOperationException($"{name} is missing EntityStatsDefinition.");
            }

            EnsureRuntimeStats();
            currentStats.InitializeFromDefinition(baseStats);
        }

        public void AddFlatModifier(StatId statId, float value, object source)
        {
            Stats.AddFlatModifier(statId, value, source);
        }

        public void AddPercentModifier(StatId statId, float value, object source)
        {
            Stats.AddPercentModifier(statId, value, source);
        }

        public void AddModifier(StatModifierDefinition modifier, object source)
        {
            if (modifier == null)
            {
                return;
            }

            Stats.AddModifier(modifier.CreateRuntimeModifier(source));
        }

        public void AddModifiers(IReadOnlyList<StatModifierDefinition> modifiers, object source)
        {
            if (modifiers == null)
            {
                return;
            }

            for (int i = 0; i < modifiers.Count; i++)
            {
                AddModifier(modifiers[i], source);
            }
        }

        public bool RemoveModifiersFrom(object source)
        {
            return Stats.RemoveModifiersFrom(source);
        }

        private void EnsureRuntimeStats()
        {
            currentStats ??= new EntityRuntimeStats();
        }

        private void EnsureInitialized()
        {
            EnsureRuntimeStats();
            if (currentStats.IsInitialized)
            {
                return;
            }

            InitializeFromBaseStats();
        }
    }
}
