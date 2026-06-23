using MP.Gameplay.Entity;
using UnityEngine;

namespace MP.Gameplay.Combat
{
    [RequireComponent(typeof(EnemyTargetingComponent))]
    public sealed class EnemyDetectionRangeIndicator : MonoBehaviour
    {
        [SerializeField] private int segmentCount = 64;
        [SerializeField] private float lineWidth = 0.03f;
        [SerializeField] private Color color = new(1f, 0.25f, 0.25f, 0.35f);
        [SerializeField] private int sortingOrder = 49;

        private EnemyTargetingComponent targeting;
        private LineRenderer line;

        private void Awake()
        {
            targeting = GetComponent<EnemyTargetingComponent>();
            line = CreateLineRenderer();
        }

        private void LateUpdate()
        {
            DrawCircle(Mathf.Max(0f, targeting.PlayerDetectionRange));
        }

        private LineRenderer CreateLineRenderer()
        {
            var child = new GameObject("PlayerDetectionRange");
            child.transform.SetParent(transform, false);

            LineRenderer lineRenderer = child.AddComponent<LineRenderer>();
            lineRenderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.useWorldSpace = true;
            lineRenderer.loop = true;
            lineRenderer.widthMultiplier = lineWidth;
            lineRenderer.positionCount = segmentCount;
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            lineRenderer.sortingOrder = sortingOrder;
            return lineRenderer;
        }

        private void DrawCircle(float radius)
        {
            line.enabled = radius > 0.001f;
            if (!line.enabled)
            {
                return;
            }

            for (int i = 0; i < segmentCount; i++)
            {
                float angle = i / (float)segmentCount * Mathf.PI * 2f;
                Vector3 offset = new(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
                line.SetPosition(i, transform.position + offset);
            }
        }
    }
}
