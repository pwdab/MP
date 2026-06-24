using MP.Gameplay.Combat;
using MP.Gameplay.Entity;
using MP.Gameplay.Movement;
using MP.Network;
using UnityEngine;

namespace MP.Gameplay.Stages
{
    public sealed class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private CastleEntity targetCastle;
        [SerializeField] private EnemySpawnPoint[] spawnPoints;
        [SerializeField] private float spawnInterval = 3f;
        [SerializeField] private int maxAliveEnemies = 12;
        [SerializeField] private bool spawnOnStart = true;

        private float spawnTimer;
        private int nextSpawnIndex;
        private int currentWaveIndex = -1;
        private WaveDefinition currentWave;
        private bool isSpawning;

        private void Awake()
        {
            if (targetCastle == null)
            {
                targetCastle = FindFirstObjectByType<CastleEntity>();
            }

            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                spawnPoints = FindObjectsByType<EnemySpawnPoint>(FindObjectsSortMode.None);
            }

            spawnTimer = spawnOnStart ? 0f : spawnInterval;
            isSpawning = spawnOnStart;
        }

        private void Update()
        {
            if (!isSpawning)
            {
                return;
            }

            if (targetCastle == null)
            {
                targetCastle = FindFirstObjectByType<CastleEntity>();
            }

            if (!NetworkContext.IsNetworkActive || !NetworkContext.HasServerAuthority() || enemyPrefab == null || targetCastle == null || targetCastle.IsDestroyed)
            {
                return;
            }

            spawnTimer -= Time.deltaTime;
            if (spawnTimer > 0f)
            {
                return;
            }

            spawnTimer = GetSpawnInterval();
            if (CountAliveEnemies() >= GetMaxAliveEnemies())
            {
                return;
            }

            SpawnEnemyServer(ChooseEnemyPrefab(), false);
        }

        public void BeginWave(int waveIndex, WaveDefinition wave, CastleEntity castle)
        {
            currentWaveIndex = waveIndex;
            currentWave = wave;
            targetCastle = castle != null ? castle : targetCastle;
            spawnTimer = 0f;
            isSpawning = true;
        }

        public void StopSpawning()
        {
            isSpawning = false;
        }

        public GameObject SpawnBossServer(GameObject bossPrefab, int waveIndex, CastleEntity castle)
        {
            targetCastle = castle != null ? castle : targetCastle;
            return SpawnEnemyServer(bossPrefab, true, waveIndex);
        }

        private GameObject SpawnEnemyServer(GameObject prefab, bool isBoss, int waveIndexOverride = -1)
        {
            if (prefab == null)
            {
                return null;
            }

            Vector3 spawnPosition = GetNextSpawnPosition();
            GameObject enemy = Instantiate(prefab, spawnPosition, Quaternion.identity);

            if (enemy.TryGetComponent(out EnemyTargetingComponent targeting))
            {
                targeting.SetFallbackCastle(targetCastle);
            }

            if (enemy.TryGetComponent(out EnemyMoveToCastleComponent movement))
            {
                movement.SetTargetCastle(targetCastle);
            }

            if (enemy.TryGetComponent(out EnemyCastleAttackComponent attack))
            {
                attack.SetTargetCastle(targetCastle);
            }

            WaveEnemyComponent waveEnemy = enemy.GetComponent<WaveEnemyComponent>();
            if (waveEnemy == null)
            {
                waveEnemy = enemy.AddComponent<WaveEnemyComponent>();
            }

            int waveIndex = waveIndexOverride >= 0 ? waveIndexOverride : currentWaveIndex;
            waveEnemy.Initialize(waveIndex, isBoss);

            NetworkSpawnUtility.TrySpawnNetworkObject(enemy);
            return enemy;
        }

        private Vector3 GetNextSpawnPosition()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                return transform.position;
            }

            int index = Mathf.Abs(nextSpawnIndex++) % spawnPoints.Length;
            EnemySpawnPoint spawnPoint = spawnPoints[index];
            return spawnPoint != null ? spawnPoint.Position : transform.position;
        }

        private float GetSpawnInterval()
        {
            return currentWave != null ? currentWave.SpawnInterval : Mathf.Max(0.1f, spawnInterval);
        }

        private int GetMaxAliveEnemies()
        {
            return currentWave != null ? currentWave.MaxAliveEnemies : Mathf.Max(0, maxAliveEnemies);
        }

        private GameObject ChooseEnemyPrefab()
        {
            EnemySpawnEntry[] entries = currentWave != null ? currentWave.SpawnEntries : null;
            if (entries == null || entries.Length == 0)
            {
                return enemyPrefab;
            }

            float totalWeight = 0f;
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i] != null && entries[i].EnemyPrefab != null)
                {
                    totalWeight += entries[i].Weight;
                }
            }

            if (totalWeight <= 0f)
            {
                return enemyPrefab;
            }

            float roll = Random.value * totalWeight;
            for (int i = 0; i < entries.Length; i++)
            {
                EnemySpawnEntry entry = entries[i];
                if (entry == null || entry.EnemyPrefab == null)
                {
                    continue;
                }

                roll -= entry.Weight;
                if (roll <= 0f)
                {
                    return entry.EnemyPrefab;
                }
            }

            return enemyPrefab;
        }

        public static int CountAliveEnemies()
        {
            EnemyEntity[] enemies = FindObjectsByType<EnemyEntity>(FindObjectsSortMode.None);
            int count = 0;
            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyEntity enemy = enemies[i];
                if (enemy != null && enemy.Health != null && !enemy.Health.IsDead)
                {
                    count++;
                }
            }

            return count;
        }

        public static int CountAliveBosses(int waveIndex)
        {
            WaveEnemyComponent[] enemies = FindObjectsByType<WaveEnemyComponent>(FindObjectsSortMode.None);
            int count = 0;
            for (int i = 0; i < enemies.Length; i++)
            {
                WaveEnemyComponent enemy = enemies[i];
                if (enemy == null || !enemy.IsBoss || enemy.WaveIndex != waveIndex)
                {
                    continue;
                }

                if (enemy.TryGetComponent(out HealthComponent health) && !health.IsDead)
                {
                    count++;
                }
            }

            return count;
        }

        public static void KillAliveEnemies()
        {
            EnemyEntity[] enemies = FindObjectsByType<EnemyEntity>(FindObjectsSortMode.None);
            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyEntity enemy = enemies[i];
                if (enemy != null && enemy.Health != null && !enemy.Health.IsDead)
                {
                    enemy.Health.ApplyDamage(float.MaxValue);
                }
            }
        }
    }
}
