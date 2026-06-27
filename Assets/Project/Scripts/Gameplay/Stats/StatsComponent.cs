using System.Collections.Generic;
using UnityEngine;

namespace MP.Gameplay.Stats
{
    /*
        GameObject가 보유한 스탯을 관리하는 Unity 컴포넌트
        EntityStatsDefinition으로 런타임 스탯을 초기화하고, 외부 시스템에는 GetValue와 Modifier 적용 API를 제공
        장비나 직업처럼 교체되는 효과는 ReplaceModifiersFrom을 사용하고, 누적 강화는 AddModifier 계열을 사용
    */
    public sealed class StatsComponent : MonoBehaviour
    {
        [Tooltip("이 객체가 사용할 기본 스탯 데이터입니다. 런타임 시작 시 이 값으로 EntityRuntimeStats를 초기화합니다.")]
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

        private void Awake()
        {
            InitializeFromBaseStats();
        }

        public float GetValue(StatId statId)
        {
            return Stats.GetValue(statId);
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

        public void AddFlatModifier(StatId statId, float value, StatModifierSource source)
        {
            Stats.AddFlatModifier(statId, value, source);
        }

        public void AddPercentModifier(StatId statId, float value, StatModifierSource source)
        {
            Stats.AddPercentModifier(statId, value, source);
        }

        public void AddModifier(StatModifierDefinition modifier, StatModifierSource source)
        {
            if (modifier == null)
            {
                WarnNullModifier(source);
                return;
            }

            Stats.AddModifier(modifier.CreateRuntimeModifier(source));
        }

        public void AddModifiers(IReadOnlyList<StatModifierDefinition> modifiers, StatModifierSource source)
        {
            if (modifiers == null)
            {
                return;
            }

            // 누적 적용용 API다. 교체 적용은 ReplaceModifiersFrom을 사용한다.
            for (int i = 0; i < modifiers.Count; i++)
            {
                AddModifier(modifiers[i], source);
            }
        }

        public void ReplaceModifiersFrom(StatModifierSource source, IReadOnlyList<StatModifierDefinition> modifiers)
        {
            if (modifiers == null)
            {
                Stats.ReplaceModifiersFrom(source, null);
                return;
            }

            var runtimeModifiers = new List<StatRuntimeModifier>(modifiers.Count);
            for (int i = 0; i < modifiers.Count; i++)
            {
                StatModifierDefinition modifier = modifiers[i];
                if (modifier == null)
                {
                    WarnNullModifier(source);
                    continue;
                }

                runtimeModifiers.Add(modifier.CreateRuntimeModifier(source));
            }

            Stats.ReplaceModifiersFrom(source, runtimeModifiers);
        }

        public bool RemoveModifiersFrom(StatModifierSource source)
        {
            return Stats.RemoveModifiersFrom(source);
        }

        private void WarnNullModifier(StatModifierSource source)
        {
            Debug.LogWarning($"{name} ignored a null StatModifierDefinition from '{source.DisplayName}'.", this);
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
