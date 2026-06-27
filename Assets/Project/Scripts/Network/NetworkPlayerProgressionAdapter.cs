using MP.Progression.Level;
using Unity.Netcode;
using UnityEngine;

namespace MP.Network
{
    /*
        PlayerProgressionComponent의 값을 NetworkVariable로 동기화하는 어댑터
        순수 진행도 로직은 PlayerProgressionComponent가, 네트워크 동기화는 이 어댑터가 담당한다.
    */
    [RequireComponent(typeof(PlayerProgressionComponent))]
    public sealed class NetworkPlayerProgressionAdapter : NetworkBehaviour
    {
        private readonly NetworkVariable<int> networkLevel = new(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> networkExperience = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> networkRemainingGrowthPoints = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private PlayerProgressionComponent progression;

        private void Awake()
        {
            progression = GetComponent<PlayerProgressionComponent>();
        }

        public override void OnNetworkSpawn()
        {
            networkLevel.OnValueChanged += OnProgressionChanged;
            networkExperience.OnValueChanged += OnProgressionChanged;
            networkRemainingGrowthPoints.OnValueChanged += OnProgressionChanged;

            if (IsServer)
            {
                PublishProgression();
            }
            else
            {
                ApplyNetworkProgression();
            }
        }

        public override void OnNetworkDespawn()
        {
            networkLevel.OnValueChanged -= OnProgressionChanged;
            networkExperience.OnValueChanged -= OnProgressionChanged;
            networkRemainingGrowthPoints.OnValueChanged -= OnProgressionChanged;
        }

        private void LateUpdate()
        {
            if (IsServer)
            {
                PublishProgression();
            }
        }

        private void PublishProgression()
        {
            networkLevel.Value = progression.Level;
            networkExperience.Value = progression.Experience;
            networkRemainingGrowthPoints.Value = progression.RemainingGrowthPoints;
        }

        private void ApplyNetworkProgression()
        {
            progression.ApplySnapshot(networkLevel.Value, networkExperience.Value, networkRemainingGrowthPoints.Value);
        }

        private void OnProgressionChanged(int _, int __)
        {
            ApplyNetworkProgression();
        }
    }
}
