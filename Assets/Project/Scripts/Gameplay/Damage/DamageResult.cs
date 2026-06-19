using MP.Gameplay.Entity;
using UnityEngine;

namespace MP.Gameplay.Damage
{
    public readonly struct DamageResult
    {
        public DamageResult(GameObject attacker, HealthComponent target, float requestedDamage, float appliedDamage, bool killed)
        {
            Attacker = attacker;
            Target = target;
            RequestedDamage = requestedDamage;
            AppliedDamage = appliedDamage;
            Killed = killed;
        }

        public GameObject Attacker { get; }
        public HealthComponent Target { get; }
        public float RequestedDamage { get; }
        public float AppliedDamage { get; }
        public bool Killed { get; }
    }
}
