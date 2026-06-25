using System.Collections;
using MP.Gameplay.Combat;
using MP.Network;
using MP.UI;
using Unity.Netcode;
using UnityEngine;

namespace MP.Gameplay.Entity
{
    [RequireComponent(typeof(HealthComponent))]
    public sealed class DespawnOnDeathComponent : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float fadeDuration = 1f;

        private HealthComponent health;
        private SpriteRenderer spriteRenderer;
        private Coroutine fadeCoroutine;
        private Coroutine despawnCoroutine;

        private void Awake()
        {
            health = GetComponent<HealthComponent>();
            spriteRenderer = GetComponent<SpriteRenderer>();
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

            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null;
            }

            if (despawnCoroutine != null)
            {
                StopCoroutine(despawnCoroutine);
                despawnCoroutine = null;
            }
        }

        private void Start()
        {
            if (!TryGetComponent(out WorldCombatFeedbackComponent _))
            {
                gameObject.AddComponent<WorldCombatFeedbackComponent>();
            }
        }

        private void OnDied(HealthComponent _)
        {
            StartDeathVisuals();

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
                StartDeathVisuals();
            }
        }

        private void StartDeathVisuals()
        {
            HideDebugVisuals();

            if (TryGetComponent(out WorldCombatFeedbackComponent feedback))
            {
                feedback.CancelFlash(true);
            }

            if (fadeCoroutine == null)
            {
                fadeCoroutine = StartCoroutine(FadeOut());
            }
        }

        private void HideDebugVisuals()
        {
            foreach (CombatRangeIndicator indicator in GetComponentsInChildren<CombatRangeIndicator>(true))
            {
                indicator.enabled = false;
            }

            foreach (EnemyDetectionRangeIndicator indicator in GetComponentsInChildren<EnemyDetectionRangeIndicator>(true))
            {
                indicator.enabled = false;
            }

            foreach (LineRenderer lineRenderer in GetComponentsInChildren<LineRenderer>(true))
            {
                lineRenderer.enabled = false;
            }
        }

        private IEnumerator FadeOut()
        {
            if (spriteRenderer == null)
            {
                yield return new WaitForSeconds(fadeDuration);
                fadeCoroutine = null;
                yield break;
            }

            Color startColor = spriteRenderer.color;
            float duration = Mathf.Max(0.001f, fadeDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / duration);
                spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                yield return null;
            }

            spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, 0f);
            fadeCoroutine = null;
        }

        private IEnumerator DespawnAfterFade()
        {
            yield return new WaitForSeconds(Mathf.Max(0f, fadeDuration));
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
