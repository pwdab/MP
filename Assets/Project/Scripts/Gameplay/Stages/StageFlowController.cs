using System;
using MP.Gameplay.Combat;
using MP.Gameplay.Entity;
using MP.Network;
using UnityEngine;

namespace MP.Gameplay.Stages
{
    public sealed class StageFlowController : MonoBehaviour
    {
        [SerializeField] private StageDefinition stageDefinition;
        [SerializeField] private CastleEntity castle;
        [SerializeField] private EnemySpawner enemySpawner;
        [SerializeField] private bool autoStart = true;
        [SerializeField] private float playerStartRadius = 3f;

        private float stageElapsedTime;
        private float waveElapsedTime;
        private int currentWaveIndex = -1;
        private int currentGold;
        private int currentExperience;
        private bool bossSpawned;

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

        public void AddGold(int amount)
        {
            if (!NetworkContext.HasServerAuthority())
            {
                return;
            }

            currentGold += Mathf.Max(0, amount);
        }

        public void AddExperience(int amount)
        {
            if (!NetworkContext.HasServerAuthority())
            {
                return;
            }

            currentExperience += Mathf.Max(0, amount);
        }

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            SubscribeCastle();
        }

        private void OnDisable()
        {
            UnsubscribeCastle();
        }

        private void Update()
        {
            if (!NetworkContext.HasServerAuthority())
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

            stageElapsedTime += Time.deltaTime;
            waveElapsedTime += Time.deltaTime;
            TickWave();
        }

        public void StartStage()
        {
            if (!NetworkContext.HasServerAuthority() || stageDefinition == null || castle == null)
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
            SetStageState(StageState.Playing);
            PositionPlayersAroundCastle();
            BeginNextWave();
        }

        public void ContinueFromRest()
        {
            if (!NetworkContext.HasServerAuthority() || CurrentStageState != StageState.Rest)
            {
                return;
            }

            SetStageState(StageState.Playing);
            BeginNextWave();
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
                bossSpawned = enemySpawner != null && enemySpawner.SpawnBossServer(CurrentWave.BossPrefab, currentWaveIndex, castle) != null;
            }

            if (CurrentWave.BossWave && bossSpawned && EnemySpawner.CountAliveBosses(currentWaveIndex) == 0)
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
            EnemySpawner.KillAliveEnemies();
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
                SetStageState(StageState.Rest);
                return;
            }

            BeginNextWave();
        }

        private void ClearStage()
        {
            enemySpawner?.StopSpawning();
            CurrentWave = null;
            CurrentWaveState = WaveState.Idle;
            SetStageState(StageState.Cleared);
        }

        private void FailStage()
        {
            enemySpawner?.StopSpawning();
            CurrentWave = null;
            CurrentWaveState = WaveState.Idle;
            SetStageState(StageState.Failed);
        }

        private void OnCastleDied(HealthComponent _)
        {
            if (NetworkContext.HasServerAuthority())
            {
                FailStage();
            }
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

                if (player.TryGetComponent(out PlayerActiveSkillComponent activeSkill))
                {
                    activeSkill.ResetCooldownServer();
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
                enemySpawner = FindFirstObjectByType<EnemySpawner>();
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
