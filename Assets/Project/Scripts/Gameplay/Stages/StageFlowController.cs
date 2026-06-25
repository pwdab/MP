using System;
using MP.Gameplay.Combat;
using MP.Gameplay.Entity;
using MP.Network;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace MP.Gameplay.Stages
{
    public sealed class StageFlowController : MonoBehaviour
    {
        private const string StageSnapshotMessageName = "MP.StageFlow.Snapshot";

        [SerializeField] private StageDefinition stageDefinition;
        [SerializeField] private CastleEntity castle;
        [SerializeField] private EnemySpawner enemySpawner;
        [SerializeField] private bool autoStart;
        [SerializeField] private float playerStartRadius = 3f;
        [SerializeField, Min(0.02f)] private float snapshotSendInterval = 0.1f;

        private float stageElapsedTime;
        private float waveElapsedTime;
        private int currentWaveIndex = -1;
        private int currentGold;
        private int currentExperience;
        private bool bossSpawned;
        private bool hasCurrentWave;
        private bool registeredSnapshotHandler;
        private float snapshotSendTimer;

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
            PublishStageSnapshot();
        }

        public bool TrySpendGold(int amount)
        {
            if (!NetworkContext.HasServerAuthority())
            {
                return false;
            }

            int clampedAmount = Mathf.Max(0, amount);
            if (currentGold < clampedAmount)
            {
                return false;
            }

            currentGold -= clampedAmount;
            PublishStageSnapshot();
            return true;
        }

        public void AddExperience(int amount)
        {
            if (!NetworkContext.HasServerAuthority())
            {
                return;
            }

            currentExperience += Mathf.Max(0, amount);
            PublishStageSnapshot();
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
            UnregisterSnapshotHandler(NetworkManager.Singleton);
        }

        private void Update()
        {
            UpdateSnapshotMessaging();

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
            TickSnapshotPublishing();
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
            hasCurrentWave = false;
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
            hasCurrentWave = true;
            waveElapsedTime = 0f;
            bossSpawned = false;
            CurrentWaveState = WaveState.Spawning;
            enemySpawner?.BeginWave(currentWaveIndex, wave, castle);
            WaveStarted?.Invoke(currentWaveIndex, wave);
            PublishStageSnapshot();
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

        private void TickSnapshotPublishing()
        {
            snapshotSendTimer += Time.deltaTime;
            if (snapshotSendTimer < snapshotSendInterval)
            {
                return;
            }

            snapshotSendTimer = 0f;
            PublishStageSnapshot();
        }

        private void UpdateSnapshotMessaging()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager == null || !networkManager.IsListening || networkManager.CustomMessagingManager == null)
            {
                UnregisterSnapshotHandler(networkManager);
                return;
            }

            if (networkManager.IsServer)
            {
                UnregisterSnapshotHandler(networkManager);
                return;
            }

            if (registeredSnapshotHandler)
            {
                return;
            }

            networkManager.CustomMessagingManager.RegisterNamedMessageHandler(StageSnapshotMessageName, OnStageSnapshotMessage);
            registeredSnapshotHandler = true;
        }

        private void UnregisterSnapshotHandler(NetworkManager networkManager)
        {
            if (!registeredSnapshotHandler || networkManager == null || networkManager.CustomMessagingManager == null)
            {
                registeredSnapshotHandler = false;
                return;
            }

            networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(StageSnapshotMessageName);
            registeredSnapshotHandler = false;
        }

        private void PublishStageSnapshot()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (!NetworkContext.HasServerAuthority() || networkManager == null || !networkManager.IsListening || networkManager.CustomMessagingManager == null)
            {
                return;
            }

            foreach (ulong clientId in networkManager.ConnectedClientsIds)
            {
                if (clientId == NetworkManager.ServerClientId)
                {
                    continue;
                }

                using var writer = new FastBufferWriter(64, Allocator.Temp);
                writer.WriteValueSafe((int)CurrentStageState);
                writer.WriteValueSafe((int)CurrentWaveState);
                writer.WriteValueSafe(currentWaveIndex);
                writer.WriteValueSafe(hasCurrentWave);
                writer.WriteValueSafe(stageElapsedTime);
                writer.WriteValueSafe(waveElapsedTime);
                writer.WriteValueSafe(currentGold);
                writer.WriteValueSafe(currentExperience);
                networkManager.CustomMessagingManager.SendNamedMessage(StageSnapshotMessageName, clientId, writer);
            }
        }

        private void OnStageSnapshotMessage(ulong _, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int stageStateValue);
            reader.ReadValueSafe(out int waveStateValue);
            reader.ReadValueSafe(out int waveIndex);
            reader.ReadValueSafe(out bool snapshotHasCurrentWave);
            reader.ReadValueSafe(out float snapshotStageElapsedTime);
            reader.ReadValueSafe(out float snapshotWaveElapsedTime);
            reader.ReadValueSafe(out int snapshotGold);
            reader.ReadValueSafe(out int snapshotExperience);

            StageState nextStageState = Enum.IsDefined(typeof(StageState), stageStateValue) ? (StageState)stageStateValue : StageState.NotStarted;
            WaveState nextWaveState = Enum.IsDefined(typeof(WaveState), waveStateValue) ? (WaveState)waveStateValue : WaveState.Idle;
            bool stageStateChanged = CurrentStageState != nextStageState;

            CurrentStageState = nextStageState;
            CurrentWaveState = nextWaveState;
            currentWaveIndex = waveIndex;
            hasCurrentWave = snapshotHasCurrentWave;
            stageElapsedTime = Mathf.Max(0f, snapshotStageElapsedTime);
            waveElapsedTime = Mathf.Max(0f, snapshotWaveElapsedTime);
            currentGold = Mathf.Max(0, snapshotGold);
            currentExperience = Mathf.Max(0, snapshotExperience);

            if (hasCurrentWave && stageDefinition != null && stageDefinition.TryGetWave(currentWaveIndex, out WaveDefinition wave))
            {
                CurrentWave = wave;
            }
            else
            {
                CurrentWave = null;
            }

            if (stageStateChanged)
            {
                StageStateChanged?.Invoke(CurrentStageState);
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
            PublishStageSnapshot();
        }
    }
}
