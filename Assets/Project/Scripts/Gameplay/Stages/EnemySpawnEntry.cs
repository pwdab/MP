using System;
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

        public bool IsValid()
        {
            return IsValid(out _);
        }

        public bool IsValid(out string reason)
        {
            if (enemyPrefab == null)
            {
                reason = "EnemySpawnEntry enemy prefab is missing.";
                return false;
            }

            if (float.IsNaN(weight) || float.IsInfinity(weight) || weight < 0f)
            {
                reason = $"EnemySpawnEntry has invalid weight '{weight}'.";
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
            weight = Mathf.Max(0f, weight);
        }
    }
}
