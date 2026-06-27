using MP.Gameplay.Entity;
using System;
using UnityEngine;

namespace MP.Gameplay.Stages
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class GoldPickupComponent : MonoBehaviour
    {
        [SerializeField, Min(0)] private int goldAmount = 1;

        private bool collected;

        public int GoldAmount => Mathf.Max(0, goldAmount);
        public event Action<GoldPickupComponent, PlayerEntity> Collected;

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
            if (collected || other == null || !other.TryGetComponent(out PlayerEntity player))
            {
                return;
            }

            collected = true;
            Collected?.Invoke(this, player);
        }
    }
}
