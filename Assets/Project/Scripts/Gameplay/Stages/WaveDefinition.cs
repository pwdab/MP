using System;
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

        public bool IsValid()
        {
            return IsValid(out _);
        }

        public bool IsValid(out string reason)
        {
            if (float.IsNaN(waveDuration) || float.IsInfinity(waveDuration) || waveDuration < 0f)
            {
                reason = $"{DisplayName} has invalid wave duration '{waveDuration}'.";
                return false;
            }

            if (float.IsNaN(spawnDuration) || float.IsInfinity(spawnDuration) || spawnDuration < 0f || spawnDuration > waveDuration)
            {
                reason = $"{DisplayName} has invalid spawn duration '{spawnDuration}'.";
                return false;
            }

            if (float.IsNaN(spawnInterval) || float.IsInfinity(spawnInterval) || spawnInterval <= 0f)
            {
                reason = $"{DisplayName} has invalid spawn interval '{spawnInterval}'.";
                return false;
            }

            if (maxAliveEnemies < 0)
            {
                reason = $"{DisplayName} has invalid max alive enemies '{maxAliveEnemies}'.";
                return false;
            }

            if (bossWave)
            {
                if (bossPrefab == null)
                {
                    reason = $"{DisplayName} is a boss wave but has no boss prefab.";
                    return false;
                }

                if (float.IsNaN(bossSpawnTime) || float.IsInfinity(bossSpawnTime) || bossSpawnTime < 0f || bossSpawnTime > waveDuration)
                {
                    reason = $"{DisplayName} has invalid boss spawn time '{bossSpawnTime}'.";
                    return false;
                }
            }

            bool hasPositiveSpawnEntry = false;
            if (spawnEntries != null)
            {
                for (int i = 0; i < spawnEntries.Length; i++)
                {
                    EnemySpawnEntry entry = spawnEntries[i];
                    if (entry == null)
                    {
                        reason = $"{DisplayName} has an empty spawn entry at index {i}.";
                        return false;
                    }

                    if (!entry.IsValid(out string entryReason))
                    {
                        reason = $"{DisplayName} spawn entry {i} is invalid: {entryReason}";
                        return false;
                    }

                    hasPositiveSpawnEntry |= entry.Weight > 0f;
                }
            }

            if (!bossWave && !hasPositiveSpawnEntry)
            {
                reason = $"{DisplayName} has no usable enemy spawn entry.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        public void ValidateOrThrow()
        {
            if (!IsValid(out string reason))
            {
                throw new InvalidOperationException(reason);
            }
        }

        internal void Normalize()
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
                spawnEntries[i]?.Normalize();
            }
        }
    }
}
