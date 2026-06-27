using System;
using System.Collections.Generic;

namespace MP.Gameplay.Stats
{
    /*
        엔티티의 런타임 스탯 계산 상태
        EntityStatsDefinition의 BaseValue와 StatCatalogDefinition의 Bounds를 복사한 뒤 Modifier를 적용해 최종값을 계산
        Modifier는 source별로 기록하고 StatId별 합산값을 캐싱하므로 제거와 계산이 모두 안정적으로 동작해야 함
        source는 StatModifierSource로 관리하며 AddModifier는 누적, ReplaceModifiersFrom은 교체 용도로 사용
    */
    public sealed class EntityRuntimeStats
    {
        private const float ModifierSumEpsilon = 0.000001f;

        /*
            현재는 스탯 수가 적고 디버깅과 확장성이 중요하므로 Dictionary 기반으로 관리한다.
            수천 개 엔티티가 매 프레임 많은 스탯을 조회하는 상황에서 실제 병목으로 확인되면,
            StatId를 배열 index로 사용하는 float[] 기반 캐시 구조로 전환을 검토한다.
        */
        private readonly Dictionary<StatId, float> baseValues = new();
        private readonly Dictionary<StatId, StatBounds> bounds = new();
        private readonly Dictionary<StatModifierSource, List<StatRuntimeModifier>> modifiersBySource = new();
        private readonly Dictionary<StatId, float> flatModifierSums = new();
        private readonly Dictionary<StatId, float> percentModifierSums = new();
        private bool isInitialized;

        public bool IsInitialized => isInitialized;

        public void InitializeFromDefinition(EntityStatsDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            definition.ValidateOrThrow();

            baseValues.Clear();
            bounds.Clear();
            isInitialized = false;

            IReadOnlyList<StatEntry> definitions = definition.Stats;
            for (int i = 0; i < definitions.Count; i++)
            {
                StatEntry stat = definitions[i];
                baseValues[stat.StatId] = stat.BaseValue;
                bounds[stat.StatId] = definition.GetBounds(stat.StatId);
            }

            ClearModifiersInternal();
            isInitialized = true;
        }

        public void SetBaseValue(StatId statId, float value)
        {
            EnsureInitialized();
            if (!bounds.TryGetValue(statId, out StatBounds statBounds))
            {
                throw new InvalidOperationException($"Cannot set base value for stat '{statId}' before its bounds are defined.");
            }

            baseValues[statId] = statBounds.Clamp(value);
        }

        public void AddFlatModifier(StatId statId, float value, StatModifierSource source)
        {
            AddModifier(new StatRuntimeModifier(statId, StatModifierType.Flat, value, source));
        }

        public void AddPercentModifier(StatId statId, float value, StatModifierSource source)
        {
            AddModifier(new StatRuntimeModifier(statId, StatModifierType.Percent, value, source));
        }

        // 같은 source의 modifier를 여러 번 누적할 때 사용한다.
        public void AddModifier(StatRuntimeModifier modifier)
        {
            EnsureInitialized();
            ValidateModifier(modifier, modifier.Source);
            AddModifierInternal(modifier);
        }

        // 같은 source의 기존 modifier를 새 목록으로 교체할 때 사용한다.
        public void ReplaceModifiersFrom(StatModifierSource source, IReadOnlyList<StatRuntimeModifier> modifiers)
        {
            EnsureInitialized();
            ValidateSource(source);

            if (modifiers != null)
            {
                for (int i = 0; i < modifiers.Count; i++)
                {
                    ValidateModifier(modifiers[i], source);
                }
            }

            RemoveModifiersFromInternal(source);
            if (modifiers == null)
            {
                return;
            }

            for (int i = 0; i < modifiers.Count; i++)
            {
                StatRuntimeModifier modifier = modifiers[i];
                AddModifierInternal(modifier);
            }
        }

        public bool RemoveModifiersFrom(StatModifierSource source)
        {
            EnsureInitialized();
            ValidateSource(source);

            return RemoveModifiersFromInternal(source);
        }

        private bool RemoveModifiersFromInternal(StatModifierSource source)
        {
            if (!modifiersBySource.TryGetValue(source, out List<StatRuntimeModifier> modifiers))
            {
                return false;
            }

            for (int i = 0; i < modifiers.Count; i++)
            {
                RemoveModifierFromCache(modifiers[i]);
            }

            modifiersBySource.Remove(source);
            return true;
        }

        public void ClearModifiers()
        {
            EnsureInitialized();
            ClearModifiersInternal();
        }

        private void ClearModifiersInternal()
        {
            modifiersBySource.Clear();
            flatModifierSums.Clear();
            percentModifierSums.Clear();
        }

        // snapshot 배열을 만들기 때문에 디버그 UI/로그용으로는 좋지만, 매 프레임 핫패스에서 쓰면 안 됨.
        public void ForEachModifierSource(Action<StatModifierSource, IReadOnlyList<StatRuntimeModifier>> visitor)
        {
            EnsureInitialized();
            if (visitor == null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }

            foreach (KeyValuePair<StatModifierSource, List<StatRuntimeModifier>> pair in modifiersBySource)
            {
                visitor(pair.Key, pair.Value.ToArray());
            }
        }

        public float GetValue(StatId statId)
        {
            EnsureInitialized();
            return CalculateValue(statId);
        }

        private float CalculateValue(StatId statId)
        {
            float baseValue = GetBaseValue(statId);
            float flatModifier = GetModifierSum(flatModifierSums, statId);
            float percentModifier = GetModifierSum(percentModifierSums, statId);

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

        private void AddModifierInternal(StatRuntimeModifier modifier)
        {
            if (!modifiersBySource.TryGetValue(modifier.Source, out List<StatRuntimeModifier> modifiers))
            {
                modifiers = new List<StatRuntimeModifier>();
                modifiersBySource[modifier.Source] = modifiers;
            }

            modifiers.Add(modifier);
            AddModifierToCache(modifier);
        }

        private void AddModifierToCache(StatRuntimeModifier modifier)
        {
            switch (modifier.Type)
            {
                case StatModifierType.Flat:
                    AddModifierSum(flatModifierSums, modifier.StatId, modifier.Value);
                    break;
                case StatModifierType.Percent:
                    AddModifierSum(percentModifierSums, modifier.StatId, modifier.Value);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(modifier), modifier.Type, "Unsupported stat modifier type.");
            }
        }

        private void RemoveModifierFromCache(StatRuntimeModifier modifier)
        {
            switch (modifier.Type)
            {
                case StatModifierType.Flat:
                    AddModifierSum(flatModifierSums, modifier.StatId, -modifier.Value);
                    break;
                case StatModifierType.Percent:
                    AddModifierSum(percentModifierSums, modifier.StatId, -modifier.Value);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(modifier), modifier.Type, "Unsupported stat modifier type.");
            }
        }

        private static void ValidateModifierType(StatModifierType type)
        {
            if (type != StatModifierType.Flat && type != StatModifierType.Percent)
            {
                throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported stat modifier type.");
            }
        }

        private void ValidateModifier(StatRuntimeModifier modifier, StatModifierSource expectedSource)
        {
            modifier.ValidateOrThrow();
            ValidateSource(modifier.Source);
            if (modifier.Source != expectedSource)
            {
                throw new InvalidOperationException("Modifier source must match the expected source object.");
            }

            if (!baseValues.ContainsKey(modifier.StatId))
            {
                throw new InvalidOperationException($"Cannot add modifier for missing stat '{modifier.StatId}'.");
            }

            ValidateModifierType(modifier.Type);
        }

        private static float GetModifierSum(Dictionary<StatId, float> modifierSums, StatId statId)
        {
            return modifierSums.TryGetValue(statId, out float value) ? value : 0f;
        }

        private static void AddModifierSum(Dictionary<StatId, float> modifierSums, StatId statId, float value)
        {
            modifierSums.TryGetValue(statId, out float currentValue);
            float nextValue = currentValue + value;
            if (Math.Abs(nextValue) <= ModifierSumEpsilon)
            {
                modifierSums.Remove(statId);
                return;
            }

            modifierSums[statId] = nextValue;
        }

        private static void ValidateSource(StatModifierSource source)
        {
            if (!source.IsValid())
            {
                throw new ArgumentException("Modifier source must be valid.", nameof(source));
            }
        }

        private void EnsureInitialized()
        {
            if (!isInitialized)
            {
                throw new InvalidOperationException("EntityRuntimeStats must be initialized before use.");
            }
        }
    }
}
