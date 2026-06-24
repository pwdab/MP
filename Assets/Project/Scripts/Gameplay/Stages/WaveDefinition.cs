using UnityEngine;

namespace MP.Gameplay.Stages
{
    [System.Serializable]
    public sealed class WaveDefinition
    {
        [SerializeField] private string displayName = "Wave";
        [SerializeField] private float waveDuration = 30f;
        [SerializeField] private float spawnDuration = 20f;
        [SerializeField] private float spawnInterval = 2f;
        [SerializeField] private int maxAliveEnemies = 16;
        [SerializeField] private bool bossWave;
        [SerializeField] private GameObject bossPrefab;
        [SerializeField] private float bossSpawnTime = 15f;
        [SerializeField] private EnemySpawnEntry[] spawnEntries;

        public string DisplayName => displayName;
        public float WaveDuration => Mathf.Max(0f, waveDuration);
        public float SpawnDuration => Mathf.Max(0f, spawnDuration);
        public float SpawnInterval => Mathf.Max(0.1f, spawnInterval);
        public int MaxAliveEnemies => Mathf.Max(0, maxAliveEnemies);
        public bool BossWave => bossWave;
        public GameObject BossPrefab => bossPrefab;
        public float BossSpawnTime => Mathf.Max(0f, bossSpawnTime);
        public EnemySpawnEntry[] SpawnEntries => spawnEntries;
    }
}
