using UnityEngine;

namespace MP.Gameplay.Stages
{
    [System.Serializable]
    public sealed class WaveDefinition
    {
        [Header("Wave")]
        [Tooltip("Name shown in HUD and editor summaries.")]
        [SerializeField] private string displayName = "Wave";

        [Min(0f)]
        [Tooltip("Total wave duration. When this time ends, the wave is cleared.")]
        [SerializeField] private float waveDuration = 30f;

        [Min(0f)]
        [Tooltip("How long portals keep spawning enemies after the wave starts.")]
        [SerializeField] private float spawnDuration = 20f;

        [Min(0.1f)]
        [Tooltip("Seconds between enemy spawn attempts.")]
        [SerializeField] private float spawnInterval = 2f;

        [Min(0)]
        [Tooltip("Maximum number of alive enemies allowed before spawning pauses.")]
        [SerializeField] private int maxAliveEnemies = 16;

        [Header("Boss")]
        [Tooltip("If enabled, killing this wave's boss clears the wave.")]
        [SerializeField] private bool bossWave;

        [Tooltip("Boss prefab spawned during this wave.")]
        [SerializeField] private GameObject bossPrefab;

        [Min(0f)]
        [Tooltip("Seconds after wave start before the boss is spawned.")]
        [SerializeField] private float bossSpawnTime = 15f;

        [Header("Enemy Spawn Table")]
        [Tooltip("Weighted enemy prefab entries used by this wave.")]
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

        public string Summary
        {
            get
            {
                string bossText = bossWave ? $"Boss @ {BossSpawnTime:0.#}s" : "No Boss";
                return $"{DisplayName} | Wave {WaveDuration:0.#}s | Spawn {SpawnDuration:0.#}s / {SpawnInterval:0.##}s | Max {MaxAliveEnemies} | {bossText}";
            }
        }

        internal void Validate()
        {
            waveDuration = Mathf.Max(0f, waveDuration);
            spawnDuration = Mathf.Clamp(spawnDuration, 0f, waveDuration);
            spawnInterval = Mathf.Max(0.1f, spawnInterval);
            maxAliveEnemies = Mathf.Max(0, maxAliveEnemies);
            bossSpawnTime = Mathf.Clamp(bossSpawnTime, 0f, waveDuration);

            if (spawnEntries == null)
            {
                return;
            }

            for (int i = 0; i < spawnEntries.Length; i++)
            {
                spawnEntries[i]?.Validate();
            }
        }
    }
}
