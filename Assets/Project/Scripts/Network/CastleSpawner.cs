using MP.Gameplay.Entity;
using UnityEngine;

namespace MP.Network
{
    public sealed class CastleSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject castlePrefab;
        [SerializeField] private Vector3 spawnPosition;

        private CastleEntity spawnedCastle;

        public CastleEntity SpawnedCastle => spawnedCastle;

        private void Update()
        {
            if (!NetworkContext.IsNetworkActive || !NetworkContext.HasServerAuthority() || spawnedCastle != null || castlePrefab == null)
            {
                return;
            }

            GameObject castleObject = Instantiate(castlePrefab, spawnPosition, Quaternion.identity);
            spawnedCastle = castleObject.GetComponent<CastleEntity>();
            NetworkSpawnUtility.TrySpawnNetworkObject(castleObject);
        }
    }
}

