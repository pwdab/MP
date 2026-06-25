using MP.Gameplay.Entity;
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
    }
}
