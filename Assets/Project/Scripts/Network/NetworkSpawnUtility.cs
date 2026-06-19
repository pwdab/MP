using System;
using Unity.Netcode;
using UnityEngine;

namespace MP.Network
{
    public static class NetworkSpawnUtility
    {
        public static bool TrySpawnNetworkObject(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return false;
            }

            if (!NetworkContext.IsNetworkActive)
            {
                return true;
            }

            if (!NetworkContext.IsServer)
            {
                return false;
            }

            if (!gameObject.TryGetComponent(out NetworkObject networkObject))
            {
                return false;
            }

            if (networkObject.IsSpawned)
            {
                return true;
            }

            if (!gameObject.scene.IsValid())
            {
                return false;
            }

            try
            {
                networkObject.Spawn();
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"{gameObject.name} failed to spawn as a NetworkObject: {exception.Message}", gameObject);
                return false;
            }
        }
    }
}
