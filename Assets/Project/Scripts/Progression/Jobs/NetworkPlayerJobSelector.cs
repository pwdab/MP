using System.Collections.Generic;
using MP.Gameplay.Stages;
using Unity.Netcode;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MP.Progression.Jobs
{
    [RequireComponent(typeof(PlayerJobComponent))]
    public sealed class NetworkPlayerJobSelector : NetworkBehaviour
    {
        [SerializeField] private JobDefinition[] availableJobs;

        private readonly NetworkVariable<int> selectedJobIndex = new(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private PlayerJobComponent playerJob;

        public IReadOnlyList<JobDefinition> AvailableJobs => availableJobs;
        public int SelectedJobIndex => selectedJobIndex.Value;
        public JobDefinition SelectedJob => TryGetJob(selectedJobIndex.Value);

        private void Awake()
        {
            playerJob = GetComponent<PlayerJobComponent>();
        }

        public override void OnNetworkSpawn()
        {
            selectedJobIndex.OnValueChanged += OnSelectedJobIndexChanged;

            if (IsServer && selectedJobIndex.Value < 0 && HasJob(0))
            {
                selectedJobIndex.Value = 0;
            }

            ApplySelectedJob(selectedJobIndex.Value);
        }

        public override void OnNetworkDespawn()
        {
            selectedJobIndex.OnValueChanged -= OnSelectedJobIndexChanged;
        }

        private void Update()
        {
            if (!IsOwner || !StageSimulationGate.CanAcceptPlayerInput())
            {
                return;
            }

            for (int i = 0; i < 6; i++)
            {
                if (WasNumberKeyPressed(i + 1))
                {
                    SelectJob(i);
                    break;
                }
            }
        }

        public void SelectJob(int jobIndex)
        {
            if (!StageSimulationGate.CanAcceptPlayerInput())
            {
                return;
            }

            if (!HasJob(jobIndex))
            {
                return;
            }

            if (IsServer)
            {
                SetSelectedJobIndex(jobIndex);
                return;
            }

            SelectJobServerRpc(jobIndex);
        }

        [ServerRpc]
        private void SelectJobServerRpc(int jobIndex)
        {
            SetSelectedJobIndex(jobIndex);
        }

        private void SetSelectedJobIndex(int jobIndex)
        {
            if (!StageSimulationGate.CanAcceptPlayerInput())
            {
                return;
            }

            if (!HasJob(jobIndex))
            {
                return;
            }

            selectedJobIndex.Value = jobIndex;
            ApplySelectedJob(jobIndex);
        }

        private void OnSelectedJobIndexChanged(int previousValue, int newValue)
        {
            ApplySelectedJob(newValue);
        }

        private void ApplySelectedJob(int jobIndex)
        {
            playerJob ??= GetComponent<PlayerJobComponent>();
            playerJob.SetJob(TryGetJob(jobIndex));
        }

        private JobDefinition TryGetJob(int jobIndex)
        {
            return HasJob(jobIndex) ? availableJobs[jobIndex] : null;
        }

        private bool HasJob(int jobIndex)
        {
            return availableJobs != null && jobIndex >= 0 && jobIndex < availableJobs.Length && availableJobs[jobIndex] != null;
        }

        private static bool WasNumberKeyPressed(int number)
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && number switch
            {
                1 => Keyboard.current.digit1Key.wasPressedThisFrame,
                2 => Keyboard.current.digit2Key.wasPressedThisFrame,
                3 => Keyboard.current.digit3Key.wasPressedThisFrame,
                4 => Keyboard.current.digit4Key.wasPressedThisFrame,
                5 => Keyboard.current.digit5Key.wasPressedThisFrame,
                6 => Keyboard.current.digit6Key.wasPressedThisFrame,
                _ => false
            };
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha0 + number));
#else
            return false;
#endif
        }
    }
}
