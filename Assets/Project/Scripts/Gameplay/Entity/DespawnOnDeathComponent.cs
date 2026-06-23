using System.Collections;
using MP.Network;
using Unity.Netcode;
using UnityEngine;

namespace MP.Gameplay.Entity
{
    [RequireComponent(typeof(HealthComponent))]
    public sealed class DespawnOnDeathComponent : MonoBehaviour
    {
        [SerializeField] private float despawnDelay = 3f;

        private HealthComponent health;
        private Coroutine despawnCoroutine;

        private void Awake()
        {
            health = GetComponent<HealthComponent>();
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

            if (despawnCoroutine != null)
            {
                StopCoroutine(despawnCoroutine);
                despawnCoroutine = null;
            }
        }

        private void OnDied(HealthComponent _)
        {
            if (!NetworkContext.HasServerAuthority() || despawnCoroutine != null)
            {
                return;
            }

            despawnCoroutine = StartCoroutine(DespawnAfterDelay());
        }

        private IEnumerator DespawnAfterDelay()
        {
            yield return new WaitForSeconds(Mathf.Max(0f, despawnDelay));
            despawnCoroutine = null;

            if (!NetworkContext.HasServerAuthority())
            {
                yield break;
            }

            if (TryGetComponent(out NetworkObject networkObject) && networkObject.IsSpawned)
            {
                networkObject.Despawn();
                yield break;
            }

            Destroy(gameObject);
        }
    }
}
