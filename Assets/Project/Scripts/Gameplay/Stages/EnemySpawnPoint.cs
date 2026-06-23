using UnityEngine;

namespace MP.Gameplay.Stages
{
    public sealed class EnemySpawnPoint : MonoBehaviour
    {
        [SerializeField] private int spawnIndex;

        public int SpawnIndex => spawnIndex;
        public Vector3 Position => transform.position;
    }
}
