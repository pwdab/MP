using System.Collections.Generic;
using MP.Gameplay.Entity;
using MP.Gameplay.Stages;
using MP.Gameplay.Combat;
using MP.Gameplay.Stats;
using MP.Network;
using MP.Progression.Level;
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
        private PlayerProgressionComponent localProgression;
        private PlayerActiveSkillComponent localActiveSkill;
        private RespawnComponent localRespawn;
        private Vector2 scrollPosition;

        private static readonly object CastleUpgradeSource = new();
        private static readonly object PlayerAttackUpgradeSource = new();

        private void OnGUI()
        {
            float width = Mathf.Clamp(Screen.width - 20f, 260f, 430f);
            float height = Mathf.Clamp(Screen.height - 20f, 220f, 420f);
            GUILayout.BeginArea(new Rect(10f, 10f, width, height), GUI.skin.box);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true);

            GUILayout.Label("Castle Defense Prototype");
            DrawNetworkState();
            DrawObjectiveState();
            DrawStageState();
            DrawCastleState();
            DrawBossState();
            DrawLocalPlayerState();
            DrawEnemyState();
            DrawResultState();

            GUILayout.Space(8f);
            GUILayout.Label("Ctrl+H Host | Ctrl+C Client | Ctrl+S Shutdown");
            GUILayout.Label("Ctrl+R Restart | R Revive");
            GUILayout.Label("1-6: Select Job");

            GUILayout.EndScrollView();
            GUILayout.EndArea();

            DrawDebugLegend();
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
                GUILayout.Label($"Spawn: {stageFlow.CurrentWaveState}");
                if (stageFlow.CurrentWave.BossWave)
                {
                    GUILayout.Label($"Bosses: {EnemySpawner.CountAliveBosses(stageFlow.CurrentWaveIndex)}");
                }
            }
            else
            {
                GUILayout.Label($"Wave: -/{stageFlow.WaveCount}");
                if (stageFlow.CurrentStageState == StageState.Rest)
                {
                    GUILayout.Label($"Next: Wave {stageFlow.CurrentWaveNumber + 1}/{stageFlow.WaveCount}");
                }
            }

            GUILayout.Label($"Gold: {stageFlow.CurrentGold}  Player EXP: {GetLocalPlayerExperienceText()}");

            if (stageFlow.CurrentStageState == StageState.Rest && NetworkContext.HasServerAuthority())
            {
                DrawRestPhaseControls();

                if (GUILayout.Button("Start Next Wave"))
                {
                    stageFlow.ContinueFromRest();
                }
            }
        }

        private void DrawObjectiveState()
        {
            stageFlow ??= FindFirstObjectByType<StageFlowController>();
            if (stageFlow == null)
            {
                return;
            }

            if (stageFlow.CurrentStageState == StageState.Rest)
            {
                GUILayout.Label("Objective: Upgrade, then start the next wave.");
                return;
            }

            if (stageFlow.CurrentWave != null && stageFlow.CurrentWave.BossWave)
            {
                GUILayout.Label("Objective: Defeat the boss before the castle falls.");
                return;
            }

            GUILayout.Label("Objective: Defend the castle.");
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

        private void DrawBossState()
        {
            WaveEnemyComponent[] waveEnemies = FindObjectsByType<WaveEnemyComponent>(FindObjectsSortMode.None);
            for (int i = 0; i < waveEnemies.Length; i++)
            {
                WaveEnemyComponent enemy = waveEnemies[i];
                if (enemy == null || !enemy.IsBoss || !enemy.TryGetComponent(out HealthComponent health) || health.IsDead)
                {
                    continue;
                }

                float currentHealth = health.CurrentHealth;
                if (enemy.TryGetComponent(out NetworkHealthState networkHealth))
                {
                    currentHealth = networkHealth.CurrentHealth;
                }

                GUILayout.Label($"Boss HP: {currentHealth:0}/{health.MaxHealth:0}");
                return;
            }
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
            if (localHealth != null && localHealth.IsDead)
            {
                string respawnText = localRespawn != null && localRespawn.IsWaitingForRespawn ? $"{localRespawn.RemainingRespawnTime:0.0}s" : "waiting";
                GUILayout.Label($"Player State: DEAD | Respawn: {respawnText}");
            }
            else if (localRespawn != null && localRespawn.IsWaitingForRespawn)
            {
                GUILayout.Label($"Respawn: {localRespawn.RemainingRespawnTime:0.0}s");
            }

            if (localActiveSkill != null)
            {
                string cooldownText = localActiveSkill.IsReady ? "Ready" : $"{localActiveSkill.RemainingCooldown:0.0}s";
                GUILayout.Label($"Space Skill: {cooldownText}");
            }

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
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"{i + 1}. {jobs[i].DisplayName}");
                    if (GUILayout.Button("Select", GUILayout.Width(64f)))
                    {
                        localJobSelector.SelectJob(i);
                    }
                    GUILayout.EndHorizontal();
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
                    localProgression = selector.GetComponent<PlayerProgressionComponent>();
                    localActiveSkill = selector.GetComponent<PlayerActiveSkillComponent>();
                    localRespawn = selector.GetComponent<RespawnComponent>();
                    return;
                }
            }
        }

        private string GetLocalPlayerExperienceText()
        {
            FindLocalPlayerComponents();
            return localProgression != null ? localProgression.Experience.ToString() : "-";
        }

        private void DrawRestPhaseControls()
        {
            GUILayout.Space(6f);
            GUILayout.Label("Rest Phase Upgrades");
            DrawUpgradeButton("5G Castle +50 Max HP", ApplyCastleUpgrade);
            DrawUpgradeButton("5G Players +2 Attack", ApplyPlayerAttackUpgrade);
            DrawUpgradeButton("5G Space Cooldown -1s", ApplySkillCooldownUpgrade);
        }

        private void DrawUpgradeButton(string label, System.Action apply)
        {
            bool canBuy = stageFlow != null && stageFlow.CurrentGold >= 5;
            GUI.enabled = canBuy;
            if (GUILayout.Button(label))
            {
                apply?.Invoke();
            }

            GUI.enabled = true;
        }

        private void ApplyCastleUpgrade()
        {
            if (stageFlow == null || castle == null || !stageFlow.TrySpendGold(5))
            {
                return;
            }

            if (castle.TryGetComponent(out StatsComponent stats))
            {
                stats.AddFlatModifier(StatId.MaxHealth, 50f, CastleUpgradeSource);
                castle.Health.RestoreToFullHealth();
            }
        }

        private void ApplyPlayerAttackUpgrade()
        {
            if (stageFlow == null || !stageFlow.TrySpendGold(5))
            {
                return;
            }

            PlayerEntity[] players = FindObjectsByType<PlayerEntity>(FindObjectsSortMode.None);
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i] != null && players[i].TryGetComponent(out StatsComponent stats))
                {
                    stats.AddFlatModifier(StatId.AttackPower, 2f, PlayerAttackUpgradeSource);
                }
            }
        }

        private void ApplySkillCooldownUpgrade()
        {
            if (stageFlow == null || !stageFlow.TrySpendGold(5))
            {
                return;
            }

            PlayerActiveSkillComponent[] skills = FindObjectsByType<PlayerActiveSkillComponent>(FindObjectsSortMode.None);
            for (int i = 0; i < skills.Length; i++)
            {
                if (skills[i] != null)
                {
                    skills[i].AddCooldownReductionServer(1f);
                    skills[i].ResetCooldownServer();
                }
            }
        }

        private void DrawResultState()
        {
            if (stageFlow == null)
            {
                return;
            }

            if (stageFlow.CurrentStageState != StageState.Cleared && stageFlow.CurrentStageState != StageState.Failed)
            {
                return;
            }

            GUILayout.Space(8f);
            GUILayout.Label(stageFlow.CurrentStageState == StageState.Cleared ? "RESULT: CLEAR" : "RESULT: FAILED");
            GUILayout.Label($"Reached Wave: {stageFlow.CurrentWaveNumber}/{stageFlow.WaveCount}");
            GUILayout.Label($"Play Time: {stageFlow.StageElapsedTime:0.0}s");
            GUILayout.Label($"Gold: {stageFlow.CurrentGold}");
            GUILayout.Label($"Player EXP: {GetLocalPlayerExperienceText()}");
            GUILayout.Label("Press Ctrl+R to restart.");
        }

        private void DrawDebugLegend()
        {
            const float width = 260f;
            GUILayout.BeginArea(new Rect(Screen.width - width - 10f, 10f, width, 170f), GUI.skin.box);
            GUILayout.Label("Debug Draw Legend");
            DrawLegendLine(new Color(1f, 0.85f, 0.25f, 1f), "Auto Attack Range");
            DrawLegendLine(new Color(0.1f, 0.55f, 1f, 1f), "AutoProjectileRange");
            DrawLegendLine(new Color(0.15f, 1f, 1f, 1f), "ManualProjectileRange");
            DrawLegendLine(new Color(1f, 0.25f, 0.85f, 1f), "Space Active Skill Range");
            DrawLegendLine(new Color(0.2f, 1f, 0.35f, 1f), "Move Direction");
            GUILayout.EndArea();
        }

        private static void DrawLegendLine(Color color, string label)
        {
            Color previousColor = GUI.color;
            GUILayout.BeginHorizontal();
            GUI.color = color;
            GUILayout.Label("###", GUILayout.Width(28f));
            GUI.color = previousColor;
            GUILayout.Label(label);
            GUILayout.EndHorizontal();
        }
    }
}
