using UnityEngine;

namespace MP.Gameplay.Entity
{
    [RequireComponent(typeof(HealthComponent))]
    public sealed class CharacterStateComponent : MonoBehaviour
    {
        private HealthComponent health;

        public bool IsDead => health != null && health.IsDead;
        public bool CanMove => !IsDead;
        public bool CanAttack => !IsDead;
        public bool CanUseSkill => !IsDead;

        private void Awake()
        {
            health = GetComponent<HealthComponent>();
        }
    }
}
