using System;
using MP.Gameplay.Entity;
using UnityEngine;

namespace MP.Gameplay.Stages
{
    /*
        적 사망 시 골드 오브젝트를 생성하는 Gameplay 규칙
        생성된 오브젝트를 네트워크에 스폰할지는 Network 어댑터가 결정
    */
    [RequireComponent(typeof(HealthComponent))]
    public sealed class EnemyGoldDropComponent : MonoBehaviour
    {
        [SerializeField, Min(0)] private int goldAmount = 1;
        [SerializeField] private GameObject goldPrefab;
        [SerializeField, Min(0f)] private float scatterRadius = 0.35f;

        private HealthComponent health;

        public event Action<GameObject> GoldDropped;

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
            if (goldAmount <= 0 || goldPrefab == null)
            {
                return;
            }

            Vector2 offset = UnityEngine.Random.insideUnitCircle * Mathf.Max(0f, scatterRadius);
            GameObject goldObject = Instantiate(goldPrefab, transform.position + (Vector3)offset, Quaternion.identity);
            if (goldObject.TryGetComponent(out GoldPickupComponent pickup))
            {
                pickup.Initialize(goldAmount);
            }

            GoldDropped?.Invoke(goldObject);
        }
    }
}
