using UnityEngine;
using MP.Gameplay.Entity;
using MP.Gameplay.Stats;

namespace MP.Gameplay.Damage
{
    public static class DamageSystem
    {
        public static DamageResult ApplyDamage(DamageRequest request)
        {
            if (!request.IsValid(out string reason))
            {
                Debug.LogWarning(reason);
                return new DamageResult(request.Context, null, request.BaseDamage, 0f, false);
            }

            if (request.Target.TryGetComponent(out CharacterStateComponent state) && state.IsInvulnerable)
            {
                return new DamageResult(request.Context, request.Target, request.BaseDamage, 0f, false);
            }

            float finalDamage = ApplyDefense(request.Target, request.BaseDamage);
            float appliedDamage = request.Target.ApplyDamage(finalDamage, request.Context);

            return new DamageResult(
                request.Context,
                request.Target,
                request.BaseDamage,
                appliedDamage,
                request.Target.IsDead);
        }

        private static float ApplyDefense(HealthComponent target, float baseDamage)
        {
            float clampedBaseDamage = Mathf.Max(0f, baseDamage);
            if (!target.TryGetComponent(out StatsComponent stats))
            {
                return clampedBaseDamage;
            }

            float defense = stats.GetValue(StatId.Defense);
            if (defense <= 0f)
            {
                return clampedBaseDamage;
            }

            return clampedBaseDamage * 100f / defense;
        }
    }
}
