using System;
using MP.Gameplay.Stats;
using UnityEngine;

namespace MP.Gameplay.Entity
{
    [RequireComponent(typeof(StatsComponent))]
    public sealed class HealthComponent : MonoBehaviour
    {
        private float currentHealth;

        public event Action<HealthComponent> Died;
        public event Action<HealthComponent> CurrentHealthChanged;
        public event Action<HealthComponent, bool> DeathStateChanged;
        public event Action<HealthComponent, float> Damaged;
        public event Action<HealthComponent, float> Healed;

        private StatsComponent stats;

        public float MaxHealth => stats.MaxHealth;

        public float CurrentHealth => currentHealth;
        public bool IsDead { get; private set; }
        public GameObject LastDamageSource { get; private set; }

        private void Awake()
        {
            stats = GetComponent<StatsComponent>();
            currentHealth = MaxHealth;
            IsDead = currentHealth <= 0f;
        }

        public void RestoreToFullHealth()
        {
            float previousHealth = currentHealth;
            bool wasDead = IsDead;

            currentHealth = MaxHealth;
            IsDead = false;
            LastDamageSource = null;

            RaiseCurrentHealthChanged(previousHealth);
            RaiseDeathStateChanged(wasDead);
        }

        public void ApplyHealthStateSnapshot(float health, bool isDead)
        {
            float previousHealth = currentHealth;
            bool wasDead = IsDead;

            // HP and death state are intentionally kept separate.
            currentHealth = Mathf.Clamp(health, 0f, MaxHealth);
            IsDead = isDead;

            RaiseCurrentHealthChanged(previousHealth);
            RaiseDeathStateChanged(wasDead);
        }

        public float ApplyDamage(float damage)
        {
            return ApplyDamage(damage, null);
        }

        public float ApplyDamage(float damage, GameObject damageSource)
        {
            if (IsDead || damage <= 0f)
            {
                return 0f;
            }

            LastDamageSource = damageSource;
            float previousHealth = currentHealth;
            bool wasDead = IsDead;
            currentHealth = Mathf.Max(0f, currentHealth - damage);
            float appliedDamage = previousHealth - currentHealth;

            if (currentHealth <= 0f)
            {
                IsDead = true;
            }

            if (appliedDamage > 0f)
            {
                RaiseCurrentHealthChanged(previousHealth);
                Damaged?.Invoke(this, appliedDamage);
            }

            RaiseDeathStateChanged(wasDead);

            if (!wasDead && IsDead)
            {
                Died?.Invoke(this);
            }

            return appliedDamage;
        }

        public float ApplyHeal(float amount)
        {
            if (IsDead || amount <= 0f)
            {
                return 0f;
            }

            float previousHealth = currentHealth;
            currentHealth = Mathf.Min(MaxHealth, currentHealth + amount);
            float healedAmount = currentHealth - previousHealth;

            if (healedAmount > 0f)
            {
                RaiseCurrentHealthChanged(previousHealth);
                Healed?.Invoke(this, healedAmount);
            }

            return healedAmount;
        }

        private void RaiseCurrentHealthChanged(float previousHealth)
        {
            if (!Mathf.Approximately(previousHealth, currentHealth))
            {
                CurrentHealthChanged?.Invoke(this);
            }
        }

        private void RaiseDeathStateChanged(bool wasDead)
        {
            if (wasDead != IsDead)
            {
                DeathStateChanged?.Invoke(this, IsDead);
            }
        }
    }
}
