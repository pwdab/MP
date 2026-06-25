using MP.Gameplay.Entity;
namespace MP.Gameplay.Damage
{
    public readonly struct DamageResult
    {
        public DamageResult(DamageContext context, HealthComponent target, float requestedDamage, float appliedDamage, bool killed)
        {
            Context = context;
            Target = target;
            RequestedDamage = requestedDamage;
            AppliedDamage = appliedDamage;
            Killed = killed;
        }

        public DamageContext Context { get; }
        public HealthComponent Target { get; }
        public float RequestedDamage { get; }
        public float AppliedDamage { get; }
        public bool Killed { get; }
    }
}
