using System;
using System.Collections.Generic;
using UnityEngine;

namespace MP.Gameplay.Stats
{
    /*
        특정 엔티티의 기본 스탯값을 관리
        StatEntry를 통해 StatId별 BaseValue를 정의하고, 스탯의 공통 규칙과 허용 범위는 StatCatalogDefinition에서 조회
        에셋 생성 시 코드에 정의된 기본값으로 초기화되며, 각 스탯 값은 Inspector에서 수정 가능
        런타임에서는 StatCatalogDefinition이 반드시 연결되어 있어야 함
    */
    [CreateAssetMenu(menuName = "MP/Data/Entity Stats Definition")]
    public sealed class EntityStatsDefinition : ScriptableObject
    {
        // StatId 목록은 런타임에 변하지 않으므로 한 번만 캐시해 Enum.GetValues의 반복 할당을 피한다.
        private static readonly StatId[] AllStatIds = (StatId[])Enum.GetValues(typeof(StatId));

        [Tooltip("이 엔티티가 사용할 전역 스탯 카탈로그입니다. 런타임에서는 반드시 연결되어 있어야 합니다.")]
        [SerializeField] private StatCatalogDefinition statCatalog;

        [Header("기본 스탯")]
        [Tooltip("이 엔티티의 기본 스탯 목록입니다. 각 StatId는 중복 없이 한 번만 등록되어야 합니다.")]
        [SerializeField] private StatEntry[] stats = CreateDefaultStats();

        // 런타임에서 불변 에셋을 매 스폰마다 재검증하지 않도록, 1회 검증 후 캐시한다. 에디터 OnValidate에서 초기화된다.
        [NonSerialized] private bool runtimeValidated;

        public StatCatalogDefinition StatCatalog => statCatalog;
        public IReadOnlyList<StatEntry> Stats => stats ?? Array.Empty<StatEntry>();

        private void OnValidate()
        {
            runtimeValidated = false;
            RepairMissingStats();
            NormalizeStats();
            LogValidationWarnings();
        }

        public bool IsValid()
        {
            return IsValid(out _);
        }

        public bool IsValid(out string reason)
        {
            if (statCatalog == null)
            {
                reason = $"{name} is missing stat catalog.";
                return false;
            }

            if (!statCatalog.IsValid(out string catalogReason))
            {
                reason = catalogReason;
                return false;
            }

            IReadOnlyList<StatEntry> statValues = Stats;
            if (statValues.Count == 0)
            {
                reason = $"{name} has no stats.";
                return false;
            }

            IReadOnlyList<StatDefinition> statDefinitions = statCatalog.Stats;
            for (int i = 0; i < statDefinitions.Count; i++)
            {
                StatId statId = statDefinitions[i].StatId;
                if (!ContainsStat(statValues, statId))
                {
                    reason = $"{name} is missing stat '{statId}'.";
                    return false;
                }
            }

            for (int i = 0; i < statValues.Count; i++)
            {
                StatEntry stat = statValues[i];
                if (!stat.IsValid(out string statReason))
                {
                    reason = $"{name} has invalid stat entry at index {i}. {statReason}";
                    return false;
                }

                if (!GetBounds(stat.StatId).IsValid(out string boundsReason))
                {
                    reason = $"{name} has invalid bounds for stat '{stat.StatId}'. {boundsReason}";
                    return false;
                }

                for (int j = i + 1; j < statValues.Count; j++)
                {
                    if (statValues[j].StatId == stat.StatId)
                    {
                        reason = $"{name} has duplicate stat '{stat.StatId}'.";
                        return false;
                    }
                }
            }

            reason = string.Empty;
            return true;
        }

        public void ValidateOrThrow()
        {
            if (runtimeValidated)
            {
                return;
            }

            if (!IsValid(out string reason))
            {
                throw new InvalidOperationException(reason);
            }

            runtimeValidated = true;
        }

        public float GetBaseValue(StatId statId)
        {
            IReadOnlyList<StatEntry> statValues = Stats;
            for (int i = statValues.Count - 1; i >= 0; i--)
            {
                if (statValues[i].StatId == statId)
                {
                    return statValues[i].BaseValue;
                }
            }

            throw new InvalidOperationException($"Missing base value for stat '{statId}' in {name}.");
        }

        public StatBounds GetBounds(StatId statId)
        {
            if (statCatalog == null)
            {
                throw new InvalidOperationException($"{name} is missing stat catalog.");
            }

            if (!statCatalog.TryGetDefinition(statId, out StatDefinition definition))
            {
                throw new InvalidOperationException($"{statCatalog.name} is missing stat definition '{statId}'.");
            }

            return definition.Bounds;
        }

        private void LogValidationWarnings()
        {
            if (statCatalog == null)
            {
                Debug.LogWarning($"{name} has no stat catalog. Runtime stat bounds cannot be resolved.", this);
            }

            // 누락 스탯은 RepairMissingStats에서 이미 기본값으로 채우고 경고하므로 여기서 다시 검사하지 않는다.
            IReadOnlyList<StatEntry> statValues = Stats;
            for (int i = 0; i < statValues.Count; i++)
            {
                StatEntry stat = statValues[i];
                if (statCatalog != null)
                {
                    if (!TryGetCatalogBounds(stat.StatId, out StatBounds bounds))
                    {
                        Debug.LogWarning($"{statCatalog.name} is missing stat definition '{stat.StatId}'.", statCatalog);
                    }
                    else if (!bounds.IsValid())
                    {
                        Debug.LogWarning($"{name} has invalid bounds for stat '{stat.StatId}'. Minimum is greater than maximum.", this);
                    }
                }

                for (int j = i + 1; j < statValues.Count; j++)
                {
                    if (statValues[j].StatId == stat.StatId)
                    {
                        Debug.LogWarning($"{name} has duplicate stat '{stat.StatId}'.", this);
                        break;
                    }
                }
            }
        }

        private void RepairMissingStats()
        {
            if (statCatalog == null)
            {
                return;
            }

            IReadOnlyList<StatEntry> statValues = Stats;
            List<StatEntry> repairedStats = null;
            IReadOnlyList<StatDefinition> statDefinitions = statCatalog.Stats;
            for (int i = 0; i < statDefinitions.Count; i++)
            {
                StatId statId = statDefinitions[i].StatId;
                IReadOnlyList<StatEntry> currentStats = repairedStats ?? statValues;
                if (ContainsStat(currentStats, statId))
                {
                    continue;
                }

                repairedStats ??= new List<StatEntry>(statValues);
                repairedStats.Add(CreateDefaultStat(statId));
                Debug.LogWarning($"{name}에 누락된 StatEntry '{statId}'를 기본값으로 자동 추가했습니다. 실제 BaseValue는 Inspector에서 확인 후 조정하세요.", this);
            }

            if (repairedStats != null)
            {
                stats = repairedStats.ToArray();
            }
        }

        private void NormalizeStats()
        {
            if (stats == null)
            {
                return;
            }

            if (statCatalog == null)
            {
                Debug.LogWarning($"{name} cannot normalize stat values because stat catalog is missing.", this);
                return;
            }

            for (int i = 0; i < stats.Length; i++)
            {
                StatEntry stat = stats[i];
                if (!TryGetCatalogBounds(stat.StatId, out StatBounds bounds))
                {
                    Debug.LogWarning($"{name} cannot normalize stat '{stat.StatId}' because {statCatalog.name} has no matching definition.", this);
                    continue;
                }

                float clampedValue = bounds.Clamp(stat.BaseValue);
                if (!Mathf.Approximately(clampedValue, stat.BaseValue))
                {
                    Debug.LogWarning($"{name} normalized base value for stat '{stat.StatId}' from {stat.BaseValue} to {clampedValue}.", this);
                }

                stats[i] = stat.WithBaseValue(clampedValue);
            }
        }

        private bool TryGetCatalogBounds(StatId statId, out StatBounds bounds)
        {
            if (statCatalog != null && statCatalog.TryGetDefinition(statId, out StatDefinition definition))
            {
                bounds = definition.Bounds;
                return true;
            }

            bounds = default;
            return false;
        }

        private static StatEntry CreateDefaultStat(StatId statId)
        {
            return statId switch
            {
                StatId.MaxHealth => new StatEntry(statId, 100f),
                StatId.Defense => new StatEntry(statId, 100f),
                StatId.AttackPower => new StatEntry(statId, 10f),
                StatId.AttackSpeed => new StatEntry(statId, 1f),
                StatId.AutoAttackRange => new StatEntry(statId, 1.5f),
                StatId.AutoProjectileRange => new StatEntry(statId, 5f),
                StatId.ManualProjectileRange => new StatEntry(statId, 7.5f),
                StatId.MoveSpeed => new StatEntry(statId, 5f),
                StatId.RespawnDelay => new StatEntry(statId, 3f),
                _ => new StatEntry(statId, 0f)
            };
        }

        private static StatEntry[] CreateDefaultStats()
        {
            StatEntry[] defaultStats = new StatEntry[AllStatIds.Length];
            for (int i = 0; i < AllStatIds.Length; i++)
            {
                defaultStats[i] = CreateDefaultStat(AllStatIds[i]);
            }

            return defaultStats;
        }

        private static bool ContainsStat(IReadOnlyList<StatEntry> statValues, StatId statId)
        {
            for (int i = 0; i < statValues.Count; i++)
            {
                if (statValues[i].StatId == statId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
