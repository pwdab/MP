using UnityEngine;

namespace MP.Gameplay.Entity
{
    [RequireComponent(typeof(HealthComponent))]
    public sealed class CastleEntity : MonoBehaviour
    {
        [SerializeField] private TeamId team = TeamId.Player;

        private HealthComponent health;

        public TeamId Team => team;
        public HealthComponent Health => health;
        public bool IsDestroyed => health != null && health.IsDead;

        private void Awake()
        {
            health = GetComponent<HealthComponent>();
        }

        private void OnEnable()
        {
            CastleEntityRegistry.Register(this);
        }

        private void OnDisable()
        {
            CastleEntityRegistry.Unregister(this);
        }
    }
}
