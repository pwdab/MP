using MP.Gameplay.Combat;
using MP.Gameplay.Entity;
using MP.Gameplay.Movement;
using MP.Network;
using Unity.Netcode;
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
        }

        private void Update()
        {
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

            spawnTimer = Mathf.Max(0.1f, spawnInterval);
            if (CountAliveEnemies() >= maxAliveEnemies)
            {
                return;
            }

            SpawnEnemyServer();
        }

        private void SpawnEnemyServer()
        {
            Vector3 spawnPosition = GetNextSpawnPosition();
            GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);

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

            NetworkSpawnUtility.TrySpawnNetworkObject(enemy);
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

        private static int CountAliveEnemies()
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
    }
}
