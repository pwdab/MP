using MP.Gameplay.Entity;
using MP.Gameplay.Stages;
using MP.Network;
using UnityEngine;

namespace MP.Gameplay.Movement
{
    [RequireComponent(typeof(CharacterStateComponent))]
    public sealed class PlayerKnockbackComponent : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float defaultDistance = 1f;
        [SerializeField, Min(0.01f)] private float defaultDuration = 0.5f;
        [SerializeField, Min(0f)] private float immunityDuration = 1f;

        private CharacterStateComponent characterState;
        private NetworkPlayerMovement networkMovement;
        private Vector2 direction;
        private float remainingDistance;
        private float speed;
        private float immunityRemainingTime;
        private bool isKnockbackActive;

        public bool IsKnockbackActive => isKnockbackActive;
        public bool IsImmune => immunityRemainingTime > 0f;
        public bool CanReceiveKnockback => !isKnockbackActive && immunityRemainingTime <= 0f;

        private void Awake()
        {
            characterState = GetComponent<CharacterStateComponent>();
            networkMovement = GetComponent<NetworkPlayerMovement>();
        }

        private void Update()
        {
            if (immunityRemainingTime > 0f)
            {
                immunityRemainingTime = Mathf.Max(0f, immunityRemainingTime - Time.deltaTime);
            }

            if (!NetworkContext.HasServerAuthority() || !StageSimulationGate.CanRunCombatSimulation() || !isKnockbackActive)
            {
                return;
            }

            TickKnockback(Time.deltaTime);
        }

        public bool TryApplyKnockback(Vector2 knockbackDirection)
        {
            return TryApplyKnockback(knockbackDirection, defaultDistance, defaultDuration);
        }

        public bool TryApplyKnockback(Vector2 knockbackDirection, float distance, float duration)
        {
            if (!NetworkContext.HasServerAuthority() || !CanReceiveKnockback)
            {
                return false;
            }

            if (knockbackDirection.sqrMagnitude <= 0.0001f)
            {
                knockbackDirection = Vector2.right;
            }

            direction = knockbackDirection.normalized;
            remainingDistance = Mathf.Max(0f, distance);
            float clampedDuration = Mathf.Max(0.01f, duration);
            speed = remainingDistance / clampedDuration;
            isKnockbackActive = remainingDistance > 0f;
            characterState.LockMovement(clampedDuration);

            if (!isKnockbackActive)
            {
                immunityRemainingTime = Mathf.Max(0f, immunityDuration);
            }

            return true;
        }

        public bool TryApplyKnockbackFrom(Vector2 sourcePosition)
        {
            Vector2 awayFromSource = (Vector2)transform.position - sourcePosition;
            return TryApplyKnockback(awayFromSource);
        }

        private void TickKnockback(float deltaTime)
        {
            float moveDistance = Mathf.Min(remainingDistance, speed * deltaTime);
            remainingDistance -= moveDistance;
            Vector3 nextPosition = transform.position + (Vector3)(direction * moveDistance);

            if (networkMovement != null)
            {
                networkMovement.SetServerPosition(nextPosition);
            }
            else
            {
                transform.position = nextPosition;
            }

            if (remainingDistance > 0f)
            {
                return;
            }

            isKnockbackActive = false;
            immunityRemainingTime = Mathf.Max(0f, immunityDuration);
        }
    }
}
