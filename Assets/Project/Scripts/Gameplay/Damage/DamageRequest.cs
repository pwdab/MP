using MP.Gameplay.Entity;
using UnityEngine;

namespace MP.Gameplay.Damage
{
    public readonly struct DamageRequest
    {
        public DamageRequest(GameObject attacker, HealthComponent target, float baseDamage)
        {
            Attacker = attacker;
            Target = target;
            BaseDamage = baseDamage;
        }

        public GameObject Attacker { get; }
        public HealthComponent Target { get; }
        public float BaseDamage { get; }
    }
}
