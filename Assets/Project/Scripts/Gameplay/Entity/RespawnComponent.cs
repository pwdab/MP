using System.Collections;
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
        private Coroutine respawnCoroutine;

        private void Awake()
        {
            health = GetComponent<HealthComponent>();
            stats = GetComponent<StatsComponent>();
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
            // TODO: Apply invulnerability, restore input, and clear status effects.
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
            float remainingDelay = stats.RespawnDelay;
            while (remainingDelay > 0f)
            {
                if (StageSimulationGate.CanRunCombatSimulation())
                {
                    remainingDelay -= Time.deltaTime;
                }

                yield return null;
            }

            respawnCoroutine = null;
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
