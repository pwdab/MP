using UnityEngine;

namespace MP.Gameplay.Stages
{
    [System.Serializable]
    public sealed class EnemySpawnEntry
    {
        [Tooltip("Spawned enemy prefab for this weighted entry.")]
        [SerializeField] private GameObject enemyPrefab;

        [Min(0f)]
        [Tooltip("Relative spawn weight. Entries with 0 weight are ignored.")]
        [SerializeField] private float weight = 1f;

        public GameObject EnemyPrefab => enemyPrefab;
        public float Weight => Mathf.Max(0f, weight);

        public string DisplayName => enemyPrefab != null ? enemyPrefab.name : "Missing Enemy Prefab";

        internal void Validate()
        {
            weight = Mathf.Max(0f, weight);
        }
    }
}
