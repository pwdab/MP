using MP.Gameplay.Combat;
using MP.Gameplay.Entity;
using MP.Gameplay.Stats;
using UnityEngine;

namespace MP.Gameplay.Stages
{
    /*
        플레이어 부활 규칙
        사망 대기 시간과 부활 위치/상태 복구를 관리하며, 서버 권한과 입력 처리는 외부 어댑터가 담당
    */
    [RequireComponent(typeof(HealthComponent))]
    [RequireComponent(typeof(StatsComponent))]
    public sealed class RespawnComponent : MonoBehaviour
    {
        [SerializeField] private bool autoRespawnOnDeath;
        [SerializeField] private bool respawnNearCastle;
        [SerializeField] private float castleRespawnRadius = 3f;

        private HealthComponent health;
        private StatsComponent stats;
        private CharacterStateComponent characterState;
        private PlayerActiveSkillAbilityComponent activeSkill;
        private bool isWaitingForRespawn;
        private float remainingRespawnTime;

        public bool AutoRespawnOnDeath => autoRespawnOnDeath;
        public bool IsWaitingForRespawn => isWaitingForRespawn;
        public float RemainingRespawnTime => Mathf.Max(0f, remainingRespawnTime);

        private void Awake()
        {
            health = GetComponent<HealthComponent>();
            stats = GetComponent<StatsComponent>();
            characterState = GetComponent<CharacterStateComponent>();
            activeSkill = GetComponent<PlayerActiveSkillAbilityComponent>();
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Died += OnDied;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= OnDied;
            }

            CancelPendingRespawn();
        }

        public void TickRespawn(float deltaTime)
        {
            if (!isWaitingForRespawn || deltaTime <= 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
            {
                return;
            }

            remainingRespawnTime = Mathf.Max(0f, remainingRespawnTime - deltaTime);
            if (remainingRespawnTime <= 0f)
            {
                Respawn();
            }
        }

        public void Respawn()
        {
            if (!health.IsDead)
            {
                return;
            }

            CancelPendingRespawn();
            MoveToRespawnPosition();
            health.RestoreToFullHealth();
            characterState?.ApplyRespawnState();
            activeSkill?.ResetCooldown();
            // TODO: Clear buffs and status effects when those systems are implemented.
        }

        public void BeginRespawnCountdown()
        {
            if (isWaitingForRespawn)
            {
                return;
            }

            isWaitingForRespawn = true;
            remainingRespawnTime = stats.GetValue(StatId.RespawnDelay);
        }

        private void OnDied(HealthComponent _)
        {
            if (autoRespawnOnDeath)
            {
                BeginRespawnCountdown();
            }
        }

        private void CancelPendingRespawn()
        {
            isWaitingForRespawn = false;
            remainingRespawnTime = 0f;
        }

        private void MoveToRespawnPosition()
        {
            if (!respawnNearCastle)
            {
                return;
            }

            CastleEntity castle = FindFirstObjectByType<CastleEntity>();
            if (castle == null)
            {
                return;
            }

            Vector2 offset = Random.insideUnitCircle * Mathf.Max(0f, castleRespawnRadius);
            transform.position = castle.transform.position + (Vector3)offset;
        }
    }
}
