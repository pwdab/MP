using MP.Gameplay.Entity;
using UnityEngine;

namespace MP.Gameplay.Movement
{
    [RequireComponent(typeof(CharacterStateComponent))]
    public sealed class PlayerMoveDirectionDebugIndicator : MonoBehaviour
    {
        [SerializeField] private bool showDebugDirection = true;
        [SerializeField] private float length = 1.1f;
        [SerializeField] private float headLength = 0.25f;
        [SerializeField] private float headAngle = 28f;
        [SerializeField] private float lineWidth = 0.055f;
        [SerializeField] private Color color = new(0.2f, 1f, 0.35f, 1f);
        [SerializeField] private int sortingOrder = 80;

        private CharacterStateComponent characterState;
        private LineRenderer shaftLine;
        private LineRenderer leftHeadLine;
        private LineRenderer rightHeadLine;

        private void Awake()
        {
            characterState = GetComponent<CharacterStateComponent>();
            shaftLine = CreateLineRenderer("MoveDirectionShaft");
            leftHeadLine = CreateLineRenderer("MoveDirectionHeadLeft");
            rightHeadLine = CreateLineRenderer("MoveDirectionHeadRight");
        }

        private void LateUpdate()
        {
            bool visible = showDebugDirection && characterState != null && characterState.HasMoveDirection;
            SetVisible(visible);
            if (!visible)
            {
                return;
            }

            Vector2 direction = characterState.LastMoveDirection;
            Vector3 origin = transform.position;
            Vector3 tip = origin + (Vector3)(direction * Mathf.Max(0f, length));
            Vector2 leftHeadDirection = Rotate(-direction, headAngle);
            Vector2 rightHeadDirection = Rotate(-direction, -headAngle);

            SetLine(shaftLine, origin, tip);
            SetLine(leftHeadLine, tip, tip + (Vector3)(leftHeadDirection * Mathf.Max(0f, headLength)));
            SetLine(rightHeadLine, tip, tip + (Vector3)(rightHeadDirection * Mathf.Max(0f, headLength)));
        }

        private LineRenderer CreateLineRenderer(string childName)
        {
            var child = new GameObject(childName);
            child.transform.SetParent(transform, false);

            LineRenderer lineRenderer = child.AddComponent<LineRenderer>();
            lineRenderer.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.useWorldSpace = true;
            lineRenderer.positionCount = 2;
            lineRenderer.widthMultiplier = lineWidth;
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            lineRenderer.sortingOrder = sortingOrder;
            lineRenderer.enabled = false;
            return lineRenderer;
        }

        private void SetVisible(bool visible)
        {
            shaftLine.enabled = visible;
            leftHeadLine.enabled = visible;
            rightHeadLine.enabled = visible;
        }

        private static void SetLine(LineRenderer lineRenderer, Vector3 start, Vector3 end)
        {
            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, end);
        }

        private static Vector2 Rotate(Vector2 direction, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float sin = Mathf.Sin(radians);
            float cos = Mathf.Cos(radians);
            return new Vector2(direction.x * cos - direction.y * sin, direction.x * sin + direction.y * cos).normalized;
        }
    }
}
