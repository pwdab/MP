using MP.Gameplay.Entity;
using MP.Network;
using UnityEngine;

namespace MP.UI
{
    [RequireComponent(typeof(HealthComponent))]
    public sealed class WorldHealthLabel : MonoBehaviour
    {
        [SerializeField] private Vector3 offset = new(0f, 0.75f, 0f);
        [SerializeField] private int fontSize = 28;
        [SerializeField] private Color color = Color.white;

        private HealthComponent health;
        private NetworkHealthState networkHealth;
        private TextMesh textMesh;

        private void Awake()
        {
            health = GetComponent<HealthComponent>();
            TryGetComponent(out networkHealth);
            textMesh = CreateTextMesh();
        }

        private void LateUpdate()
        {
            if (health == null || textMesh == null)
            {
                return;
            }

            float currentHealth = networkHealth != null ? networkHealth.CurrentHealth : health.CurrentHealth;
            textMesh.text = $"{currentHealth:0}/{health.MaxHealth:0}";
            textMesh.transform.position = transform.position + offset;
            textMesh.transform.rotation = Quaternion.identity;
        }

        private TextMesh CreateTextMesh()
        {
            var label = new GameObject("HealthLabel");
            label.transform.SetParent(transform, false);
            label.transform.localPosition = offset;

            TextMesh mesh = label.AddComponent<TextMesh>();
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.fontSize = fontSize;
            mesh.characterSize = 0.06f;
            mesh.color = color;

            MeshRenderer renderer = label.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = 100;
            }

            return mesh;
        }
    }
}
