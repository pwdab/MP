using System;
using System.Collections.Generic;

namespace MP.Gameplay.Stats
{
    public sealed class EntityRuntimeStats
    {
        private readonly Dictionary<StatId, float> baseValues = new();
        private readonly Dictionary<StatId, StatBounds> bounds = new();
        private readonly Dictionary<StatId, float> currentValues = new();
        private List<StatRuntimeModifier> modifiers;
        private bool isDirty = true;
        private bool isInitialized;

        public bool IsInitialized => isInitialized;

        public float MaxHealth
        {
            get
            {
                return GetValue(StatId.MaxHealth);
            }
        }

        public float Defense
        {
            get
            {
                return GetValue(StatId.Defense);
            }
        }

        public float AttackPower
        {
            get
            {
                return GetValue(StatId.AttackPower);
            }
        }

        public float AttackSpeed
        {
            get
            {
                return GetValue(StatId.AttackSpeed);
            }
        }

        public float AutoAttackRange
        {
            get
            {
                return GetValue(StatId.AutoAttackRange);
            }
        }

        public float ProjectileRange
        {
            get
            {
                return GetValue(StatId.ProjectileRange);
            }
        }

        public float MoveSpeed
        {
            get
            {
                return GetValue(StatId.MoveSpeed);
            }
        }

        public float RespawnDelay
        {
            get
            {
                return GetValue(StatId.RespawnDelay);
            }
        }

        public void InitializeFromDefinition(EntityStatsDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            definition.ValidateOrThrow();

            baseValues.Clear();
            bounds.Clear();
            currentValues.Clear();
            isInitialized = false;

            IReadOnlyList<StatEntry> definitions = definition.Stats;
            for (int i = 0; i < definitions.Count; i++)
            {
                StatEntry stat = definitions[i];
                baseValues[stat.StatId] = stat.BaseValue;
                bounds[stat.StatId] = stat.Bounds;
            }

            ClearModifiersInternal();
            isInitialized = true;
        }

        public void SetBaseValue(StatId statId, float value, StatBounds statBounds)
        {
            EnsureInitialized();
            if (!statBounds.IsValid)
            {
                throw new InvalidOperationException($"Cannot set invalid bounds for stat '{statId}'. Minimum is greater than maximum.");
            }

            baseValues[statId] = value;
            bounds[statId] = statBounds;
            MarkDirty();
        }

        public void SetBaseValue(StatId statId, float value)
        {
            EnsureInitialized();
            if (!bounds.TryGetValue(statId, out StatBounds statBounds))
            {
                throw new InvalidOperationException($"Cannot set base value for stat '{statId}' before its bounds are defined.");
            }

            SetBaseValue(statId, value, statBounds);
        }

        public void SetBounds(StatId statId, StatBounds statBounds)
        {
            EnsureInitialized();
            if (!statBounds.IsValid)
            {
                throw new InvalidOperationException($"Cannot set invalid bounds for stat '{statId}'. Minimum is greater than maximum.");
            }

            if (!baseValues.ContainsKey(statId))
            {
                throw new InvalidOperationException($"Cannot set bounds for stat '{statId}' before its base value is defined.");
            }

            bounds[statId] = statBounds;
            MarkDirty();
        }

        public void AddFlatModifier(StatId statId, float value, object source)
        {
            AddModifier(new StatRuntimeModifier(statId, StatModifierType.Flat, value, source));
        }

        public void AddPercentModifier(StatId statId, float value, object source)
        {
            AddModifier(new StatRuntimeModifier(statId, StatModifierType.Percent, value, source));
        }

        public void AddModifier(StatRuntimeModifier modifier)
        {
            EnsureInitialized();
            if (modifier.Source == null)
            {
                throw new ArgumentException("Modifier source cannot be null.", nameof(modifier));
            }

            if (!baseValues.ContainsKey(modifier.StatId))
            {
                throw new InvalidOperationException($"Cannot add modifier for missing stat '{modifier.StatId}'.");
            }

            EnsureModifiers();
            modifiers.Add(modifier);
            MarkDirty();
        }

        public bool RemoveModifiersFrom(object source)
        {
            EnsureInitialized();
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            EnsureModifiers();
            int removedCount = modifiers.RemoveAll(modifier => ReferenceEquals(modifier.Source, source));
            if (removedCount > 0)
            {
                MarkDirty();
            }

            return removedCount > 0;
        }

        public void ClearModifiers()
        {
            EnsureInitialized();
            ClearModifiersInternal();
        }

        private void ClearModifiersInternal()
        {
            EnsureModifiers();
            modifiers.Clear();
            MarkDirty();
        }

        public void Recalculate()
        {
            EnsureInitialized();
            currentValues.Clear();
            foreach (StatId statId in baseValues.Keys)
            {
                currentValues[statId] = CalculateValue(statId);
            }

            isDirty = false;
        }

        public float GetValue(StatId statId)
        {
            EnsureInitialized();
            EnsureRecalculated();
            if (currentValues.TryGetValue(statId, out float value))
            {
                return value;
            }

            throw new InvalidOperationException($"Missing runtime value for stat '{statId}'.");
        }

        private float CalculateValue(StatId statId)
        {
            float baseValue = GetBaseValue(statId);
            float flatModifier = 0f;
            float percentModifier = 0f;

            EnsureModifiers();
            for (int i = 0; i < modifiers.Count; i++)
            {
                StatRuntimeModifier modifier = modifiers[i];
                if (modifier.StatId != statId)
                {
                    continue;
                }

                if (modifier.Type == StatModifierType.Flat)
                {
                    flatModifier += modifier.Value;
                }
                else if (modifier.Type == StatModifierType.Percent)
                {
                    percentModifier += modifier.Value;
                }
            }

            float value = (baseValue + flatModifier) * (1f + percentModifier);
            return GetBounds(statId).Clamp(value);
        }

        private float GetBaseValue(StatId statId)
        {
            if (baseValues.TryGetValue(statId, out float value))
            {
                return value;
            }

            throw new InvalidOperationException($"Missing base value for stat '{statId}'.");
        }

        public float GetMinimumValue(StatId statId)
        {
            EnsureInitialized();
            return GetBounds(statId).Minimum;
        }

        public float GetMaximumValue(StatId statId)
        {
            EnsureInitialized();
            return GetBounds(statId).Maximum;
        }

        private StatBounds GetBounds(StatId statId)
        {
            if (bounds.TryGetValue(statId, out StatBounds value))
            {
                return value;
            }

            throw new InvalidOperationException($"Missing bounds for stat '{statId}'.");
        }

        private void EnsureModifiers()
        {
            modifiers ??= new List<StatRuntimeModifier>();
        }

        private void EnsureRecalculated()
        {
            if (isDirty)
            {
                Recalculate();
            }
        }

        private void EnsureInitialized()
        {
            if (!isInitialized)
            {
                throw new InvalidOperationException("EntityRuntimeStats must be initialized before use.");
            }
        }

        private void MarkDirty()
        {
            isDirty = true;
        }
    }
}
