using MP.Gameplay.Entity;
using MP.Gameplay.Stages;
using MP.Gameplay.Stats;
using Unity.Netcode;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MP.Gameplay.Combat
{
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(StatsComponent))]
    [RequireComponent(typeof(CharacterStateComponent))]
    public sealed class NetworkProjectileLauncher : NetworkBehaviour
    {
        [SerializeField] private TeamId team = TeamId.Player;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform firePoint;

        private StatsComponent statsComponent;
        private CharacterStateComponent characterState;

        public float CurrentAttackRange => statsComponent != null ? statsComponent.ManualProjectileRange : 0f;

        private void Awake()
        {
            statsComponent = GetComponent<StatsComponent>();
            characterState = GetComponent<CharacterStateComponent>();
            if (firePoint == null)
            {
                firePoint = transform;
            }
        }

        private void Update()
        {
            if (!IsOwner || !StageSimulationGate.CanAcceptPlayerInput() || !characterState.CanAttack || !WasFirePressedThisFrame())
            {
                return;
            }

            Vector2 aimWorldPosition = GetAimWorldPosition();
            if (IsServer)
            {
                FireServer(aimWorldPosition);
            }
            else
            {
                FireServerRpc(aimWorldPosition);
            }
        }

        [ServerRpc]
        private void FireServerRpc(Vector2 aimWorldPosition)
        {
            FireServer(aimWorldPosition);
        }

        private void FireServer(Vector2 aimWorldPosition)
        {
            if (!StageSimulationGate.CanAcceptPlayerInput())
            {
                return;
            }

            if (projectilePrefab == null || !characterState.CanAttack || !IsFinite(aimWorldPosition))
            {
                return;
            }

            Vector2 firePosition = firePoint.position;
            Vector2 direction = aimWorldPosition - firePosition;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = Vector2.right;
            }
            else
            {
                direction.Normalize();
            }

            EntityRuntimeStats stats = statsComponent.Stats;
            NetworkProjectileSpawner.TrySpawn(projectilePrefab, firePoint.position, direction, team, stats.AttackPower, CurrentAttackRange, gameObject);
        }

        private Vector2 GetAimWorldPosition()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                return firePoint.position + Vector3.right;
            }

#if ENABLE_INPUT_SYSTEM
            Vector2 screenPosition = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
            Vector2 screenPosition = Input.mousePosition;
#endif
            Vector3 worldPosition = camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -camera.transform.position.z));
            return worldPosition;
        }

        private static bool IsFinite(Vector2 value)
        {
            return IsFinite(value.x) && IsFinite(value.y);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool WasFirePressedThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButtonDown(0);
#else
            return false;
#endif
        }
    }
}
