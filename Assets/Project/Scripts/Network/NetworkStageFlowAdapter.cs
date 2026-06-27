using System;
using MP.Gameplay.Stages;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace MP.Network
{
    /*
        StageFlowController의 실행 권한과 네트워크 스냅샷 동기화를 담당하는 어댑터
        스테이지 규칙은 Gameplay의 StageFlowController가 담당
    */
    [RequireComponent(typeof(StageFlowController))]
    public sealed class NetworkStageFlowAdapter : MonoBehaviour
    {
        private const string StageSnapshotMessageName = "MP.StageFlow.Snapshot";

        [SerializeField, Min(0.02f)] private float snapshotSendInterval = 0.1f;

        private StageFlowController stageFlow;
        private bool registeredSnapshotHandler;
        private float snapshotSendTimer;

        private void Awake()
        {
            stageFlow = GetComponent<StageFlowController>();
            stageFlow.SetTickInUpdate(false);
            EnsureEnemySpawnAdapters();
        }

        private void OnDisable()
        {
            UnregisterSnapshotHandler(NetworkManager.Singleton);
        }

        private void Update()
        {
            UpdateSnapshotMessaging();

            if (!NetworkContext.HasServerAuthority())
            {
                return;
            }

            stageFlow.Tick(Time.deltaTime);
            TickSnapshotPublishing();
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

            StageSnapshot snapshot = stageFlow.CreateSnapshot();
            foreach (ulong clientId in networkManager.ConnectedClientsIds)
            {
                if (clientId == NetworkManager.ServerClientId)
                {
                    continue;
                }

                using var writer = new FastBufferWriter(64, Allocator.Temp);
                writer.WriteValueSafe((int)snapshot.StageState);
                writer.WriteValueSafe((int)snapshot.WaveState);
                writer.WriteValueSafe(snapshot.WaveIndex);
                writer.WriteValueSafe(snapshot.HasCurrentWave);
                writer.WriteValueSafe(snapshot.StageElapsedTime);
                writer.WriteValueSafe(snapshot.WaveElapsedTime);
                writer.WriteValueSafe(snapshot.Gold);
                writer.WriteValueSafe(snapshot.Experience);
                networkManager.CustomMessagingManager.SendNamedMessage(StageSnapshotMessageName, clientId, writer);
            }
        }

        private void OnStageSnapshotMessage(ulong _, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int stageStateValue);
            reader.ReadValueSafe(out int waveStateValue);
            reader.ReadValueSafe(out int waveIndex);
            reader.ReadValueSafe(out bool hasCurrentWave);
            reader.ReadValueSafe(out float stageElapsedTime);
            reader.ReadValueSafe(out float waveElapsedTime);
            reader.ReadValueSafe(out int gold);
            reader.ReadValueSafe(out int experience);

            StageState stageState = Enum.IsDefined(typeof(StageState), stageStateValue) ? (StageState)stageStateValue : StageState.NotStarted;
            WaveState waveState = Enum.IsDefined(typeof(WaveState), waveStateValue) ? (WaveState)waveStateValue : WaveState.Idle;
            stageFlow.ApplySnapshot(new StageSnapshot(stageState, waveState, waveIndex, hasCurrentWave, stageElapsedTime, waveElapsedTime, gold, experience));
        }

        private static void EnsureEnemySpawnAdapters()
        {
            EnemySpawner[] spawners = FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None);
            for (int i = 0; i < spawners.Length; i++)
            {
                if (!spawners[i].TryGetComponent(out NetworkEnemySpawnAdapter _))
                {
                    spawners[i].gameObject.AddComponent<NetworkEnemySpawnAdapter>();
                }
            }
        }
    }
}
