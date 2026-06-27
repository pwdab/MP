using System;
using MP.Gameplay.Entity;
using UnityEngine;

namespace MP.Gameplay.Stages
{
    /*
        스테이지 진행 상태 머신
        웨이브 시작/종료, 휴식/클리어/실패, 자원 값을 관리하며 네트워크 전송 방식은 알지 않는다
    */
    public sealed class StageFlowController : MonoBehaviour, IStageStateProvider, IStageSnapshotReceiver
    {
        [SerializeField] private StageDefinition stageDefinition;
        [SerializeField] private CastleEntity castle;
        [SerializeField] private MonoBehaviour enemySpawnerBehaviour;
        [SerializeField] private bool autoStart;
        [SerializeField] private bool tickInUpdate = true;
        [SerializeField] private float playerStartRadius = 3f;

        private IEnemyWaveSpawner enemySpawner;
        private float stageElapsedTime;
        private float waveElapsedTime;
        private int currentWaveIndex = -1;
        private int currentGold;
        private int currentExperience;
        private bool bossSpawned;
        private bool hasCurrentWave;

        public event Action<StageState> StageStateChanged;
        public event Action<int, WaveDefinition> WaveStarted;
        public event Action<int, WaveDefinition> WaveCleared;

        public StageState CurrentStageState { get; private set; } = StageState.NotStarted;
        public WaveState CurrentWaveState { get; private set; } = WaveState.Idle;
        public int CurrentWaveIndex => currentWaveIndex;
        public int CurrentWaveNumber => currentWaveIndex + 1;
        public int WaveCount => stageDefinition != null ? stageDefinition.WaveCount : 0;
        public float StageElapsedTime => stageElapsedTime;
        public float WaveElapsedTime => waveElapsedTime;
        public float CurrentWaveRemainingTime => Mathf.Max(0f, CurrentWave != null ? CurrentWave.WaveDuration - waveElapsedTime : 0f);
        public int CurrentGold => currentGold;
        public int CurrentExperience => currentExperience;
        public WaveDefinition CurrentWave { get; private set; }

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            StageSimulationGate.Register(this);
            SubscribeCastle();
        }

        private void OnDisable()
        {
            StageSimulationGate.Unregister(this);
            UnsubscribeCastle();
        }

        private void Update()
        {
            if (tickInUpdate)
            {
                Tick(Time.deltaTime);
            }
        }

        public void SetTickInUpdate(bool value)
        {
            tickInUpdate = value;
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
            {
                return;
            }

            ResolveReferences();
            if (autoStart && CurrentStageState == StageState.NotStarted && castle != null)
            {
                StartStage();
            }

            if (CurrentStageState != StageState.Playing || CurrentWave == null)
            {
                return;
            }

            stageElapsedTime += deltaTime;
            waveElapsedTime += deltaTime;
            TickWave();
        }

        public void StartStage()
        {
            if (stageDefinition == null || castle == null)
            {
                return;
            }

            UnsubscribeCastle();
            castle.Health.RestoreToFullHealth();
            SubscribeCastle();

            stageElapsedTime = 0f;
            waveElapsedTime = 0f;
            currentWaveIndex = -1;
            currentGold = stageDefinition.StartingGold;
            currentExperience = stageDefinition.StartingExperience;
            bossSpawned = false;
            hasCurrentWave = false;
            SetStageState(StageState.Playing);
            PositionPlayersAroundCastle();
            BeginNextWave();
        }

        public void ContinueFromRest()
        {
            if (CurrentStageState != StageState.Rest)
            {
                return;
            }

            SetStageState(StageState.Playing);
            BeginNextWave();
        }

        public void AddGold(int amount)
        {
            currentGold += Mathf.Max(0, amount);
        }

        public bool TrySpendGold(int amount)
        {
            int clampedAmount = Mathf.Max(0, amount);
            if (currentGold < clampedAmount)
            {
                return false;
            }

            currentGold -= clampedAmount;
            return true;
        }

        public void AddExperience(int amount)
        {
            currentExperience += Mathf.Max(0, amount);
        }

        public StageSnapshot CreateSnapshot()
        {
            return new StageSnapshot(CurrentStageState, CurrentWaveState, currentWaveIndex, hasCurrentWave, stageElapsedTime, waveElapsedTime, currentGold, currentExperience);
        }

        public void ApplySnapshot(StageSnapshot snapshot)
        {
            if (!snapshot.IsValid())
            {
                return;
            }

            StageState previousStageState = CurrentStageState;
            CurrentStageState = snapshot.StageState;
            CurrentWaveState = snapshot.WaveState;
            currentWaveIndex = snapshot.WaveIndex;
            hasCurrentWave = snapshot.HasCurrentWave;
            stageElapsedTime = Mathf.Max(0f, snapshot.StageElapsedTime);
            waveElapsedTime = Mathf.Max(0f, snapshot.WaveElapsedTime);
            currentGold = Mathf.Max(0, snapshot.Gold);
            currentExperience = Mathf.Max(0, snapshot.Experience);

            if (hasCurrentWave && stageDefinition != null && stageDefinition.TryGetWave(currentWaveIndex, out WaveDefinition wave))
            {
                CurrentWave = wave;
            }
            else
            {
                CurrentWave = null;
            }

            if (previousStageState != CurrentStageState)
            {
                StageStateChanged?.Invoke(CurrentStageState);
            }
        }

        private void TickWave()
        {
            if (CurrentWaveState == WaveState.Spawning && waveElapsedTime >= CurrentWave.SpawnDuration)
            {
                enemySpawner?.StopSpawning();
                CurrentWaveState = WaveState.WaitingForClear;
            }

            if (CurrentWave.BossWave && !bossSpawned && CurrentWave.BossPrefab != null && waveElapsedTime >= CurrentWave.BossSpawnTime)
            {
                bossSpawned = enemySpawner != null && enemySpawner.SpawnBoss(CurrentWave.BossPrefab, currentWaveIndex, castle) != null;
            }

            if (CurrentWave.BossWave && bossSpawned && enemySpawner != null && enemySpawner.CountAliveBosses(currentWaveIndex) == 0)
            {
                CompleteCurrentWave();
                return;
            }

            if (waveElapsedTime >= CurrentWave.WaveDuration)
            {
                CompleteCurrentWave();
            }
        }

        private void BeginNextWave()
        {
            currentWaveIndex++;
            if (!stageDefinition.TryGetWave(currentWaveIndex, out WaveDefinition wave))
            {
                ClearStage();
                return;
            }

            CurrentWave = wave;
            hasCurrentWave = true;
            waveElapsedTime = 0f;
            bossSpawned = false;
            CurrentWaveState = WaveState.Spawning;
            enemySpawner?.BeginWave(currentWaveIndex, wave, castle);
            WaveStarted?.Invoke(currentWaveIndex, wave);
        }

        private void CompleteCurrentWave()
        {
            if (CurrentWave == null || CurrentStageState != StageState.Playing)
            {
                return;
            }

            enemySpawner?.StopSpawning();
            enemySpawner?.KillAliveEnemies();
            CurrentWaveState = WaveState.Cleared;
            WaveCleared?.Invoke(currentWaveIndex, CurrentWave);

            bool isFinalWave = currentWaveIndex >= stageDefinition.WaveCount - 1;
            bool shouldPauseAfterBoss = CurrentWave.BossWave && !isFinalWave;

            if (isFinalWave)
            {
                ClearStage();
                return;
            }

            if (shouldPauseAfterBoss)
            {
                CurrentWave = null;
                hasCurrentWave = false;
                SetStageState(StageState.Rest);
                return;
            }

            BeginNextWave();
        }

        private void ClearStage()
        {
            enemySpawner?.StopSpawning();
            CurrentWave = null;
            hasCurrentWave = false;
            CurrentWaveState = WaveState.Idle;
            SetStageState(StageState.Cleared);
        }

        private void FailStage()
        {
            enemySpawner?.StopSpawning();
            CurrentWave = null;
            hasCurrentWave = false;
            CurrentWaveState = WaveState.Idle;
            SetStageState(StageState.Failed);
        }

        private void OnCastleDied(HealthComponent _)
        {
            FailStage();
        }

        private void PositionPlayersAroundCastle()
        {
            PlayerEntity[] players = FindObjectsByType<PlayerEntity>(FindObjectsSortMode.None);
            if (players.Length == 0)
            {
                return;
            }

            float radius = Mathf.Max(0f, playerStartRadius);
            float angleOffset = UnityEngine.Random.value * Mathf.PI * 2f;
            for (int i = 0; i < players.Length; i++)
            {
                PlayerEntity player = players[i];
                if (player == null)
                {
                    continue;
                }

                float angle = angleOffset + Mathf.PI * 2f * i / players.Length;
                float distance = UnityEngine.Random.Range(radius * 0.5f, radius);
                Vector2 offset = new(Mathf.Cos(angle), Mathf.Sin(angle));
                player.transform.position = castle.transform.position + (Vector3)(offset * distance);
                player.Health?.RestoreToFullHealth();

                if (player.TryGetComponent(out CharacterStateComponent state))
                {
                    state.ResetCombatState();
                }

                foreach (IStageStartResettable resettable in player.GetComponents<IStageStartResettable>())
                {
                    resettable.ResetForStageStart();
                }
            }
        }

        private void ResolveReferences()
        {
            if (castle == null)
            {
                castle = FindFirstObjectByType<CastleEntity>();
            }

            if (enemySpawner == null)
            {
                enemySpawner = enemySpawnerBehaviour as IEnemyWaveSpawner;
            }

            if (enemySpawner == null)
            {
                MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
                for (int i = 0; i < behaviours.Length; i++)
                {
                    if (behaviours[i] is IEnemyWaveSpawner spawner)
                    {
                        enemySpawner = spawner;
                        enemySpawnerBehaviour = behaviours[i];
                        break;
                    }
                }
            }
        }

        private void SubscribeCastle()
        {
            if (castle != null && castle.Health != null)
            {
                castle.Health.Died -= OnCastleDied;
                castle.Health.Died += OnCastleDied;
            }
        }

        private void UnsubscribeCastle()
        {
            if (castle != null && castle.Health != null)
            {
                castle.Health.Died -= OnCastleDied;
            }
        }

        private void SetStageState(StageState state)
        {
            if (CurrentStageState == state)
            {
                return;
            }

            CurrentStageState = state;
            StageStateChanged?.Invoke(state);
        }
    }
}
