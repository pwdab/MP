using UnityEngine;

namespace MP.Gameplay.Stages
{
    [System.Serializable]
    public sealed class EnemySpawnEntry
    {
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private float weight = 1f;

        public GameObject EnemyPrefab => enemyPrefab;
        public float Weight => Mathf.Max(0f, weight);
    }
}
