using UnityEngine;

namespace MP.Gameplay.Damage
{
    public readonly struct DamageContext
    {
        public DamageContext(GameObject damageSource, GameObject instigator)
        {
            DamageSource = damageSource;
            Instigator = instigator;
        }

        public GameObject DamageSource { get; }
        public GameObject Instigator { get; }
        public GameObject RewardReceiver => Instigator != null ? Instigator : DamageSource;

        public static DamageContext None => new(null, null);

        public static DamageContext FromInstigator(GameObject instigator)
        {
            return new DamageContext(instigator, instigator);
        }
    }
}
