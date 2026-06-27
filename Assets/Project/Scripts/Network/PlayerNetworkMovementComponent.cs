using MP.Gameplay.Movement;
using MP.Gameplay.Stages;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MP.Network
{
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(PlayerEntityMovementComponent))]
    public sealed class PlayerNetworkMovementComponent : NetworkBehaviour
    {
        [SerializeField] private float predictionSnapThreshold = 1.25f;
        [SerializeField] private float predictionCorrectionRate = 12f;
        [SerializeField] private float maxClientDeltaTime = 0.05f;

        private readonly NetworkVariable<Vector2> serverPosition = new(Vector2.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private PlayerEntityMovementComponent movement;
        private NetworkTransform networkTransform;

        private void Awake()
        {
            movement = GetComponent<PlayerEntityMovementComponent>();
            networkTransform = GetComponent<NetworkTransform>();

            if (networkTransform != null)
            {
                // 보간을 끄면 비소유 클라이언트에서 서버 위치가 보간 버퍼 지연 없이 즉시 반영된다(정지 시 미끄러짐 제거).
                networkTransform.Interpolate = false;
            }
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
                ReconcileClientPredictionIfNeeded();
                return;
            }

            if (!movement.CanMove)
            {
                ReconcileClientPredictionIfNeeded();
                return;
            }

            Vector2 input = ReadMoveInput();
            if (IsServer)
            {
                MoveServer(input, Time.deltaTime);
                return;
            }

            float deltaTime = Time.deltaTime;
            movement.Move(input, deltaTime);
            MoveServerRpc(input, deltaTime);
            ReconcilePrediction();
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
            if (!StageSimulationGate.CanAcceptPlayerInput() || !movement.CanMove)
            {
                return;
            }

            movement.Move(input, deltaTime);
            SetServerPosition(transform.position);
        }

        public void SetServerPosition(Vector3 position)
        {
            if (!IsServer || !IsFinite(position))
            {
                return;
            }

            movement.SetPosition(position);
            var nextServerPosition = new Vector2(position.x, position.y);
            if ((serverPosition.Value - nextServerPosition).sqrMagnitude > 0.000001f)
            {
                serverPosition.Value = nextServerPosition;
            }
        }

        private void ReconcileClientPredictionIfNeeded()
        {
            if (!IsServer)
            {
                ReconcilePrediction();
            }
        }

        private void ReconcilePrediction()
        {
            Vector3 authoritativePosition = new(serverPosition.Value.x, serverPosition.Value.y, transform.position.z);
            Vector3 delta = authoritativePosition - transform.position;
            if (delta.sqrMagnitude > predictionSnapThreshold * predictionSnapThreshold)
            {
                movement.SetPosition(authoritativePosition);
                return;
            }

            float correction = Mathf.Clamp01(predictionCorrectionRate * Time.deltaTime);
            movement.SetPosition(transform.position + delta * correction);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
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
