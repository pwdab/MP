using MP.Gameplay.Entity;
using UnityEngine;

namespace MP.Gameplay.Combat
{
    [RequireComponent(typeof(HealthComponent))]
    public sealed class TargetableComponent : MonoBehaviour
    {
        [SerializeField] private TeamId team = TeamId.Enemy;

        private HealthComponent health;

        public TeamId Team => team;
        public HealthComponent Health => health;
        public Vector2 Position => transform.position;
        public bool IsTargetable => health != null && !health.IsDead && isActiveAndEnabled;

        private void Awake()
        {
            health = GetComponent<HealthComponent>();
        }

        private void OnEnable()
        {
            TargetRegistry.Register(this);
        }

        private void OnDisable()
        {
            TargetRegistry.Unregister(this);
        }
    }
}
