using MP.Gameplay.Events;
using UnityEngine;

namespace MP.Gameplay.Entity
{
    [RequireComponent(typeof(HealthComponent))]
    public sealed class EnemyEntity : MonoBehaviour
    {
        [SerializeField] private TeamId team = TeamId.Enemy;
        [SerializeField] private string enemyId;
        [SerializeField] private EnemyKilledEventChannel enemyKilledEventChannel;

        private HealthComponent health;

        public TeamId Team => team;
        public HealthComponent Health => health;
        public string EnemyId => string.IsNullOrWhiteSpace(enemyId) ? name : enemyId;

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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"{nameof(EnemyEntity)} on {name} has no EnemyKilledEventChannel.", this);
#endif
                return;
            }

            enemyKilledEventChannel.Raise(new EnemyKilledEvent(EnemyId, transform.position, health.LastDamageContext));
        }
    }
}
