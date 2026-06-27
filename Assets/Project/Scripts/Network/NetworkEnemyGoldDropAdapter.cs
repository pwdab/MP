using MP.Gameplay.Stages;
using UnityEngine;

namespace MP.Network
{
    /*
        EnemyGoldDropComponent가 생성한 골드 오브젝트를 네트워크 오브젝트로 스폰하는 어댑터
    */
    [RequireComponent(typeof(EnemyGoldDropComponent))]
    public sealed class NetworkEnemyGoldDropAdapter : MonoBehaviour
    {
        private EnemyGoldDropComponent goldDrop;

        private void Awake()
        {
            goldDrop = GetComponent<EnemyGoldDropComponent>();
        }

        private void OnEnable()
        {
            if (goldDrop != null)
            {
                goldDrop.GoldDropped += OnGoldDropped;
            }
        }

        private void OnDisable()
        {
            if (goldDrop != null)
            {
                goldDrop.GoldDropped -= OnGoldDropped;
            }
        }

        private void OnGoldDropped(GameObject goldObject)
        {
            if (!NetworkContext.HasServerAuthority() || goldObject == null)
            {
                return;
            }

            if (!goldObject.TryGetComponent(out NetworkGoldPickupAdapter _))
            {
                goldObject.AddComponent<NetworkGoldPickupAdapter>();
            }

            NetworkSpawnUtility.TrySpawnNetworkObject(goldObject);
        }
    }
}
