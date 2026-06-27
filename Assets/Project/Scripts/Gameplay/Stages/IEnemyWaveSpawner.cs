using MP.Gameplay.Entity;
using UnityEngine;

namespace MP.Gameplay.Stages
{
    /*
        StageFlowController가 적 스폰 구현을 직접 알지 않기 위한 웨이브 스폰 인터페이스
        Network 스폰, 로컬 스폰, 테스트 스폰 구현체가 같은 스테이지 흐름을 재사용할 수 있게 한다
    */
    public interface IEnemyWaveSpawner
    {
        void BeginWave(int waveIndex, WaveDefinition wave, CastleEntity castle);
        void StopSpawning();
        GameObject SpawnBoss(GameObject bossPrefab, int waveIndex, CastleEntity castle);
        int CountAliveBosses(int waveIndex);
        void KillAliveEnemies();
    }
}
