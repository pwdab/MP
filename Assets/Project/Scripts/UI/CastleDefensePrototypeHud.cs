using System.Collections.Generic;
using MP.Gameplay.Entity;
using MP.Gameplay.Stages;
using MP.Network;
using MP.Progression.Jobs;
using Unity.Netcode;
using UnityEngine;

namespace MP.UI
{
    public sealed class CastleDefensePrototypeHud : MonoBehaviour
    {
        private CastleEntity castle;
        private StageFlowController stageFlow;
        private NetworkPlayerJobSelector localJobSelector;
        private HealthComponent localHealth;
        private Vector2 scrollPosition;

        private void OnGUI()
        {
            float width = Mathf.Clamp(Screen.width - 20f, 260f, 430f);
            float height = Mathf.Clamp(Screen.height - 20f, 220f, 420f);
            GUILayout.BeginArea(new Rect(10f, 10f, width, height), GUI.skin.box);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true);

            GUILayout.Label("Castle Defense Prototype");
            DrawNetworkState();
            DrawStageState();
            DrawCastleState();
            DrawLocalPlayerState();
            DrawEnemyState();

            GUILayout.Space(8f);
            GUILayout.Label("Ctrl+H Host | Ctrl+C Client | Ctrl+S Shutdown");
            GUILayout.Label("Ctrl+R Restart | R Revive");
            GUILayout.Label("1-6: Select Job");

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawNetworkState()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            string mode = "Offline";
            if (networkManager != null && networkManager.IsListening)
            {
                mode = networkManager.IsHost ? "Host" : networkManager.IsClient ? "Client" : "Server";
            }

            GUILayout.Label($"Mode: {mode}");
        }

        private void DrawStageState()
        {
            stageFlow ??= FindFirstObjectByType<StageFlowController>();
            if (stageFlow == null)
            {
                GUILayout.Label("Stage: missing");
                return;
            }

            GUILayout.Label($"Stage: {stageFlow.CurrentStageState}");
            if (stageFlow.CurrentWave != null)
            {
                GUILayout.Label($"Wave: {stageFlow.CurrentWaveNumber}/{stageFlow.WaveCount} {stageFlow.CurrentWave.DisplayName}");
                GUILayout.Label($"Wave Time: {stageFlow.CurrentWaveRemainingTime:0.0}s");
                if (stageFlow.CurrentWave.BossWave)
                {
                    GUILayout.Label($"Bosses: {EnemySpawner.CountAliveBosses(stageFlow.CurrentWaveIndex)}");
                }
            }
            else
            {
                GUILayout.Label($"Wave: -/{stageFlow.WaveCount}");
            }

            GUILayout.Label($"Gold: {stageFlow.CurrentGold}  EXP: {stageFlow.CurrentExperience}");

            if (stageFlow.CurrentStageState == StageState.Rest && NetworkContext.HasServerAuthority())
            {
                if (GUILayout.Button("Start Next Wave"))
                {
                    stageFlow.ContinueFromRest();
                }
            }
        }

        private void DrawCastleState()
        {
            castle ??= FindFirstObjectByType<CastleEntity>();
            if (castle == null || castle.Health == null)
            {
                GUILayout.Label("Castle: missing");
                return;
            }

            float currentHealth = castle.Health.CurrentHealth;
            if (castle.TryGetComponent(out NetworkHealthState networkHealthState))
            {
                currentHealth = networkHealthState.CurrentHealth;
            }

            GUILayout.Label($"Castle HP: {currentHealth:0}/{castle.Health.MaxHealth:0}");
        }

        private void DrawLocalPlayerState()
        {
            FindLocalPlayerComponents();
            if (localJobSelector == null)
            {
                GUILayout.Label("Local Player: missing");
                return;
            }

            string jobName = localJobSelector.SelectedJob != null ? localJobSelector.SelectedJob.DisplayName : "None";
            string hpText = localHealth != null ? $"{localHealth.CurrentHealth:0}/{localHealth.MaxHealth:0}" : "missing";
            GUILayout.Label($"Player HP: {hpText}");
            GUILayout.Label($"Job: {jobName}");

            IReadOnlyList<JobDefinition> jobs = localJobSelector.AvailableJobs;
            if (jobs == null)
            {
                return;
            }

            for (int i = 0; i < jobs.Count; i++)
            {
                if (jobs[i] != null)
                {
                    GUILayout.Label($"{i + 1}. {jobs[i].DisplayName}");
                }
            }
        }

        private void DrawEnemyState()
        {
            EnemyEntity[] enemies = FindObjectsByType<EnemyEntity>(FindObjectsSortMode.None);
            int aliveCount = 0;
            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i] != null && enemies[i].Health != null && !enemies[i].Health.IsDead)
                {
                    aliveCount++;
                }
            }

            GUILayout.Label($"Enemies: {aliveCount}/{enemies.Length}");
        }

        private void FindLocalPlayerComponents()
        {
            if (localJobSelector != null)
            {
                return;
            }

            NetworkPlayerJobSelector[] selectors = FindObjectsByType<NetworkPlayerJobSelector>(FindObjectsSortMode.None);
            for (int i = 0; i < selectors.Length; i++)
            {
                NetworkPlayerJobSelector selector = selectors[i];
                if (selector != null && selector.IsOwner)
                {
                    localJobSelector = selector;
                    localHealth = selector.GetComponent<HealthComponent>();
                    return;
                }
            }
        }
    }
}
