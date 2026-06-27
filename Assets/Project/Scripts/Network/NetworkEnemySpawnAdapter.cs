using MP.Gameplay.Stages;
using UnityEngine;

namespace MP.Network
{
    /*
        EnemySpawner가 생성한 적 GameObject를 NetworkObject로 spawn하는 어댑터
        적 선택, 웨이브 스폰 규칙은 Gameplay의 EnemySpawner가 담당한다
    */
    [RequireComponent(typeof(EnemySpawner))]
    public sealed class NetworkEnemySpawnAdapter : MonoBehaviour
    {
        private EnemySpawner enemySpawner;

        private void Awake()
        {
            enemySpawner = GetComponent<EnemySpawner>();
            enemySpawner.SetTickInUpdate(false);
        }

        private void OnEnable()
        {
            if (enemySpawner != null)
            {
                enemySpawner.EnemySpawned += OnEnemySpawned;
            }
        }

        private void OnDisable()
        {
            if (enemySpawner != null)
            {
                enemySpawner.EnemySpawned -= OnEnemySpawned;
            }
        }

        private void Update()
        {
            if (!NetworkContext.HasServerAuthority())
            {
                return;
            }

            enemySpawner.Tick(Time.deltaTime);
        }

        private static void OnEnemySpawned(GameObject enemy)
        {
            NetworkSpawnUtility.TrySpawnNetworkObject(enemy);
        }
    }
}
