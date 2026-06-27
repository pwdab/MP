using MP.Gameplay.Damage;
using MP.Gameplay.Entity;
using MP.Gameplay.Movement;
using UnityEngine;

namespace MP.Gameplay.Combat
{
    /*
        투사체 Gameplay 규칙
        이동, 수명, 최대 사거리, 대상 충돌, 피해, 넉백을 처리하며 네트워크 스폰/동기화는 외부 어댑터가 담당
    */
    public sealed class ProjectileComponent : MonoBehaviour
    {
        [SerializeField] private float speed = 8f;
        [SerializeField] private float radius = 0.18f;
        [SerializeField] private float lifetime = 2f;
        [SerializeField] private float knockbackDistance = 1f;
        [SerializeField] private float knockbackDuration = 0.5f;

        private Vector2 direction;
        private Vector2 spawnPosition;
        private TeamId ownerTeam;
        private float damage;
        private float maxDistance;
        private GameObject instigator;
        private float traveledDistance;
        private float remainingLifetime;

        public Vector2 Direction => direction;
        public Vector2 SpawnPosition => spawnPosition;
        public float MaxDistance => maxDistance;

        public void Configure(float moveSpeed, float hitRadius, float maxLifetime, float knockbackDistanceValue, float knockbackDurationValue)
        {
            speed = Mathf.Max(0f, moveSpeed);
            radius = Mathf.Max(0f, hitRadius);
            lifetime = Mathf.Max(0f, maxLifetime);
            knockbackDistance = Mathf.Max(0f, knockbackDistanceValue);
            knockbackDuration = Mathf.Max(0f, knockbackDurationValue);
        }

        public void Initialize(Vector2 spawnDirection, TeamId team, float projectileDamage, float projectileMaxDistance, GameObject projectileInstigator)
        {
            direction = IsFinite(spawnDirection) && spawnDirection.sqrMagnitude > 0.0001f ? spawnDirection.normalized : Vector2.right;
            spawnPosition = transform.position;
            ownerTeam = team;
            damage = Mathf.Max(0f, projectileDamage);
            maxDistance = Mathf.Max(0f, projectileMaxDistance);
            instigator = projectileInstigator;
            traveledDistance = 0f;
            remainingLifetime = Mathf.Max(0f, lifetime);
        }

        public void InitializeVisual(Vector2 serverSpawnPosition, Vector2 spawnDirection, float projectileMaxDistance, float elapsedTime)
        {
            direction = IsFinite(spawnDirection) && spawnDirection.sqrMagnitude > 0.0001f ? spawnDirection.normalized : Vector2.right;
            spawnPosition = IsFinite(serverSpawnPosition) ? serverSpawnPosition : transform.position;
            ownerTeam = TeamId.Neutral;
            damage = 0f;
            maxDistance = Mathf.Max(0f, projectileMaxDistance);
            instigator = null;

            float moveDistance = Mathf.Max(0f, speed) * Mathf.Max(0f, elapsedTime);
            transform.position = spawnPosition + direction * moveDistance;
            traveledDistance = moveDistance;
            remainingLifetime = Mathf.Max(0f, lifetime - elapsedTime);
        }

        public ProjectileTickResult Tick(float deltaTime, bool canApplyDamage)
        {
            if (deltaTime <= 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
            {
                return ProjectileTickResult.Running;
            }

            if (remainingLifetime <= 0f)
            {
                return ProjectileTickResult.Expired;
            }

            if (maxDistance <= 0f)
            {
                return ProjectileTickResult.ReachedMaxDistance;
            }

            float moveDistance = Mathf.Max(0f, speed) * deltaTime;
            transform.position += (Vector3)(direction * moveDistance);
            traveledDistance += moveDistance;
            remainingLifetime -= deltaTime;

            if (canApplyDamage && TryHitTarget())
            {
                return ProjectileTickResult.HitTarget;
            }

            if (remainingLifetime <= 0f)
            {
                return ProjectileTickResult.Expired;
            }

            if (traveledDistance >= maxDistance)
            {
                return ProjectileTickResult.ReachedMaxDistance;
            }

            return ProjectileTickResult.Running;
        }

        private bool TryHitTarget()
        {
            var targets = TargetRegistry.ActiveTargets;
            float clampedRadius = Mathf.Max(0f, radius);
            float radiusSqr = clampedRadius * clampedRadius;
            Vector2 position = transform.position;

            for (int i = 0; i < targets.Count; i++)
            {
                TargetableComponent target = targets[i];
                if (target == null || !target.IsTargetable || !TeamUtility.AreEnemies(ownerTeam, target.Team))
                {
                    continue;
                }

                if ((target.Position - position).sqrMagnitude > radiusSqr)
                {
                    continue;
                }

                DamageSystem.ApplyDamage(new DamageRequest(new DamageContext(gameObject, instigator), target.Health, damage));
                ApplyKnockbackIfNeeded(target);
                return true;
            }

            return false;
        }

        private void ApplyKnockbackIfNeeded(TargetableComponent target)
        {
            if (ownerTeam != TeamId.Enemy || target == null || target.Team != TeamId.Player)
            {
                return;
            }

            if (target.TryGetComponent(out PlayerKnockbackMovementComponent knockback))
            {
                knockback.TryApplyKnockback(direction, knockbackDistance, knockbackDuration);
            }
        }

        private static bool IsFinite(Vector2 value)
        {
            return !float.IsNaN(value.x) && !float.IsInfinity(value.x)
                && !float.IsNaN(value.y) && !float.IsInfinity(value.y);
        }
    }
}
