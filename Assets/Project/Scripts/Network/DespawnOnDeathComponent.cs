using System.Collections;
using MP.Gameplay.Entity;
using Unity.Netcode;
using UnityEngine;

namespace MP.Network
{
    [RequireComponent(typeof(HealthComponent))]
    [RequireComponent(typeof(DeathVisualComponent))]
    public sealed class DespawnOnDeathComponent : MonoBehaviour
    {
        private HealthComponent health;
        private DeathVisualComponent deathVisual;
        private Coroutine despawnCoroutine;

        private void Awake()
        {
            health = GetComponent<HealthComponent>();
            deathVisual = GetComponent<DeathVisualComponent>();
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Died += OnDied;
                health.DeathStateChanged += OnDeathStateChanged;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= OnDied;
                health.DeathStateChanged -= OnDeathStateChanged;
            }

            if (despawnCoroutine != null)
            {
                StopCoroutine(despawnCoroutine);
                despawnCoroutine = null;
            }
        }

        private void OnDied(HealthComponent _)
        {
            deathVisual.Play();

            if (!NetworkContext.HasServerAuthority() || despawnCoroutine != null)
            {
                return;
            }

            despawnCoroutine = StartCoroutine(DespawnAfterFade());
        }

        private void OnDeathStateChanged(HealthComponent _, bool isDead)
        {
            if (isDead)
            {
                deathVisual.Play();
            }
        }

        private IEnumerator DespawnAfterFade()
        {
            yield return new WaitForSeconds(deathVisual.FadeDuration);
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
