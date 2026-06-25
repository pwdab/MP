using System.Collections;
using MP.Gameplay.Entity;
using MP.Network;
using UnityEngine;

namespace MP.UI
{
    [RequireComponent(typeof(HealthComponent))]
    public sealed class WorldCombatFeedbackComponent : MonoBehaviour
    {
        [SerializeField] private Vector3 textOffset = new(0f, 1f, 0f);
        [SerializeField] private Color damageTextColor = new(1f, 0.25f, 0.2f, 1f);
        [SerializeField] private Color healTextColor = new(0.25f, 1f, 0.35f, 1f);
        [SerializeField] private Color deathTextColor = new(0.85f, 0.85f, 0.85f, 1f);
        [SerializeField] private Color flashColor = new(1f, 0.25f, 0.25f, 1f);
        [SerializeField, Min(0.01f)] private float flashDuration = 0.12f;

        private HealthComponent health;
        private SpriteRenderer spriteRenderer;
        private Color originalColor;
        private Coroutine flashCoroutine;
        private float lastObservedHealth;

        private void Awake()
        {
            health = GetComponent<HealthComponent>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }

            lastObservedHealth = health.CurrentHealth;
        }

        private void OnEnable()
        {
            if (health == null)
            {
                return;
            }

            health.Damaged += OnDamaged;
            health.Healed += OnHealed;
            health.Died += OnDied;
            health.CurrentHealthChanged += OnCurrentHealthChanged;
            health.DeathStateChanged += OnDeathStateChanged;
        }

        private void OnDisable()
        {
            if (health == null)
            {
                return;
            }

            health.Damaged -= OnDamaged;
            health.Healed -= OnHealed;
            health.Died -= OnDied;
            health.CurrentHealthChanged -= OnCurrentHealthChanged;
            health.DeathStateChanged -= OnDeathStateChanged;
        }

        private void OnDamaged(HealthComponent _, float amount)
        {
            lastObservedHealth = health.CurrentHealth;
            FloatingWorldText.Show(transform.position + textOffset, $"-{amount:0}", damageTextColor);
            Flash();
        }

        private void OnHealed(HealthComponent _, float amount)
        {
            lastObservedHealth = health.CurrentHealth;
            FloatingWorldText.Show(transform.position + textOffset, $"+{amount:0}", healTextColor);
        }

        private void OnDied(HealthComponent _)
        {
            FloatingWorldText.Show(transform.position + textOffset, "DOWN", deathTextColor);
        }

        private void OnCurrentHealthChanged(HealthComponent changedHealth)
        {
            if (NetworkContext.HasServerAuthority())
            {
                lastObservedHealth = changedHealth.CurrentHealth;
                return;
            }

            float delta = changedHealth.CurrentHealth - lastObservedHealth;
            lastObservedHealth = changedHealth.CurrentHealth;

            if (Mathf.Approximately(delta, 0f))
            {
                return;
            }

            if (delta < 0f)
            {
                FloatingWorldText.Show(transform.position + textOffset, $"{delta:0}", damageTextColor);
                Flash();
            }
            else
            {
                FloatingWorldText.Show(transform.position + textOffset, $"+{delta:0}", healTextColor);
            }
        }

        private void OnDeathStateChanged(HealthComponent _, bool isDead)
        {
            if (!NetworkContext.HasServerAuthority() && isDead)
            {
                FloatingWorldText.Show(transform.position + textOffset, "DOWN", deathTextColor);
            }
        }

        private void Flash()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
            }

            flashCoroutine = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            spriteRenderer.color = flashColor;
            yield return new WaitForSeconds(flashDuration);
            spriteRenderer.color = originalColor;
            flashCoroutine = null;
        }
    }
}
