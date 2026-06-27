using System;
using UnityEngine;

namespace MP.Gameplay.Stats
{
    /*
        스탯 종류별 공통 규칙을 정의
        엔티티별 수치가 아니라, 해당 스탯의 허용 범위와 기본 속성을 관리
        게임 전체의 스탯 규칙은 StatCatalogDefinition 에셋에서 한 번만 정의하는 것을 전제로 함
    */
    [Serializable]
    public struct StatDefinition
    {
        [Tooltip("이 정의가 나타내는 스탯 종류입니다.")]
        [SerializeField] private StatId statId;

        [Tooltip("이 스탯이 Modifier 적용 후 가질 수 있는 최종값 범위입니다.")]
        [SerializeField] private StatBounds bounds;

        public StatDefinition(StatId statId, StatBounds bounds)
        {
            this.statId = statId;
            this.bounds = bounds;
        }

        public StatId StatId => statId;
        public StatBounds Bounds => bounds;

        public bool IsValid()
        {
            return IsValid(out _);
        }

        public bool IsValid(out string reason)
        {
            if (!Enum.IsDefined(typeof(StatId), statId))
            {
                reason = $"StatDefinition has invalid StatId '{statId}'.";
                return false;
            }

            if (!bounds.IsValid(out reason))
            {
                reason = $"StatDefinition '{statId}' has invalid bounds. {reason}";
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

        public StatDefinition WithValidRange()
        {
            return new StatDefinition(statId, bounds.WithValidRange());
        }
    }
}
