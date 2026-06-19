using MP.Gameplay.Entity;
using Unity.Netcode;
using UnityEngine;

namespace MP.Network
{
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(HealthComponent))]
    public sealed class NetworkHealthState : NetworkBehaviour
    {
        private readonly NetworkVariable<HealthStateSnapshot> healthState = new(new HealthStateSnapshot(0f, false), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private HealthComponent health;

        public float CurrentHealth => healthState.Value.CurrentHealth;
        public bool IsDead => healthState.Value.IsDead;

        private void Awake()
        {
            TryGetHealth();
        }

        public override void OnNetworkSpawn()
        {
            if (!TryGetHealth())
            {
                return;
            }

            healthState.OnValueChanged += OnHealthStateChanged;

            if (IsServer)
            {
                PushServerState();
                health.CurrentHealthChanged += OnServerCurrentHealthChanged;
                health.DeathStateChanged += OnServerDeathStateChanged;
            }
            else
            {
                ApplyReplicatedState(healthState.Value);
            }
        }

        public override void OnNetworkDespawn()
        {
            healthState.OnValueChanged -= OnHealthStateChanged;

            if (health != null && IsServer)
            {
                health.CurrentHealthChanged -= OnServerCurrentHealthChanged;
                health.DeathStateChanged -= OnServerDeathStateChanged;
            }
        }

        private void OnServerCurrentHealthChanged(HealthComponent _)
        {
            PushServerState();
        }

        private void OnServerDeathStateChanged(HealthComponent _, bool __)
        {
            PushServerState();
        }

        private void PushServerState()
        {
            var snapshot = new HealthStateSnapshot(health.CurrentHealth, health.IsDead);
            if (!healthState.Value.ApproximatelyEquals(snapshot))
            {
                healthState.Value = snapshot;
            }
        }

        private void OnHealthStateChanged(HealthStateSnapshot _, HealthStateSnapshot newValue)
        {
            if (!IsServer)
            {
                ApplyReplicatedState(newValue);
            }
        }

        private void ApplyReplicatedState(HealthStateSnapshot snapshot)
        {
            health.ApplyHealthStateSnapshot(snapshot.CurrentHealth, snapshot.IsDead);
        }

        private bool TryGetHealth()
        {
            if (health != null)
            {
                return true;
            }

            if (TryGetComponent(out health))
            {
                return true;
            }

            Debug.LogError($"{name} is missing a {nameof(HealthComponent)}.", this);
            return false;
        }
    }
}
