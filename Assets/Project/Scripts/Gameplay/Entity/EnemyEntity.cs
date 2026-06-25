using MP.Gameplay.Events;
using UnityEngine;

namespace MP.Gameplay.Entity
{
    [RequireComponent(typeof(HealthComponent))]
    public sealed class EnemyEntity : MonoBehaviour
    {
        [SerializeField] private TeamId team = TeamId.Enemy;
        [SerializeField] private EnemyKilledEventChannel enemyKilledEventChannel;

        private HealthComponent health;

        public TeamId Team => team;
        public HealthComponent Health => health;

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
            RaiseEnemyKilled();
        }

        private void RaiseEnemyKilled()
        {
            if (enemyKilledEventChannel == null)
            {
                return;
            }

            enemyKilledEventChannel.Raise(new EnemyKilledEvent(this, health.LastDamageContext));
        }
    }
}
