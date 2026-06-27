using System;
using System.Collections.Generic;
using UnityEngine;

namespace MP.Gameplay.Stats
{
    /*
        모든 스탯 종류를 정의하고 관리
        코드에 정의된 값은 에셋 생성 및 누락 스탯 자동 추가를 위한 초기값이며, 반드시 StatCatalogDefinition 에셋에서 실제 값을 정의해야 함
        런타임 스탯의 허용 범위는 이 에셋을 단일 출처로 사용
    */
    [CreateAssetMenu(menuName = "MP/Data/Stat Catalog Definition")]
    public sealed class StatCatalogDefinition : ScriptableObject
    {
        // StatId 목록은 런타임에 변하지 않으므로 한 번만 캐시해 Enum.GetValues의 반복 할당을 피한다.
        private static readonly StatId[] AllStatIds = (StatId[])Enum.GetValues(typeof(StatId));

        [Tooltip("게임 전체에서 사용하는 스탯 정의 목록입니다. 코드 기본값은 초기 생성용이며, 실제 허용 범위는 이 에셋에서 조정합니다.")]
        [SerializeField] private StatDefinition[] stats = CreateDefaultDefinitions();

        // 런타임에서 불변 에셋을 매 스폰마다 재검증하지 않도록, 1회 검증 후 캐시한다. 에디터 OnValidate에서 초기화된다.
        [NonSerialized] private bool runtimeValidated;

        // StatId로 정의를 O(1) 조회하기 위한 지연 생성 캐시. stats가 바뀌는 OnValidate에서 무효화된다.
        [NonSerialized] private Dictionary<StatId, StatDefinition> definitionLookup;

        public IReadOnlyList<StatDefinition> Stats => stats ?? Array.Empty<StatDefinition>();

        public bool TryGetDefinition(StatId statId, out StatDefinition definition)
        {
            EnsureDefinitionLookup();
            return definitionLookup.TryGetValue(statId, out definition);
        }

        private void EnsureDefinitionLookup()
        {
            if (definitionLookup != null)
            {
                return;
            }

            IReadOnlyList<StatDefinition> statDefinitions = Stats;
            definitionLookup = new Dictionary<StatId, StatDefinition>(statDefinitions.Count);
            for (int i = 0; i < statDefinitions.Count; i++)
            {
                // 중복 StatId가 있으면 기존 역방향 선형 탐색과 동일하게 마지막 항목이 우선하도록 덮어쓴다.
                definitionLookup[statDefinitions[i].StatId] = statDefinitions[i];
            }
        }

        // 에셋 편집 중 StatId 변경 사항이 카탈로그에 반영되도록 보장
        private void OnValidate()
        {
            runtimeValidated = false;
            definitionLookup = null;
            RepairMissingStats();
            NormalizeStats();
            WarnDuplicateStats();
        }

        private void RepairMissingStats()
        {
            IReadOnlyList<StatDefinition> statDefinitions = Stats;
            List<StatDefinition> repairedStats = null;
            foreach (StatId statId in AllStatIds)
            {
                if (ContainsStat(statDefinitions, statId))
                {
                    continue;
                }

                repairedStats ??= new List<StatDefinition>(statDefinitions);
                repairedStats.Add(CreateDefaultDefinition(statId));
                Debug.LogWarning($"{name}에 누락된 StatDefinition '{statId}'를 초기값으로 자동 추가했습니다. 실제 Bounds는 Inspector에서 확인 후 조정하세요.", this);
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

            for (int i = 0; i < stats.Length; i++)
            {
                StatDefinition stat = stats[i];
                StatDefinition validStat = stat.WithValidRange();
                if (!stat.Bounds.IsValid())
                {
                    Debug.LogWarning($"{name} normalized invalid bounds for stat definition '{stat.StatId}'. Minimum is greater than maximum.", this);
                }

                stats[i] = validStat;
            }
        }

        private void WarnDuplicateStats()
        {
            IReadOnlyList<StatDefinition> statDefinitions = Stats;
            for (int i = 0; i < statDefinitions.Count; i++)
            {
                for (int j = i + 1; j < statDefinitions.Count; j++)
                {
                    if (statDefinitions[j].StatId == statDefinitions[i].StatId)
                    {
                        Debug.LogWarning($"{name} has duplicate stat definition '{statDefinitions[i].StatId}'.", this);
                        break;
                    }
                }
            }
        }

        public bool IsValid()
        {
            return IsValid(out _);
        }

        public bool IsValid(out string reason)
        {
            IReadOnlyList<StatDefinition> statDefinitions = Stats;
            if (statDefinitions.Count == 0)
            {
                reason = $"{name} has no stat definitions.";
                return false;
            }

            foreach (StatId statId in AllStatIds)
            {
                if (!ContainsStat(statDefinitions, statId))
                {
                    reason = $"{name} is missing stat definition '{statId}'.";
                    return false;
                }
            }

            for (int i = 0; i < statDefinitions.Count; i++)
            {
                StatDefinition stat = statDefinitions[i];
                if (!stat.IsValid(out string statReason))
                {
                    reason = $"{name} has invalid stat definition at index {i}. {statReason}";
                    return false;
                }

                for (int j = i + 1; j < statDefinitions.Count; j++)
                {
                    if (statDefinitions[j].StatId == stat.StatId)
                    {
                        reason = $"{name} has duplicate stat definition '{stat.StatId}'.";
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

        public static StatDefinition CreateDefaultDefinition(StatId statId)
        {
            return statId switch
            {
                StatId.MaxHealth => new StatDefinition(statId, new StatBounds(1f, float.MaxValue)),
                _ => new StatDefinition(statId, new StatBounds(0f, float.MaxValue))
            };
        }

        private static StatDefinition[] CreateDefaultDefinitions()
        {
            var definitions = new StatDefinition[AllStatIds.Length];
            for (int i = 0; i < AllStatIds.Length; i++)
            {
                definitions[i] = CreateDefaultDefinition(AllStatIds[i]);
            }

            return definitions;
        }

        private static bool ContainsStat(IReadOnlyList<StatDefinition> statDefinitions, StatId statId)
        {
            for (int i = 0; i < statDefinitions.Count; i++)
            {
                if (statDefinitions[i].StatId == statId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
