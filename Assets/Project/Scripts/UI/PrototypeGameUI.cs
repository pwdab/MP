using System.Collections.Generic;
using MP.Gameplay.Combat;
using MP.Gameplay.Entity;
using MP.Gameplay.Stages;
using MP.Gameplay.Stats;
using MP.Network;
using MP.Progression.Jobs;
using MP.Progression.Level;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace MP.UI
{
    public sealed class PrototypeGameUI : MonoBehaviour
    {
        private const int UpgradeCost = 5;
        private const float PanelWidth = 360f;

        private static readonly object CastleUpgradeSource = new();
        private static readonly object PlayerAttackUpgradeSource = new();

        private Canvas canvas;
        private Text titleText;
        private Text statusText;
        private Text objectiveText;
        private Text castleText;
        private Text bossText;
        private Text playerText;
        private Text waveText;
        private Text enemyText;
        private Text resultText;
        private RectTransform rootContent;
        private RectTransform roomPanel;
        private RectTransform lobbyPanel;
        private RectTransform gamePanel;
        private RectTransform restPanel;
        private RectTransform resultPanel;
        private RectTransform jobList;
        private RectTransform footerPanel;

        private StageFlowController stageFlow;
        private CastleEntity castle;
        private NetworkTestBootstrap networkBootstrap;
        private NetworkPlayerJobSelector localJobSelector;
        private HealthComponent localHealth;
        private PlayerProgressionComponent localProgression;
        private PlayerActiveSkillComponent localActiveSkill;
        private RespawnComponent localRespawn;

        private void Awake()
        {
            EnsureEventSystem();
            BuildUi();
        }

        private void Update()
        {
            ResolveReferences();
            RefreshUi();
        }

        private void BuildUi()
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();

            rootContent = CreateScrollableRoot();
            AddVerticalLayout(rootContent, 6f, 8f);

            titleText = CreateText("Title", rootContent, "Castle Defense Prototype", 20, FontStyle.Bold);
            statusText = CreateText("Status", rootContent, string.Empty, 14, FontStyle.Normal);

            roomPanel = CreateSection(rootContent, "Room");
            CreateText("RoomInfo", roomPanel, "Create or join a room before selecting a character.", 13, FontStyle.Normal);
            CreateButton("Host Room", roomPanel, () => GetNetworkBootstrap()?.StartHost());
            CreateButton("Join Room", roomPanel, () => GetNetworkBootstrap()?.StartClient());

            lobbyPanel = CreateSection(rootContent, "Lobby");
            CreateText("LobbyInfo", lobbyPanel, "Choose a character, then start the game.", 13, FontStyle.Normal);
            jobList = CreatePanel("JobList", lobbyPanel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            AddVerticalLayout(jobList, 4f, 0f);
            EnsureLayoutElement(jobList.gameObject).preferredHeight = 0f;
            CreateButton("Start Game", lobbyPanel, StartGame);
            CreateButton("Leave Room", lobbyPanel, () => GetNetworkBootstrap()?.Shutdown());

            gamePanel = CreateSection(rootContent, "Game");
            objectiveText = CreateText("Objective", gamePanel, string.Empty, 14, FontStyle.Bold);
            waveText = CreateText("Wave", gamePanel, string.Empty, 13, FontStyle.Normal);
            castleText = CreateText("Castle", gamePanel, string.Empty, 13, FontStyle.Normal);
            bossText = CreateText("Boss", gamePanel, string.Empty, 13, FontStyle.Normal);
            playerText = CreateText("Player", gamePanel, string.Empty, 13, FontStyle.Normal);
            enemyText = CreateText("Enemies", gamePanel, string.Empty, 13, FontStyle.Normal);

            restPanel = CreateSection(rootContent, "Rest Phase");
            CreateButton("5G Castle +50 Max HP", restPanel, ApplyCastleUpgrade);
            CreateButton("5G Players +2 Attack", restPanel, ApplyPlayerAttackUpgrade);
            CreateButton("5G Space Cooldown -1s", restPanel, ApplySkillCooldownUpgrade);
            CreateButton("Start Next Wave", restPanel, () => stageFlow?.ContinueFromRest());

            resultPanel = CreateSection(rootContent, "Result");
            resultText = CreateText("ResultText", resultPanel, string.Empty, 14, FontStyle.Bold);
            CreateButton("Restart", resultPanel, () => GetNetworkBootstrap()?.RestartPrototype());

            footerPanel = CreateSection(rootContent, "Controls");
            CreateButton("Restart", footerPanel, () => GetNetworkBootstrap()?.RestartPrototype());
            CreateButton("Shutdown", footerPanel, () => GetNetworkBootstrap()?.Shutdown());
            CreateButton("Revive", footerPanel, () => GetNetworkBootstrap()?.RequestReviveLocalPlayer());
        }

        private void RefreshUi()
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            bool isListening = networkManager != null && networkManager.IsListening;
            bool isLobby = isListening && stageFlow != null && stageFlow.CurrentStageState == StageState.NotStarted;
            bool isRest = isListening && stageFlow != null && stageFlow.CurrentStageState == StageState.Rest;
            bool isResult = isListening && stageFlow != null && (stageFlow.CurrentStageState == StageState.Cleared || stageFlow.CurrentStageState == StageState.Failed);
            bool isPlaying = isListening && stageFlow != null && stageFlow.CurrentStageState == StageState.Playing;

            statusText.text = GetNetworkStatus(networkManager);
            roomPanel.gameObject.SetActive(!isListening);
            lobbyPanel.gameObject.SetActive(isLobby);
            gamePanel.gameObject.SetActive(isPlaying || isRest || isResult);
            restPanel.gameObject.SetActive(isRest && NetworkContext.HasServerAuthority());
            resultPanel.gameObject.SetActive(isResult);
            footerPanel.gameObject.SetActive(isListening);

            RefreshJobButtons();
            RefreshGameTexts();
            RefreshResultText();
        }

        private void RefreshGameTexts()
        {
            objectiveText.text = GetObjectiveText();
            waveText.text = GetWaveText();
            castleText.text = GetCastleText();
            bossText.text = GetBossText();
            bossText.gameObject.SetActive(!string.IsNullOrEmpty(bossText.text));
            playerText.text = GetPlayerText();
            enemyText.text = GetEnemyText();
        }

        private void RefreshResultText()
        {
            if (stageFlow == null || resultText == null)
            {
                return;
            }

            if (stageFlow.CurrentStageState != StageState.Cleared && stageFlow.CurrentStageState != StageState.Failed)
            {
                resultText.text = string.Empty;
                return;
            }

            string result = stageFlow.CurrentStageState == StageState.Cleared ? "CLEAR" : "FAILED";
            resultText.text = $"{result}\nReached Wave: {stageFlow.CurrentWaveNumber}/{stageFlow.WaveCount}\nTime: {stageFlow.StageElapsedTime:0.0}s\nGold: {stageFlow.CurrentGold}\nPlayer EXP: {GetLocalPlayerExperienceText()}";
        }

        private void RefreshJobButtons()
        {
            if (jobList == null || localJobSelector == null)
            {
                return;
            }

            int desiredCount = localJobSelector.AvailableJobs != null ? localJobSelector.AvailableJobs.Count : 0;
            while (jobList.childCount < desiredCount)
            {
                int index = jobList.childCount;
                CreateButton($"Job {index + 1}", jobList, () => localJobSelector.SelectJob(index));
            }

            for (int i = 0; i < jobList.childCount; i++)
            {
                Transform child = jobList.GetChild(i);
                bool active = i < desiredCount;
                child.gameObject.SetActive(active);
                if (!active || !child.TryGetComponent(out Button button))
                {
                    continue;
                }

                Text text = child.GetComponentInChildren<Text>();
                JobDefinition job = localJobSelector.AvailableJobs[i];
                string selected = localJobSelector.SelectedJobIndex == i ? " <" : string.Empty;
                if (text != null)
                {
                    text.text = $"{i + 1}. {job.DisplayName}{selected}";
                }

                button.interactable = StageSimulationGate.CanAcceptPlayerInput();
            }

            UpdateJobListLayout(desiredCount);
        }

        private void ResolveReferences()
        {
            stageFlow ??= FindFirstObjectByType<StageFlowController>();
            castle ??= FindFirstObjectByType<CastleEntity>();
            networkBootstrap ??= FindFirstObjectByType<NetworkTestBootstrap>();

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

        private void StartGame()
        {
            if (NetworkContext.HasServerAuthority())
            {
                stageFlow?.StartStage();
            }
        }

        private void ApplyCastleUpgrade()
        {
            if (stageFlow == null || castle == null || !stageFlow.TrySpendGold(UpgradeCost))
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
            if (stageFlow == null || !stageFlow.TrySpendGold(UpgradeCost))
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
            if (stageFlow == null || !stageFlow.TrySpendGold(UpgradeCost))
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

        private string GetNetworkStatus(NetworkManager networkManager)
        {
            if (networkManager == null)
            {
                return "NetworkManager: missing";
            }

            if (!networkManager.IsListening)
            {
                return "Not connected";
            }

            return networkManager.IsHost ? "Host" : networkManager.IsClient ? "Client" : "Server";
        }

        private string GetObjectiveText()
        {
            if (stageFlow == null)
            {
                return string.Empty;
            }

            if (stageFlow.CurrentStageState == StageState.Rest)
            {
                return "Upgrade, then start the next wave.";
            }

            if (stageFlow.CurrentWave != null && stageFlow.CurrentWave.BossWave)
            {
                return "Defeat the boss before the castle falls.";
            }

            return "Defend the castle.";
        }

        private string GetWaveText()
        {
            if (stageFlow == null)
            {
                return "Stage: missing";
            }

            if (stageFlow.CurrentWave == null)
            {
                return stageFlow.CurrentStageState == StageState.Rest ? $"Next Wave: {stageFlow.CurrentWaveNumber + 1}/{stageFlow.WaveCount}" : $"Wave: -/{stageFlow.WaveCount}";
            }

            return $"Wave {stageFlow.CurrentWaveNumber}/{stageFlow.WaveCount} {stageFlow.CurrentWave.DisplayName}\nTime: {stageFlow.CurrentWaveRemainingTime:0.0}s | Spawn: {stageFlow.CurrentWaveState}\nGold: {stageFlow.CurrentGold} | Player EXP: {GetLocalPlayerExperienceText()}";
        }

        private string GetCastleText()
        {
            if (castle == null || castle.Health == null)
            {
                return "Castle: missing";
            }

            float currentHealth = castle.Health.CurrentHealth;
            if (castle.TryGetComponent(out NetworkHealthState networkHealth))
            {
                currentHealth = networkHealth.CurrentHealth;
            }

            return $"Castle HP: {currentHealth:0}/{castle.Health.MaxHealth:0}";
        }

        private string GetBossText()
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

                return $"Boss HP: {currentHealth:0}/{health.MaxHealth:0}";
            }

            return string.Empty;
        }

        private string GetPlayerText()
        {
            if (localJobSelector == null)
            {
                return "Local Player: missing";
            }

            string jobName = localJobSelector.SelectedJob != null ? localJobSelector.SelectedJob.DisplayName : "None";
            string hpText = localHealth != null ? $"{localHealth.CurrentHealth:0}/{localHealth.MaxHealth:0}" : "missing";
            string skillText = localActiveSkill != null ? localActiveSkill.IsReady ? "Ready" : $"{localActiveSkill.RemainingCooldown:0.0}s" : "missing";
            string deathText = string.Empty;
            if (localHealth != null && localHealth.IsDead)
            {
                string respawnText = localRespawn != null && localRespawn.IsWaitingForRespawn ? $"{localRespawn.RemainingRespawnTime:0.0}s" : "waiting";
                deathText = $"\nDEAD | Respawn: {respawnText}";
            }

            return $"Player HP: {hpText}\nJob: {jobName}\nSpace Skill: {skillText}{deathText}";
        }

        private string GetEnemyText()
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

            return $"Enemies: {aliveCount}/{enemies.Length}";
        }

        private string GetLocalPlayerExperienceText()
        {
            return localProgression != null ? localProgression.Experience.ToString() : "-";
        }

        private NetworkTestBootstrap GetNetworkBootstrap()
        {
            networkBootstrap ??= FindFirstObjectByType<NetworkTestBootstrap>();
            return networkBootstrap;
        }

        private void UpdateJobListLayout(int desiredCount)
        {
            LayoutElement layoutElement = EnsureLayoutElement(jobList.gameObject);
            float buttonHeight = 30f;
            float spacing = desiredCount > 1 ? (desiredCount - 1) * 4f : 0f;
            layoutElement.preferredHeight = desiredCount > 0 ? desiredCount * buttonHeight + spacing : 0f;

            LayoutRebuilder.ForceRebuildLayoutImmediate(jobList);
            LayoutRebuilder.ForceRebuildLayoutImmediate(lobbyPanel);
            if (rootContent != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rootContent);
            }
        }

        private RectTransform CreateScrollableRoot()
        {
            RectTransform viewport = CreatePanel("Viewport", transform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(12f, -12f), new Vector2(PanelWidth, -24f));
            Image background = viewport.gameObject.AddComponent<Image>();
            background.color = new Color(0.04f, 0.06f, 0.09f, 0.72f);

            Mask mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;

            ScrollRect scrollRect = viewport.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 26f;
            scrollRect.viewport = viewport;

            RectTransform content = CreatePanel("Content", viewport, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            content.offsetMin = new Vector2(0f, content.offsetMin.y);
            content.offsetMax = new Vector2(0f, content.offsetMax.y);
            scrollRect.content = content;
            return content;
        }

        private static RectTransform CreateSection(Transform parent, string title)
        {
            RectTransform section = CreatePanel(title + "Section", parent, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            AddVerticalLayout(section, 4f, 6f);
            CreateText(title + "Title", section, title, 15, FontStyle.Bold);
            return section;
        }

        private static Button CreateButton(string label, Transform parent, UnityEngine.Events.UnityAction onClick)
        {
            RectTransform rect = CreatePanel(label + "Button", parent, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, 30f));
            LayoutElement layoutElement = EnsureLayoutElement(rect.gameObject);
            layoutElement.minHeight = 30f;
            layoutElement.preferredHeight = 30f;

            Image image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.16f, 0.2f, 0.28f, 0.95f);

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            Text text = CreateText("Label", rect, label, 13, FontStyle.Bold, false);
            text.alignment = TextAnchor.MiddleCenter;
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            return button;
        }

        private static Text CreateText(string name, Transform parent, string value, int fontSize, FontStyle fontStyle, bool fitHeight = true)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            Text text = gameObject.AddComponent<Text>();
            text.text = value;
            text.font = GetDefaultFont();
            text.fontSize = fontSize;
            text.fontStyle = fontStyle;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            RectTransform rect = text.rectTransform;
            rect.sizeDelta = new Vector2(0f, Mathf.Max(22f, fontSize + 8f));

            if (fitHeight)
            {
                LayoutElement layoutElement = EnsureLayoutElement(gameObject);
                layoutElement.minHeight = Mathf.Max(22f, fontSize + 8f);
                layoutElement.preferredHeight = Mathf.Max(22f, fontSize + 8f);

                ContentSizeFitter fitter = gameObject.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            return text;
        }

        private static Font GetDefaultFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
            {
                return font;
            }

            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static RectTransform CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = gameObject.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            return rect;
        }

        private static void AddVerticalLayout(RectTransform rect, float spacing, float padding)
        {
            VerticalLayoutGroup layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.padding = new RectOffset((int)padding, (int)padding, (int)padding, (int)padding);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = rect.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private static LayoutElement EnsureLayoutElement(GameObject gameObject)
        {
            if (!gameObject.TryGetComponent(out LayoutElement layoutElement))
            {
                layoutElement = gameObject.AddComponent<LayoutElement>();
            }

            return layoutElement;
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
#else
            eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
