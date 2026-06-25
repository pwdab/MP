using MP.Gameplay.Entity;
using MP.Gameplay.Stages;
using MP.Gameplay.Stats;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MP.Gameplay.Movement
{
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(StatsComponent))]
    [RequireComponent(typeof(CharacterStateComponent))]
    public sealed class NetworkPlayerMovement : NetworkBehaviour
    {
        [SerializeField] private float predictionSnapThreshold = 1.25f;
        [SerializeField] private float maxClientDeltaTime = 0.05f;

        private readonly NetworkVariable<Vector2> serverPosition = new(Vector2.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private StatsComponent statsComponent;
        private CharacterStateComponent characterState;
        private NetworkTransform networkTransform;

        private void Awake()
        {
            statsComponent = GetComponent<StatsComponent>();
            characterState = GetComponent<CharacterStateComponent>();
            networkTransform = GetComponent<NetworkTransform>();
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                SetServerPosition(transform.position);
            }

            if (IsOwner && !IsServer && networkTransform != null)
            {
                networkTransform.enabled = false;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (networkTransform != null)
            {
                networkTransform.enabled = true;
            }
        }

        private void Update()
        {
            if (!IsOwner)
            {
                return;
            }

            if (!StageSimulationGate.CanAcceptPlayerInput())
            {
                if (!IsServer)
                {
                    SnapPredictionIfTooFar();
                }

                return;
            }

            if (!characterState.CanMove)
            {
                if (!IsServer)
                {
                    SnapPredictionIfTooFar();
                }

                return;
            }

            Vector2 input = ReadMoveInput();
            if (IsServer)
            {
                MoveServer(input, Time.deltaTime);
            }
            else
            {
                float deltaTime = Time.deltaTime;
                MoveLocal(input, deltaTime);
                MoveServerRpc(input, deltaTime);
                SnapPredictionIfTooFar();
            }
        }

        [ServerRpc]
        private void MoveServerRpc(Vector2 input, float clientDeltaTime)
        {
            if (!IsFinite(clientDeltaTime))
            {
                return;
            }

            MoveServer(input, Mathf.Clamp(clientDeltaTime, 0f, maxClientDeltaTime));
        }

        private void MoveServer(Vector2 input, float deltaTime)
        {
            if (!StageSimulationGate.CanAcceptPlayerInput())
            {
                return;
            }

            if (!characterState.CanMove)
            {
                return;
            }

            input = SanitizeInput(input);
            characterState.SetMoveDirection(input);

            transform.position += (Vector3)(input * statsComponent.MoveSpeed * deltaTime);
            SetServerPosition(transform.position);
        }

        private void MoveLocal(Vector2 input, float deltaTime)
        {
            input = SanitizeInput(input);
            characterState.SetMoveDirection(input);

            transform.position += (Vector3)(input * statsComponent.MoveSpeed * deltaTime);
        }

        public void SetServerPosition(Vector3 position)
        {
            if (!IsServer)
            {
                return;
            }

            if (!IsFinite(position))
            {
                return;
            }

            transform.position = position;
            var nextServerPosition = new Vector2(position.x, position.y);
            if ((serverPosition.Value - nextServerPosition).sqrMagnitude > 0.000001f)
            {
                serverPosition.Value = nextServerPosition;
            }
        }

        private void SnapPredictionIfTooFar()
        {
            Vector3 authoritativePosition = new(serverPosition.Value.x, serverPosition.Value.y, transform.position.z);
            Vector3 delta = authoritativePosition - transform.position;
            if (delta.sqrMagnitude <= predictionSnapThreshold * predictionSnapThreshold)
            {
                return;
            }

            transform.position = authoritativePosition;
        }

        private static Vector2 SanitizeInput(Vector2 input)
        {
            if (!IsFinite(input))
            {
                return Vector2.zero;
            }

            if (input.sqrMagnitude > 1f)
            {
                input.Normalize();
            }

            return input;
        }

        private static bool IsFinite(Vector2 value)
        {
            return IsFinite(value.x) && IsFinite(value.y);
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static Vector2 ReadMoveInput()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return Vector2.zero;
            }

            Vector2 input = Vector2.zero;
            if (keyboard.wKey.isPressed)
            {
                input.y += 1f;
            }

            if (keyboard.sKey.isPressed)
            {
                input.y -= 1f;
            }

            if (keyboard.aKey.isPressed)
            {
                input.x -= 1f;
            }

            if (keyboard.dKey.isPressed)
            {
                input.x += 1f;
            }

            return input;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#else
            return Vector2.zero;
#endif
        }
    }
}
