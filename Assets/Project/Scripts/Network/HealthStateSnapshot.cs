using Unity.Netcode;
using UnityEngine;
using System;

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
        public bool IsValid()
        {
            return IsValid(out _);
        }

        public bool IsValid(out string reason)
        {
            if (float.IsNaN(currentHealth) || float.IsInfinity(currentHealth))
            {
                reason = $"HealthStateSnapshot has invalid current health '{currentHealth}'.";
                return false;
            }

            if (currentHealth < 0f)
            {
                reason = "HealthStateSnapshot current health is negative.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        public void ValidateOrThrow()
        {
            if (!IsValid(out string reason))
            {
                throw new InvalidOperationException(reason);
            }
        }

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
