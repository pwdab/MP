using System.Collections;
using MP.Gameplay.Combat;
using MP.UI;
using UnityEngine;

namespace MP.Gameplay.Entity
{
    /*
        엔티티 사망 시 표시되는 순수 시각 처리
        네트워크 despawn 여부와 무관하게 디버그 선을 숨기고 SpriteRenderer를 서서히 투명하게 만든다
    */
    public sealed class DeathVisualComponent : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float fadeDuration = 1f;

        private SpriteRenderer spriteRenderer;
        private Coroutine fadeCoroutine;

        public float FadeDuration => Mathf.Max(0f, fadeDuration);

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            if (!TryGetComponent(out WorldCombatFeedbackComponent _))
            {
                gameObject.AddComponent<WorldCombatFeedbackComponent>();
            }
        }

        private void OnDisable()
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null;
            }
        }

        public void Play()
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
            foreach (LineRenderer lineRenderer in GetComponentsInChildren<LineRenderer>(true))
            {
                lineRenderer.enabled = false;
            }
        }

        private IEnumerator FadeOut()
        {
            if (spriteRenderer == null)
            {
                yield return new WaitForSeconds(FadeDuration);
                fadeCoroutine = null;
                yield break;
            }

            Color startColor = spriteRenderer.color;
            float duration = Mathf.Max(0.001f, FadeDuration);
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
    }
}
