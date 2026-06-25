using System.Collections;
using MP.Gameplay.Combat;
using MP.Gameplay.Stages;
using MP.Gameplay.Stats;
using MP.Network;
using UnityEngine;

namespace MP.Gameplay.Entity
{
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
        private PlayerActiveSkillComponent activeSkill;
        private Coroutine respawnCoroutine;
        private float remainingRespawnTime;

        public bool IsWaitingForRespawn => respawnCoroutine != null;
        public float RemainingRespawnTime => Mathf.Max(0f, remainingRespawnTime);

        private void Awake()
        {
            health = GetComponent<HealthComponent>();
            stats = GetComponent<StatsComponent>();
            characterState = GetComponent<CharacterStateComponent>();
            activeSkill = GetComponent<PlayerActiveSkillComponent>();
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

            if (respawnCoroutine != null)
            {
                StopCoroutine(respawnCoroutine);
                respawnCoroutine = null;
                remainingRespawnTime = 0f;
            }
        }

        public void RespawnServer()
        {
            if (!NetworkContext.HasServerAuthority())
            {
                return;
            }

            if (!StageSimulationGate.CanRunCombatSimulation())
            {
                return;
            }

            if (!health.IsDead)
            {
                return;
            }

            CancelPendingRespawn();
            MoveToRespawnPosition();
            health.RestoreToFullHealth();
            characterState?.ApplyRespawnState();
            activeSkill?.ResetCooldownServer();
            remainingRespawnTime = 0f;
            // TODO: Clear buffs and status effects when those systems are implemented.
        }

        private void OnDied(HealthComponent _)
        {
            if (!autoRespawnOnDeath || !NetworkContext.HasServerAuthority() || respawnCoroutine != null)
            {
                return;
            }

            respawnCoroutine = StartCoroutine(RespawnAfterDelay());
        }

        private IEnumerator RespawnAfterDelay()
        {
            remainingRespawnTime = stats.RespawnDelay;
            while (remainingRespawnTime > 0f)
            {
                if (StageSimulationGate.CanRunCombatSimulation())
                {
                    remainingRespawnTime -= Time.deltaTime;
                }

                yield return null;
            }

            respawnCoroutine = null;
            remainingRespawnTime = 0f;
            RespawnServer();
        }

        private void CancelPendingRespawn()
        {
            if (respawnCoroutine == null)
            {
                return;
            }

            StopCoroutine(respawnCoroutine);
            respawnCoroutine = null;
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
