using UnityEngine;

namespace MP.Gameplay.Entity
{
    [RequireComponent(typeof(HealthComponent))]
    public sealed class CharacterStateComponent : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float invulnerabilityDurationAfterRespawn = 1f;

        private HealthComponent health;
        private float invulnerabilityRemainingTime;
        private float movementLockRemainingTime;
        private Vector2 lastMoveDirection;

        public bool IsDead => health != null && health.IsDead;
        public bool IsInvulnerable => invulnerabilityRemainingTime > 0f;
        public bool HasMoveDirection => lastMoveDirection.sqrMagnitude > 0.0001f;
        public Vector2 LastMoveDirection => HasMoveDirection ? lastMoveDirection.normalized : Vector2.zero;
        public bool CanMove => !IsDead && movementLockRemainingTime <= 0f;
        public bool CanAttack => !IsDead;
        public bool CanUseSkill => !IsDead;

        private void Awake()
        {
            health = GetComponent<HealthComponent>();
        }

        private void Update()
        {
            if (invulnerabilityRemainingTime > 0f)
            {
                invulnerabilityRemainingTime = Mathf.Max(0f, invulnerabilityRemainingTime - Time.deltaTime);
            }

            if (movementLockRemainingTime > 0f)
            {
                movementLockRemainingTime = Mathf.Max(0f, movementLockRemainingTime - Time.deltaTime);
            }
        }

        public void SetMoveDirection(Vector2 direction)
        {
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            lastMoveDirection = direction.normalized;
        }

        public void ResetCombatState()
        {
            lastMoveDirection = Vector2.zero;
            movementLockRemainingTime = 0f;
            invulnerabilityRemainingTime = 0f;
        }

        public void ApplyRespawnState()
        {
            ResetCombatState();
            invulnerabilityRemainingTime = Mathf.Max(0f, invulnerabilityDurationAfterRespawn);
        }

        public void LockMovement(float duration)
        {
            movementLockRemainingTime = Mathf.Max(movementLockRemainingTime, Mathf.Max(0f, duration));
        }
    }
}
