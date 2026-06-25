using MP.Gameplay.Entity;
using MP.Network;
using MP.UI;
using Unity.Netcode;
using UnityEngine;

namespace MP.Gameplay.Stages
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class GoldPickupComponent : MonoBehaviour
    {
        [SerializeField, Min(0)] private int goldAmount = 1;

        public int GoldAmount => Mathf.Max(0, goldAmount);

        public void Initialize(int amount)
        {
            goldAmount = Mathf.Max(0, amount);
        }

        private void Reset()
        {
            Collider2D pickupCollider = GetComponent<Collider2D>();
            pickupCollider.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryCollect(other);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryCollect(collision.collider);
        }

        private void TryCollect(Collider2D other)
        {
            if (!NetworkContext.HasServerAuthority() || other == null || !other.TryGetComponent(out PlayerEntity _))
            {
                return;
            }

            StageFlowController stageFlow = FindFirstObjectByType<StageFlowController>();
            stageFlow?.AddGold(GoldAmount);
            FloatingWorldText.Show(transform.position + Vector3.up * 0.5f, $"+{GoldAmount} Gold", new Color(1f, 0.82f, 0.18f, 1f));
            DespawnOrDestroy();
        }

        private void DespawnOrDestroy()
        {
            if (TryGetComponent(out NetworkObject networkObject) && networkObject.IsSpawned)
            {
                networkObject.Despawn();
                return;
            }

            Destroy(gameObject);
        }
    }
}
