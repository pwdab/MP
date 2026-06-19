using Unity.Netcode;

namespace MP.Network
{
    public static class NetworkContext
    {
        public static bool IsNetworkActive
        {
            get
            {
                NetworkManager networkManager = NetworkManager.Singleton;
                return networkManager != null && networkManager.IsListening;
            }
        }

        public static bool IsServer
        {
            get
            {
                NetworkManager networkManager = NetworkManager.Singleton;
                return networkManager != null && networkManager.IsListening && networkManager.IsServer;
            }
        }

        public static bool IsClient
        {
            get
            {
                NetworkManager networkManager = NetworkManager.Singleton;
                return networkManager != null && networkManager.IsListening && networkManager.IsClient;
            }
        }

        public static bool HasServerAuthority()
        {
            return !IsNetworkActive || IsServer;
        }
    }
}
