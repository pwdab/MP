using UnityEngine;

namespace MP.Gameplay.Damage
{
    public static class DamageSystem
    {
        public static DamageResult ApplyDamage(DamageRequest request)
        {
            if (request.Target == null)
            {
                Debug.LogWarning("Damage request ignored because target is missing.");
                return new DamageResult(request.Attacker, null, request.BaseDamage, 0f, false);
            }

            float finalDamage = Mathf.Max(0f, request.BaseDamage);
            float appliedDamage = request.Target.ApplyDamage(finalDamage);

            return new DamageResult(
                request.Attacker,
                request.Target,
                request.BaseDamage,
                appliedDamage,
                request.Target.IsDead);
        }
    }
}
