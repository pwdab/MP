using System.Collections;
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

            if (!health.IsDead)
            {
                return;
            }

            CancelPendingRespawn();
            health.RestoreToFullHealth();
            // TODO: Move to respawn position, apply invulnerability, restore input, and clear status effects.
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
            yield return new WaitForSeconds(stats.RespawnDelay);
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
    }
}
