using MP.Gameplay.Entity;
using System;

namespace MP.Gameplay.Damage
{
    public readonly struct DamageRequest
    {
        public DamageRequest(DamageContext context, HealthComponent target, float baseDamage)
        {
            Context = context;
            Target = target;
            BaseDamage = baseDamage;
        }

        public DamageContext Context { get; }
        public HealthComponent Target { get; }
        public float BaseDamage { get; }

        public bool IsValid()
        {
            return IsValid(out _);
        }

        public bool IsValid(out string reason)
        {
            if (Target == null)
            {
                reason = "DamageRequest target is missing.";
                return false;
            }

            if (float.IsNaN(BaseDamage) || float.IsInfinity(BaseDamage))
            {
                reason = $"DamageRequest has invalid base damage '{BaseDamage}'.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        public void ValidateOrThrow()
        {
            if (!IsValid(out string reason))
            {
                throw new InvalidOperationException(reason);
            }
        }
    }
}
