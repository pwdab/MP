using MP.Gameplay.Entity;
using UnityEngine;

namespace MP.Gameplay.Movement
{
    [RequireComponent(typeof(PlayerEntity))]
    public sealed class PlayerCollisionSeparationComponent : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float padding = 0.02f;

        private Collider2D playerCollider;
        private PlayerEntityMovementComponent movement;

        private void Awake()
        {
            playerCollider = GetComponent<Collider2D>();
            movement = GetComponent<PlayerEntityMovementComponent>();
        }

        public void TickSeparation()
        {
            if (playerCollider == null)
            {
                return;
            }

            PlayerEntity[] players = FindObjectsByType<PlayerEntity>(FindObjectsSortMode.None);
            for (int i = 0; i < players.Length; i++)
            {
                PlayerEntity other = players[i];
                if (other == null || other.gameObject == gameObject || !other.TryGetComponent(out Collider2D otherCollider))
                {
                    continue;
                }

                ColliderDistance2D distance = playerCollider.Distance(otherCollider);
                if (!distance.isOverlapped && distance.distance > 0f)
                {
                    continue;
                }

                Vector2 direction = (Vector2)transform.position - (Vector2)other.transform.position;
                if (direction.sqrMagnitude <= 0.0001f)
                {
                    direction = GetStableDirection(gameObject.GetInstanceID(), other.gameObject.GetInstanceID());
                }

                float separationDistance = Mathf.Abs(distance.distance) * 0.5f + padding;
                MoveServer(direction.normalized * separationDistance);
            }
        }

        private void MoveServer(Vector2 offset)
        {
            Vector3 nextPosition = transform.position + (Vector3)offset;
            if (movement != null)
            {
                movement.SetPosition(nextPosition);
                return;
            }

            transform.position = nextPosition;
        }

        private static Vector2 GetStableDirection(int firstId, int secondId)
        {
            float angle = (firstId < secondId ? 0.25f : 1.25f) * Mathf.PI;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }
    }
}


