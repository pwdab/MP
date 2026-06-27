using MP.Gameplay.Stats;
using UnityEngine;

namespace MP.Gameplay.Combat
{
    /*
        전투 범위 디버그 표시
        실제 판정에는 관여하지 않고, 현재 스탯과 공격 컴포넌트가 사용하는 범위를 시각화
    */
    [RequireComponent(typeof(StatsComponent))]
    public sealed class CombatRangeIndicator : MonoBehaviour
    {
        [SerializeField, Min(8)] private int segmentCount = 64;
        [SerializeField] private bool showAutoAttackRange = true;
        [SerializeField] private bool showAutoProjectileRange = true;
        [SerializeField] private bool showManualProjectileRange = true;
        [SerializeField] private bool showActiveSkillRange;
        [SerializeField] private float autoAttackLineWidth = 0.035f;
        [SerializeField] private float autoProjectileLineWidth = 0.045f;
        [SerializeField] private float manualProjectileLineWidth = 0.055f;
        [SerializeField] private float activeSkillLineWidth = 0.045f;
        [SerializeField] private Color autoAttackColor = new(1f, 0.85f, 0.25f, 0.9f);
        [SerializeField] private Color autoProjectileColor = new(0.1f, 0.55f, 1f, 1f);
        [SerializeField] private Color manualProjectileColor = new(0.15f, 1f, 1f, 1f);
        [SerializeField] private Color activeSkillColor = new(1f, 0.25f, 0.85f, 0.95f);
        [SerializeField] private int sortingOrder = 50;

        private StatsComponent statsComponent;
        private LineRenderer autoAttackLine;
        private LineRenderer autoProjectileLine;
        private LineRenderer manualProjectileLine;
        private LineRenderer activeSkillLine;
        private ManualProjectileAttackComponent manualProjectileAttack;
        private AutoProjectileAttackComponent autoProjectileAttack;
        private PlayerActiveSkillAbilityComponent activeSkill;
        private Material lineMaterial;

        private void Awake()
        {
            statsComponent = GetComponent<StatsComponent>();
            TryGetComponent(out manualProjectileAttack);
            TryGetComponent(out autoProjectileAttack);
            TryGetComponent(out activeSkill);
            lineMaterial = new Material(Shader.Find("Sprites/Default"));
            autoAttackLine = CreateLineRenderer("AutoAttackRange", autoAttackColor, autoAttackLineWidth, sortingOrder);
            autoProjectileLine = CreateLineRenderer("AutoProjectileRange", autoProjectileColor, autoProjectileLineWidth, sortingOrder + 1);
            manualProjectileLine = CreateLineRenderer("ManualProjectileRange", manualProjectileColor, manualProjectileLineWidth, sortingOrder + 2);
            activeSkillLine = CreateLineRenderer("ActiveSkillRange", activeSkillColor, activeSkillLineWidth, sortingOrder + 3);
        }

        private void OnDestroy()
        {
            if (lineMaterial != null)
            {
                Destroy(lineMaterial);
            }
        }

        private void LateUpdate()
        {
            ResolveOptionalComponents();

            float autoAttackRange = statsComponent.GetValue(StatId.AutoAttackRange);
            float autoProjectileRange = autoProjectileAttack != null ? autoProjectileAttack.CurrentAttackRange : 0f;
            float manualProjectileRange = manualProjectileAttack != null ? manualProjectileAttack.CurrentAttackRange : statsComponent.GetValue(StatId.ManualProjectileRange);
            float activeSkillRange = activeSkill != null ? activeSkill.Radius : 0f;

            DrawCircle(autoAttackLine, autoAttackRange, showAutoAttackRange);
            DrawCircle(autoProjectileLine, autoProjectileRange, showAutoProjectileRange && autoProjectileAttack != null);
            DrawCircle(manualProjectileLine, manualProjectileRange, showManualProjectileRange && manualProjectileAttack != null);
            DrawCircle(activeSkillLine, activeSkillRange, showActiveSkillRange && activeSkill != null);
        }

        private void ResolveOptionalComponents()
        {
            if (manualProjectileAttack == null)
            {
                TryGetComponent(out manualProjectileAttack);
            }

            if (autoProjectileAttack == null)
            {
                TryGetComponent(out autoProjectileAttack);
            }

            if (activeSkill == null)
            {
                TryGetComponent(out activeSkill);
            }
        }

        private LineRenderer CreateLineRenderer(string childName, Color color, float width, int lineSortingOrder)
        {
            var child = new GameObject(childName);
            child.transform.SetParent(transform, false);

            LineRenderer lineRenderer = child.AddComponent<LineRenderer>();
            lineRenderer.sharedMaterial = lineMaterial;
            lineRenderer.useWorldSpace = true;
            lineRenderer.loop = true;
            lineRenderer.widthMultiplier = width;
            lineRenderer.positionCount = Mathf.Max(8, segmentCount);
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

            int clampedSegmentCount = Mathf.Max(8, segmentCount);
            if (lineRenderer.positionCount != clampedSegmentCount)
            {
                lineRenderer.positionCount = clampedSegmentCount;
            }

            for (int i = 0; i < clampedSegmentCount; i++)
            {
                float angle = i / (float)clampedSegmentCount * Mathf.PI * 2f;
                Vector3 offset = new(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
                lineRenderer.SetPosition(i, transform.position + offset);
            }
        }
    }
}
