using MP.Gameplay.Entity;
using MP.Gameplay.Stages;
using MP.UI;
using Unity.Netcode;
using UnityEngine;

namespace MP.Network
{
    /*
        GoldPickupComponent의 수집 이벤트를 스테이지 골드 증가와 NetworkObject despawn으로 연결하는 어댑터
    */
    [RequireComponent(typeof(GoldPickupComponent))]
    public sealed class NetworkGoldPickupAdapter : MonoBehaviour
    {
        private GoldPickupComponent pickup;

        private void Awake()
        {
            pickup = GetComponent<GoldPickupComponent>();
        }

        private void OnEnable()
        {
            if (pickup != null)
            {
                pickup.Collected += OnCollected;
            }
        }

        private void OnDisable()
        {
            if (pickup != null)
            {
                pickup.Collected -= OnCollected;
            }
        }

        private void OnCollected(GoldPickupComponent collectedPickup, PlayerEntity _)
        {
            if (!NetworkContext.HasServerAuthority())
            {
                return;
            }

            StageFlowController stageFlow = FindFirstObjectByType<StageFlowController>();
            stageFlow?.AddGold(collectedPickup.GoldAmount);
            FloatingWorldText.Show(transform.position + Vector3.up * 0.5f, $"+{collectedPickup.GoldAmount} Gold", new Color(1f, 0.82f, 0.18f, 1f));
            DespawnOrDestroy();
        }

        private void DespawnOrDestroy()
        {
            if (TryGetComponent(out NetworkObject networkObject) && networkObject.IsSpawned)
            {
                networkObject.Despawn();
                return;
            }

            Destroy(gameObject);
        }
    }
}
