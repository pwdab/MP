using UnityEngine;

namespace MP.Gameplay.Entity
{
    [RequireComponent(typeof(HealthComponent))]
    public sealed class PlayerEntity : MonoBehaviour
    {
        [SerializeField] private TeamId team = TeamId.Player;

        private HealthComponent health;

        public TeamId Team => team;
        public HealthComponent Health => health;

        private void Awake()
        {
            health = GetComponent<HealthComponent>();
        }
    }
}
