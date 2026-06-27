using MP.Gameplay.Entity;
using MP.Gameplay.Stages;
using Unity.Netcode;
using UnityEngine;

namespace MP.Network
{
    [RequireComponent(typeof(NetworkObject))]
    public sealed class NetworkTestCommands : NetworkBehaviour
    {
        public void RequestReviveLocalPlayer()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null || !networkManager.IsListening)
            {
                return;
            }

            if (IsServer)
            {
                RevivePlayerServer(networkManager.LocalClientId);
                return;
            }

            ReviveLocalPlayerServerRpc();
        }

        [ServerRpc(RequireOwnership = false)]
        private void ReviveLocalPlayerServerRpc(ServerRpcParams rpcParams = default)
        {
            RevivePlayerServer(rpcParams.Receive.SenderClientId);
        }

        private void RevivePlayerServer(ulong ownerClientId)
        {
            if (!IsServer)
            {
                return;
            }

            PlayerEntity[] players = FindObjectsByType<PlayerEntity>(FindObjectsSortMode.None);
            for (int i = 0; i < players.Length; i++)
            {
                PlayerEntity player = players[i];
                if (!player.TryGetComponent(out NetworkObject networkObject) || networkObject.OwnerClientId != ownerClientId)
                {
                    continue;
                }

                if (player.TryGetComponent(out NetworkRespawnAdapter respawnAdapter))
                {
                    respawnAdapter.RespawnServer();
                    return;
                }

                if (player.TryGetComponent(out RespawnComponent respawn))
                {
                    respawn.Respawn();
                    return;
                }
            }
        }
    }
}
