using Unity.Netcode;
using UnityEngine;

namespace MP.Network
{
    public struct HealthStateSnapshot : INetworkSerializable
    {
        private float currentHealth;
        private bool isDead;

        public HealthStateSnapshot(float currentHealth, bool isDead)
        {
            this.currentHealth = currentHealth;
            this.isDead = isDead;
        }

        public float CurrentHealth => currentHealth;
        public bool IsDead => isDead;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref currentHealth);
            serializer.SerializeValue(ref isDead);
        }

        public bool ApproximatelyEquals(HealthStateSnapshot other)
        {
            return Mathf.Approximately(currentHealth, other.currentHealth) && isDead == other.isDead;
        }
    }
}
