using MP.Gameplay.Combat;
using UnityEngine;

namespace MP.Network
{
    /*
        AutoProjectileAttackComponent의 Gameplay 발사 요청을 NetworkProjectile 생성으로 연결하는 어댑터
        공격 타이밍과 타겟 선택은 Gameplay가 담당하고, 이 컴포넌트는 네트워크 생성만 담당
    */
    [RequireComponent(typeof(AutoProjectileAttackComponent))]
    public sealed class NetworkAutoProjectileAttackAdapter : MonoBehaviour
    {
        private AutoProjectileAttackComponent autoProjectileAttack;

        private void Awake()
        {
            autoProjectileAttack = GetComponent<AutoProjectileAttackComponent>();
        }

        private void OnEnable()
        {
            if (autoProjectileAttack != null)
            {
                autoProjectileAttack.ProjectileRequested += OnProjectileRequested;
            }
        }

        private void OnDisable()
        {
            if (autoProjectileAttack != null)
            {
                autoProjectileAttack.ProjectileRequested -= OnProjectileRequested;
            }
        }

        private static void OnProjectileRequested(ProjectileSpawnRequest request)
        {
            if (!request.IsValid())
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
    }
}
