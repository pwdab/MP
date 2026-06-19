using MP.Gameplay.Stats;
using UnityEngine;

namespace MP.Gameplay.Combat
{
    [RequireComponent(typeof(StatsComponent))]
    public sealed class CombatRangeIndicator : MonoBehaviour
    {
        [SerializeField] private int segmentCount = 64;
        [SerializeField] private float lineWidth = 0.035f;
        [SerializeField] private Color autoAttackColor = new(1f, 0.85f, 0.25f, 0.9f);
        [SerializeField] private Color projectileColor = new(0.4f, 0.75f, 1f, 0.8f);

        private StatsComponent statsComponent;
        private LineRenderer autoAttackLine;
        private LineRenderer projectileLine;

        private void Awake()
        {
            statsComponent = GetComponent<StatsComponent>();
            autoAttackLine = CreateLineRenderer("AutoAttackRange", autoAttackColor);
            projectileLine = CreateLineRenderer("ProjectileRange", projectileColor);
        }

        private void LateUpdate()
        {
            DrawCircle(autoAttackLine, statsComponent.Stats.AutoAttackRange);
            DrawCircle(projectileLine, statsComponent.Stats.ProjectileRange);
        }

        private LineRenderer CreateLineRenderer(string childName, Color color)
        {
            var child = new GameObject(childName);
            child.transform.SetParent(transform, false);

            LineRenderer lineRenderer = child.AddComponent<LineRenderer>();
            lineRenderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.useWorldSpace = false;
            lineRenderer.loop = true;
            lineRenderer.widthMultiplier = lineWidth;
            lineRenderer.positionCount = segmentCount;
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            lineRenderer.sortingOrder = -1;

            return lineRenderer;
        }

        private void DrawCircle(LineRenderer lineRenderer, float radius)
        {
            for (int i = 0; i < segmentCount; i++)
            {
                float angle = i / (float)segmentCount * Mathf.PI * 2f;
                lineRenderer.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
            }
        }
    }
}
