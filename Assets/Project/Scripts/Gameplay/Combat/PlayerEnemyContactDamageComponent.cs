using MP.Gameplay.Damage;
using MP.Gameplay.Entity;
using MP.Gameplay.Movement;
using UnityEngine;

namespace MP.Gameplay.Combat
{
    [RequireComponent(typeof(PlayerEntity))]
    [RequireComponent(typeof(HealthComponent))]
    [RequireComponent(typeof(PlayerKnockbackMovementComponent))]
    public sealed class PlayerEnemyContactDamageComponent : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float contactDamage = 5f;
        [SerializeField, Min(0f)] private float knockbackDistance = 1f;
        [SerializeField, Min(0.01f)] private float knockbackDuration = 0.5f;

        private HealthComponent health;
        private PlayerKnockbackMovementComponent knockback;
        private Collider2D playerCollider;

        private void Awake()
        {
            health = GetComponent<HealthComponent>();
            knockback = GetComponent<PlayerKnockbackMovementComponent>();
            playerCollider = GetComponent<Collider2D>();
        }

        public void TickContactDamage()
        {
            if (health == null || health.IsDead || knockback == null || !knockback.CanReceiveKnockback || playerCollider == null)
            {
                return;
            }

            EnemyEntity[] enemies = FindObjectsByType<EnemyEntity>(FindObjectsSortMode.None);
            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyEntity enemy = enemies[i];
                if (enemy == null || enemy.Health == null || enemy.Health.IsDead)
                {
                    continue;
                }

                if (!enemy.TryGetComponent(out Collider2D enemyCollider) || !IsOverlapping(enemyCollider))
                {
                    continue;
                }

                DamageResult result = DamageSystem.ApplyDamage(new DamageRequest(DamageContext.FromInstigator(enemy.gameObject), health, contactDamage));
                if (result.Killed)
                {
                    return;
                }

                Vector2 direction = (Vector2)transform.position - (Vector2)enemy.transform.position;
                knockback.TryApplyKnockback(direction, knockbackDistance, knockbackDuration);
                return;
            }
        }

        private bool IsOverlapping(Collider2D other)
        {
            ColliderDistance2D distance = playerCollider.Distance(other);
            return distance.isOverlapped || distance.distance <= 0f;
        }
    }
}
