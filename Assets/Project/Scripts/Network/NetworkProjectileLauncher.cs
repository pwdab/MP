using MP.Gameplay.Combat;
using MP.Gameplay.Entity;
using MP.Gameplay.Stages;
using Unity.Netcode;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MP.Network
{
    /*
        수동 투사체 공격 네트워크 어댑터
        입력과 ServerRpc만 처리하고, 실제 공격 요청 생성은 ManualProjectileAttackComponent에 위임
    */
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(ManualProjectileAttackComponent))]
    public sealed class NetworkProjectileLauncher : NetworkBehaviour
    {
        [SerializeField] private TeamId team = TeamId.Player;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform firePoint;

        private ManualProjectileAttackComponent manualProjectileAttack;

        public float CurrentAttackRange => manualProjectileAttack != null ? manualProjectileAttack.CurrentAttackRange : 0f;

        private void Awake()
        {
            if (!TryGetComponent(out manualProjectileAttack))
            {
                manualProjectileAttack = gameObject.AddComponent<ManualProjectileAttackComponent>();
            }

            if (firePoint == null)
            {
                firePoint = transform;
            }

            if (projectilePrefab != null || firePoint != null)
            {
                manualProjectileAttack.Configure(team, projectilePrefab, firePoint);
            }
        }

        private void Update()
        {
            if (!IsOwner || !StageSimulationGate.CanAcceptPlayerInput() || !WasFirePressedThisFrame())
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

            if (!manualProjectileAttack.TryCreateRequest(aimWorldPosition, out ProjectileSpawnRequest request))
            {
                return;
            }

            NetworkProjectileSpawner.TrySpawn(
                request.ProjectilePrefab,
                request.Position,
                request.Direction,
                request.OwnerTeam,
                request.Damage,
                request.MaxDistance,
                request.Instigator);
        }

        private Vector2 GetAimWorldPosition()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                Transform origin = firePoint != null ? firePoint : transform;
                return origin.position + Vector3.right;
            }

#if ENABLE_INPUT_SYSTEM
            Vector2 screenPosition = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
            Vector2 screenPosition = Input.mousePosition;
#endif
            Vector3 worldPosition = camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -camera.transform.position.z));
            return worldPosition;
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
