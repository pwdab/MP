using MP.Gameplay.Entity;
using MP.Network;
using UnityEngine;

namespace MP.Gameplay.Stages
{
    [RequireComponent(typeof(HealthComponent))]
    public sealed class EnemyGoldDropComponent : MonoBehaviour
    {
        [SerializeField, Min(0)] private int goldAmount = 1;
        [SerializeField] private GameObject goldPrefab;
        [SerializeField, Min(0f)] private float scatterRadius = 0.35f;

        private HealthComponent health;

        private void Awake()
        {
            health = GetComponent<HealthComponent>();
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Died += OnDied;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= OnDied;
            }
        }

        private void OnDied(HealthComponent _)
        {
            if (!NetworkContext.HasServerAuthority() || goldAmount <= 0 || goldPrefab == null)
            {
                return;
            }

            Vector2 offset = Random.insideUnitCircle * Mathf.Max(0f, scatterRadius);
            GameObject goldObject = Instantiate(goldPrefab, transform.position + (Vector3)offset, Quaternion.identity);
            if (goldObject.TryGetComponent(out GoldPickupComponent pickup))
            {
                pickup.Initialize(goldAmount);
            }

            NetworkSpawnUtility.TrySpawnNetworkObject(goldObject);
        }
    }
}
