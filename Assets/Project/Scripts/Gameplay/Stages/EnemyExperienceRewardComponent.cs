using MP.Gameplay.Entity;
using MP.Network;
using MP.Progression.Level;
using UnityEngine;

namespace MP.Gameplay.Stages
{
    [RequireComponent(typeof(HealthComponent))]
    public sealed class EnemyExperienceRewardComponent : MonoBehaviour
    {
        [SerializeField, Min(0)] private int experienceAmount = 1;

        private HealthComponent health;

        private void Awake()
        {
            health = GetComponent<HealthComponent>();
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Died += OnDied;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= OnDied;
            }
        }

        private void OnDied(HealthComponent _)
        {
            if (!NetworkContext.HasServerAuthority() || experienceAmount <= 0)
            {
                return;
            }

            if (!TryGetKillerProgression(out PlayerProgressionComponent progression))
            {
                return;
            }

            progression.AddExperience(experienceAmount);
        }

        private bool TryGetKillerProgression(out PlayerProgressionComponent progression)
        {
            progression = null;
            GameObject damageSource = health.LastDamageSource;
            if (damageSource == null)
            {
                return false;
            }

            if (damageSource.TryGetComponent(out progression))
            {
                return true;
            }

            progression = damageSource.GetComponentInParent<PlayerProgressionComponent>();
            if (progression != null)
            {
                return true;
            }

            progression = damageSource.GetComponentInChildren<PlayerProgressionComponent>();
            return progression != null;
        }
    }
}
