using System;
using UnityEngine;

namespace MP.Gameplay.Stats
{
    /*
        특정 엔티티의 기본 스탯값 정의
        StatId와 BaseValue만 관리하고, 스탯의 허용 범위는 StatCatalogDefinition에서 조회
        같은 EntityStatsDefinition 안에서 동일한 StatId는 중복 등록하지 않아야 함
    */
    [Serializable]
    public struct StatEntry
    {
        [Tooltip("이 엔트리가 나타내는 스탯 종류입니다.")]
        [SerializeField] private StatId statId;

        [Tooltip("아이템, 직업, 스킬, 버프 Modifier가 적용되기 전의 기본값입니다.")]
        [SerializeField] private float baseValue;

        public StatEntry(StatId statId, float baseValue)
        {
            this.statId = statId;
            this.baseValue = baseValue;
        }

        public StatId StatId => statId;
        public float BaseValue => baseValue;

        public bool IsValid()
        {
            return IsValid(out _);
        }

        public bool IsValid(out string reason)
        {
            if (!Enum.IsDefined(typeof(StatId), statId))
            {
                reason = $"StatEntry has invalid StatId '{statId}'.";
                return false;
            }

            if (float.IsNaN(baseValue) || float.IsInfinity(baseValue))
            {
                reason = $"StatEntry '{statId}' has invalid base value '{baseValue}'.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        public void ValidateOrThrow()
        {
            if (!IsValid(out string reason))
            {
                throw new InvalidOperationException(reason);
            }
        }

        // statId 유지하고 baseValue만 바꾼 새 Entry를 생성하고 반환
        public StatEntry WithBaseValue(float value)
        {
            return new StatEntry(statId, value);
        }
    }
}
