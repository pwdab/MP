using MP.Gameplay.Movement;
using Unity.Netcode;
using UnityEngine;

namespace MP.Network
{
    [RequireComponent(typeof(NetworkObject))]
    public sealed class NetworkPlayerSpawnOffset : NetworkBehaviour
    {
        [SerializeField] private Vector2 origin = new(-3f, -2f);
        [SerializeField] private Vector2 step = new(2f, 0f);

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                return;
            }

            Vector2 position = origin + step * (float)OwnerClientId;
            transform.position = new Vector3(position.x, position.y, transform.position.z);

            if (TryGetComponent(out NetworkPlayerMovement movement))
            {
                movement.SetServerPosition(transform.position);
            }
        }
    }
}
