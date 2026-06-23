using MP.Gameplay.Stats;
using UnityEngine;

namespace MP.Gameplay.Combat
{
    [RequireComponent(typeof(StatsComponent))]
    public sealed class CombatRangeIndicator : MonoBehaviour
    {
        [SerializeField] private int segmentCount = 64;
        [SerializeField] private bool showAutoAttackRange = true;
        [SerializeField] private bool showProjectileRange = true;
        [SerializeField] private float autoAttackLineWidth = 0.035f;
        [SerializeField] private float projectileLineWidth = 0.055f;
        [SerializeField] private Color autoAttackColor = new(1f, 0.85f, 0.25f, 0.9f);
        [SerializeField] private Color projectileColor = new(0.1f, 0.55f, 1f, 1f);
        [SerializeField] private int sortingOrder = 50;

        private StatsComponent statsComponent;
        private LineRenderer autoAttackLine;
        private LineRenderer projectileLine;
        private bool hasManualProjectileAttack;

        private void Awake()
        {
            statsComponent = GetComponent<StatsComponent>();
            hasManualProjectileAttack = TryGetComponent(out NetworkProjectileLauncher _);
            autoAttackLine = CreateLineRenderer("AutoAttackRange", autoAttackColor, autoAttackLineWidth, sortingOrder);
            projectileLine = CreateLineRenderer("ManualProjectileRange", projectileColor, projectileLineWidth, sortingOrder + 1);
        }

        private void LateUpdate()
        {
            float autoAttackRange = statsComponent.Stats.AutoAttackRange;
            float projectileRange = statsComponent.Stats.ProjectileRange;

            DrawCircle(autoAttackLine, autoAttackRange, showAutoAttackRange);
            DrawCircle(projectileLine, projectileRange, showProjectileRange || hasManualProjectileAttack);
        }

        private LineRenderer CreateLineRenderer(string childName, Color color, float width, int lineSortingOrder)
        {
            var child = new GameObject(childName);
            child.transform.SetParent(transform, false);

            LineRenderer lineRenderer = child.AddComponent<LineRenderer>();
            lineRenderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.useWorldSpace = true;
            lineRenderer.loop = true;
            lineRenderer.widthMultiplier = width;
            lineRenderer.positionCount = segmentCount;
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            lineRenderer.sortingOrder = lineSortingOrder;

            return lineRenderer;
        }

        private void DrawCircle(LineRenderer lineRenderer, float radius, bool visible)
        {
            radius = Mathf.Max(0f, radius);
            lineRenderer.enabled = visible && radius > 0.001f;
            if (!lineRenderer.enabled)
            {
                return;
            }

            for (int i = 0; i < segmentCount; i++)
            {
                float angle = i / (float)segmentCount * Mathf.PI * 2f;
                Vector3 offset = new(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
                lineRenderer.SetPosition(i, transform.position + offset);
            }
        }
    }
}
