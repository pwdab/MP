using UnityEngine;

namespace MP.UI
{
    public sealed class FloatingWorldText : MonoBehaviour
    {
        private const float Lifetime = 0.9f;
        private const float FloatSpeed = 0.8f;

        private TextMesh textMesh;
        private float remainingTime;
        private Color baseColor;

        public static void Show(Vector3 position, string text, Color color)
        {
            var gameObject = new GameObject("FloatingWorldText");
            gameObject.transform.position = position;

            FloatingWorldText floatingText = gameObject.AddComponent<FloatingWorldText>();
            floatingText.Initialize(text, color);
        }

        private void Initialize(string text, Color color)
        {
            remainingTime = Lifetime;
            baseColor = color;
            textMesh = gameObject.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.fontSize = 32;
            textMesh.characterSize = 0.055f;
            textMesh.color = color;

            MeshRenderer renderer = GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = 140;
            }
        }

        private void Update()
        {
            remainingTime -= Time.deltaTime;
            transform.position += Vector3.up * FloatSpeed * Time.deltaTime;

            if (textMesh != null)
            {
                float alpha = Mathf.Clamp01(remainingTime / Lifetime);
                textMesh.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
            }

            if (remainingTime <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }
}
