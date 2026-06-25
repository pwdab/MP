using MP.Gameplay.Entity;
using MP.Gameplay.Stages;
using MP.Network;
using UnityEngine;

namespace MP.Gameplay.Movement
{
    [RequireComponent(typeof(PlayerEntity))]
    public sealed class PlayerSeparationComponent : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float padding = 0.02f;

        private Collider2D playerCollider;
        private NetworkPlayerMovement networkMovement;

        private void Awake()
        {
            playerCollider = GetComponent<Collider2D>();
            networkMovement = GetComponent<NetworkPlayerMovement>();
        }

        private void LateUpdate()
        {
            if (!NetworkContext.HasServerAuthority() || !StageSimulationGate.CanRunCombatSimulation() || playerCollider == null)
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
            if (networkMovement != null)
            {
                networkMovement.SetServerPosition(nextPosition);
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
