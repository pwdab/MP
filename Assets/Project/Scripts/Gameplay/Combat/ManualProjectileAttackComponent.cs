using MP.Gameplay.Entity;
using MP.Gameplay.Stats;
using UnityEngine;

namespace MP.Gameplay.Combat
{
    /*
        플레이어 수동 투사체 공격 규칙
        입력과 네트워크 전송은 외부 어댑터가 담당하고, 이 컴포넌트는 공격 요청 생성에 필요한 Gameplay 데이터만 관리
    */
    [RequireComponent(typeof(StatsComponent))]
    [RequireComponent(typeof(CharacterStateComponent))]
    public sealed class ManualProjectileAttackComponent : MonoBehaviour
    {
        [SerializeField] private TeamId team = TeamId.Player;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform firePoint;

        private StatsComponent statsComponent;
        private CharacterStateComponent characterState;

        public float CurrentAttackRange => statsComponent != null ? statsComponent.GetValue(StatId.ManualProjectileRange) : 0f;

        private void Awake()
        {
            statsComponent = GetComponent<StatsComponent>();
            characterState = GetComponent<CharacterStateComponent>();
            if (firePoint == null)
            {
                firePoint = transform;
            }
        }

        public void Configure(TeamId ownerTeam, GameObject prefab, Transform origin)
        {
            team = ownerTeam;
            projectilePrefab = prefab;
            firePoint = origin != null ? origin : transform;
        }

        public bool TryCreateRequest(Vector2 aimWorldPosition, out ProjectileSpawnRequest request)
        {
            request = default;
            if (projectilePrefab == null || characterState == null || !characterState.CanAttack || !IsFinite(aimWorldPosition))
            {
                return false;
            }

            Transform origin = firePoint != null ? firePoint : transform;
            Vector2 firePosition = origin.position;
            Vector2 direction = aimWorldPosition - firePosition;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = Vector2.right;
            }
            else
            {
                direction.Normalize();
            }

            EntityRuntimeStats stats = statsComponent.Stats;
            request = new ProjectileSpawnRequest(
                projectilePrefab,
                origin.position,
                direction,
                team,
                stats.GetValue(StatId.AttackPower),
                CurrentAttackRange,
                gameObject);

            return request.IsValid();
        }

        private static bool IsFinite(Vector2 value)
        {
            return IsFinite(value.x) && IsFinite(value.y);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
