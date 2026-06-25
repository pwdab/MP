using MP.Gameplay.Combat;
using MP.Gameplay.Entity;
using MP.Gameplay.Movement;
using MP.Gameplay.Stages;
using MP.Gameplay.Stats;
using MP.Items;
using MP.Network;
using MP.Progression.Jobs;
using MP.Progression.Level;
using MP.Progression.SkillTree;
using MP.UI;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MP.Editor
{
    public static class CastleDefensePrototypeSceneBuilder
    {
        private const string ScenePath = "Assets/Project/Scenes/Main/CastleDefensePrototype.unity";
        private const string SpriteFolder = "Assets/Project/Art/Sprites/Prototype";
        private const string PlayerSpritePath = SpriteFolder + "/PrototypePlayer.png";
        private const string EnemySpritePath = SpriteFolder + "/PrototypeEnemy.png";
        private const string BossSpritePath = SpriteFolder + "/PrototypeBoss.png";
        private const string ProjectileSpritePath = SpriteFolder + "/PrototypeProjectile.png";
        private const string GoldSpritePath = SpriteFolder + "/PrototypeGold.png";
        private const string CastleSpritePath = SpriteFolder + "/PrototypeCastle.png";
        private const string MapSpritePath = SpriteFolder + "/PrototypeMapTile.png";

        private const string PlayerStatsPath = "Assets/Project/Data/Players/PrototypePlayerBaseStats.asset";
        private const string EnemyStatsPath = "Assets/Project/Data/Enemies/PrototypeEnemyBaseStats.asset";
        private const string BossStatsPath = "Assets/Project/Data/Enemies/PrototypeBossBaseStats.asset";
        private const string CastleStatsPath = "Assets/Project/Data/Stages/PrototypeCastleBaseStats.asset";
        private const string StageDefinitionPath = "Assets/Project/Data/Stages/PrototypeStage.asset";
        private const string PlayerPrefabPath = "Assets/Project/Prefabs/Players/PrototypePlayer.prefab";
        private const string EnemyPrefabPath = "Assets/Project/Prefabs/Enemies/PrototypeEnemy.prefab";
        private const string BossPrefabPath = "Assets/Project/Prefabs/Enemies/PrototypeBoss.prefab";
        private const string ProjectilePrefabPath = "Assets/Project/Prefabs/Projectiles/PrototypeProjectile.prefab";
        private const string GoldPrefabPath = "Assets/Project/Prefabs/Items/PrototypeGold.prefab";
        private const string CastlePrefabPath = "Assets/Project/Prefabs/Tower/PrototypeCastle.prefab";

        [MenuItem("MP/Prototype/Create Castle Defense Prototype Scene")]
        public static void CreateScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "CastleDefensePrototype";
            RemovePrototypeRuntimeClones();

            EnsureFolders();
            EnsureSprites();

            EntityStatsDefinition playerStats = EnsureStatsDefinition(PlayerStatsPath, CreatePlayerBaseStats());
            EntityStatsDefinition enemyStats = EnsureStatsDefinition(EnemyStatsPath, CreateEnemyBaseStats());
            EntityStatsDefinition bossStats = EnsureStatsDefinition(BossStatsPath, CreateBossBaseStats());
            EntityStatsDefinition castleStats = EnsureStatsDefinition(CastleStatsPath, CreateCastleBaseStats());
            JobDefinition[] jobs = EnsureJobDefinitions();

            GameObject projectilePrefab = EnsureProjectilePrefab();
            GameObject goldPrefab = EnsureGoldPrefab();
            GameObject castlePrefab = EnsureCastlePrefab(castleStats);
            GameObject enemyPrefab = EnsureEnemyPrefab(EnemyPrefabPath, "PrototypeEnemy", enemyStats, EnemySpritePath, Color.red, Vector3.one * 0.65f, goldPrefab, 1);
            GameObject bossPrefab = EnsureEnemyPrefab(BossPrefabPath, "PrototypeBoss", bossStats, BossSpritePath, new Color(0.75f, 0.25f, 1f), Vector3.one * 1.1f, goldPrefab, 10);
            GameObject playerPrefab = EnsurePlayerPrefab(playerStats, projectilePrefab, jobs);
            StageDefinition stageDefinition = EnsureStageDefinition(enemyPrefab, bossPrefab);

            CreateCamera();
            CreateMap();
            CreateCastleSpawner(castlePrefab);
            CreateSpawnPointsAndSpawner(enemyPrefab);
            CreateNetworkManager(playerPrefab, castlePrefab, enemyPrefab, bossPrefab, projectilePrefab, goldPrefab);
            CreateSimulationRoot(stageDefinition);
            CreateHud();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Project/Scenes", "Main");
            EnsureFolder("Assets/Project/Art/Sprites", "Prototype");
            EnsureFolder("Assets/Project/Data", "Players");
            EnsureFolder("Assets/Project/Data", "Enemies");
            EnsureFolder("Assets/Project/Data", "Stages");
            EnsureFolder("Assets/Project/Data", "Jobs");
            EnsureFolder("Assets/Project/Data/Jobs", "Modifiers");
            EnsureFolder("Assets/Project/Prefabs", "Players");
            EnsureFolder("Assets/Project/Prefabs", "Enemies");
            EnsureFolder("Assets/Project/Prefabs", "Projectiles");
            EnsureFolder("Assets/Project/Prefabs", "Items");
            EnsureFolder("Assets/Project/Prefabs", "Tower");
        }

        private static void RemovePrototypeRuntimeClones()
        {
            GameObject[] objects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            for (int i = 0; i < objects.Length; i++)
            {
                GameObject gameObject = objects[i];
                if (gameObject != null && gameObject.name.Contains("(Clone)") && gameObject.name.StartsWith("Prototype"))
                {
                    Object.DestroyImmediate(gameObject);
                }
            }
        }

        private static void EnsureSprites()
        {
            EnsureSpriteTexture(PlayerSpritePath, new Color32(80, 150, 255, 255));
            EnsureSpriteTexture(EnemySpritePath, new Color32(255, 80, 80, 255));
            EnsureSpriteTexture(BossSpritePath, new Color32(190, 70, 255, 255));
            EnsureSpriteTexture(ProjectileSpritePath, new Color32(255, 235, 90, 255));
            EnsureSpriteTexture(GoldSpritePath, new Color32(255, 190, 40, 255));
            EnsureSpriteTexture(CastleSpritePath, new Color32(170, 170, 190, 255));
            EnsureSpriteTexture(MapSpritePath, new Color32(45, 55, 48, 255));
        }

        private static GameObject EnsureProjectilePrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePrefabPath);
            if (prefab != null)
            {
                return prefab;
            }

            var gameObject = new GameObject("PrototypeProjectile");
            try
            {
                gameObject.AddComponent<NetworkObject>();
                gameObject.AddComponent<NetworkTransform>();
                gameObject.AddComponent<NetworkProjectile>();
                EnsureSpriteRenderer(gameObject, ProjectileSpritePath, Color.yellow, 2, Vector3.one * 0.25f);

                prefab = PrefabUtility.SaveAsPrefabAsset(gameObject, ProjectilePrefabPath);
                return prefab;
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        private static GameObject EnsureGoldPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GoldPrefabPath);
            if (prefab != null)
            {
                AssignGoldPrefabReferences(prefab);
                return prefab;
            }

            var gameObject = new GameObject("PrototypeGold");
            try
            {
                gameObject.AddComponent<NetworkObject>();
                gameObject.AddComponent<NetworkTransform>();
                gameObject.AddComponent<GoldPickupComponent>();
                EnsureKinematicRigidbody2D(gameObject);
                EnsureSpriteRenderer(gameObject, GoldSpritePath, new Color(1f, 0.75f, 0.15f), 2, Vector3.one * 0.3f);
                EnsureTriggerBoxCollider(gameObject, Vector2.one);

                prefab = PrefabUtility.SaveAsPrefabAsset(gameObject, GoldPrefabPath);
                return prefab;
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        private static GameObject EnsureEnemyPrefab(string prefabPath, string prefabName, EntityStatsDefinition enemyStats, string spritePath, Color color, Vector3 scale, GameObject goldPrefab, int goldAmount)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                AssignEnemyPrefabReferences(prefab, enemyStats, spritePath, color, scale, goldPrefab, goldAmount);
                return prefab;
            }

            var gameObject = new GameObject(prefabName);
            try
            {
                gameObject.AddComponent<NetworkObject>();
                gameObject.AddComponent<NetworkTransform>();
                StatsComponent stats = gameObject.AddComponent<StatsComponent>();
                gameObject.AddComponent<HealthComponent>();
                gameObject.AddComponent<CharacterStateComponent>();
                gameObject.AddComponent<EnemyEntity>();
                TargetableComponent targetable = gameObject.AddComponent<TargetableComponent>();
                gameObject.AddComponent<NetworkHealthState>();
                gameObject.AddComponent<WorldHealthLabel>();
                gameObject.AddComponent<WorldCombatFeedbackComponent>();
                gameObject.AddComponent<EnemyTargetingComponent>();
                EnemyMoveToCastleComponent enemyMovement = gameObject.AddComponent<EnemyMoveToCastleComponent>();
                gameObject.AddComponent<EnemyCastleAttackComponent>();
                gameObject.AddComponent<EnemyDetectionRangeIndicator>();
                CombatRangeIndicator rangeIndicator = gameObject.AddComponent<CombatRangeIndicator>();
                gameObject.AddComponent<DespawnOnDeathComponent>();
                EnemyGoldDropComponent goldDrop = gameObject.AddComponent<EnemyGoldDropComponent>();
                EnemyExperienceRewardComponent experienceReward = gameObject.AddComponent<EnemyExperienceRewardComponent>();
                gameObject.AddComponent<ItemDropComponent>();
                EnsureSpriteRenderer(gameObject, spritePath, color, 1, scale);
                EnsureBoxCollider(gameObject, Vector2.one);

                AssignBaseStats(stats, enemyStats);
                AssignTargetableTeam(targetable, TeamId.Enemy);
                AssignEnemyMovement(enemyMovement);
                AssignRangeIndicator(rangeIndicator, true, false, false);
                AssignGoldDrop(goldDrop, goldPrefab, goldAmount);
                AssignExperienceReward(experienceReward, goldAmount);

                prefab = PrefabUtility.SaveAsPrefabAsset(gameObject, prefabPath);
                return prefab;
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        private static GameObject EnsureCastlePrefab(EntityStatsDefinition castleStats)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CastlePrefabPath);
            if (prefab != null)
            {
                AssignCastlePrefabReferences(prefab, castleStats);
                return prefab;
            }

            var gameObject = new GameObject("PrototypeCastle");
            try
            {
                gameObject.AddComponent<NetworkObject>();
                gameObject.AddComponent<NetworkTransform>();
                StatsComponent stats = gameObject.AddComponent<StatsComponent>();
                gameObject.AddComponent<HealthComponent>();
                CastleEntity castle = gameObject.AddComponent<CastleEntity>();
                TargetableComponent targetable = gameObject.AddComponent<TargetableComponent>();
                CombatComponent combat = gameObject.AddComponent<CombatComponent>();
                CombatRangeIndicator rangeIndicator = gameObject.AddComponent<CombatRangeIndicator>();
                gameObject.AddComponent<NetworkHealthState>();
                gameObject.AddComponent<WorldHealthLabel>();
                gameObject.AddComponent<WorldCombatFeedbackComponent>();
                EnsureSpriteRenderer(gameObject, CastleSpritePath, Color.gray, 0, new Vector3(1.5f, 1.5f, 1f));
                EnsureBoxCollider(gameObject, Vector2.one);

                AssignBaseStats(stats, castleStats);
                AssignCastleTeam(castle, TeamId.Player);
                AssignTargetableTeam(targetable, TeamId.Player);
                AssignCombatTeam(combat, TeamId.Player);
                AssignRangeIndicator(rangeIndicator, true, false);

                prefab = PrefabUtility.SaveAsPrefabAsset(gameObject, CastlePrefabPath);
                return prefab;
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        private static GameObject EnsurePlayerPrefab(EntityStatsDefinition playerStats, GameObject projectilePrefab, JobDefinition[] jobs)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (prefab != null)
            {
                AssignPlayerPrefabReferences(prefab, playerStats, projectilePrefab, jobs);
                return prefab;
            }

            var gameObject = new GameObject("PrototypePlayer");
            try
            {
                gameObject.AddComponent<NetworkObject>();
                gameObject.AddComponent<NetworkTransform>();
                StatsComponent stats = gameObject.AddComponent<StatsComponent>();
                gameObject.AddComponent<HealthComponent>();
                gameObject.AddComponent<CharacterStateComponent>();
                PlayerEntity player = gameObject.AddComponent<PlayerEntity>();
                TargetableComponent targetable = gameObject.AddComponent<TargetableComponent>();
                gameObject.AddComponent<NetworkHealthState>();
                gameObject.AddComponent<NetworkTestCommands>();
                gameObject.AddComponent<WorldHealthLabel>();
                gameObject.AddComponent<WorldCombatFeedbackComponent>();
                gameObject.AddComponent<NetworkPlayerMovement>();
                AutoProjectileAttackComponent projectileAttack = gameObject.AddComponent<AutoProjectileAttackComponent>();
                NetworkProjectileLauncher launcher = gameObject.AddComponent<NetworkProjectileLauncher>();
                PlayerDirectionalBasicAttackComponent basicAttack = gameObject.AddComponent<PlayerDirectionalBasicAttackComponent>();
                PlayerActiveSkillComponent activeSkill = gameObject.AddComponent<PlayerActiveSkillComponent>();
                gameObject.AddComponent<PlayerKnockbackComponent>();
                gameObject.AddComponent<PlayerEnemyContactDamageComponent>();
                gameObject.AddComponent<PlayerSeparationComponent>();
                CombatRangeIndicator rangeIndicator = gameObject.AddComponent<CombatRangeIndicator>();
                gameObject.AddComponent<PlayerMoveDirectionDebugIndicator>();
                gameObject.AddComponent<LocalCameraFollow>();
                gameObject.AddComponent<NetworkPlayerLabel>();
                gameObject.AddComponent<NetworkPlayerSpawnOffset>();
                gameObject.AddComponent<PlayerJobComponent>();
                NetworkPlayerJobSelector jobSelector = gameObject.AddComponent<NetworkPlayerJobSelector>();
                gameObject.AddComponent<SkillTreeComponent>();
                gameObject.AddComponent<PlayerProgressionComponent>();
                gameObject.AddComponent<InventoryComponent>();
                gameObject.AddComponent<EquipComponent>();
                gameObject.AddComponent<PlayerSaveComponent>();
                RespawnComponent respawn = gameObject.AddComponent<RespawnComponent>();
                EnsureSpriteRenderer(gameObject, PlayerSpritePath, Color.blue, 1, Vector3.one * 0.7f);
                EnsureBoxCollider(gameObject, Vector2.one);

                AssignBaseStats(stats, playerStats);
                AssignPlayerTeam(player, TeamId.Player);
                AssignTargetableTeam(targetable, TeamId.Player);
                AssignAutoProjectileAttack(projectileAttack, TeamId.Player, projectilePrefab, true);
                AssignProjectileLauncher(launcher, TeamId.Player, projectilePrefab);
                AssignPlayerBasicAttack(basicAttack, TeamId.Player);
                AssignJobSelector(jobSelector, jobs);
                AssignActiveSkill(activeSkill);
                AssignRangeIndicator(rangeIndicator, true, true, false);
                AssignPlayerRespawn(respawn);

                prefab = PrefabUtility.SaveAsPrefabAsset(gameObject, PlayerPrefabPath);
                return prefab;
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        private static void AssignPlayerPrefabReferences(GameObject prefab, EntityStatsDefinition playerStats, GameObject projectilePrefab, JobDefinition[] jobs)
        {
            string path = AssetDatabase.GetAssetPath(prefab);
            GameObject root = PrefabUtility.LoadPrefabContents(path);

            GetOrAdd<NetworkObject>(root);
            GetOrAdd<NetworkTransform>(root);
            StatsComponent stats = GetOrAdd<StatsComponent>(root);
            GetOrAdd<HealthComponent>(root);
            GetOrAdd<CharacterStateComponent>(root);
            PlayerEntity player = GetOrAdd<PlayerEntity>(root);
            TargetableComponent targetable = GetOrAdd<TargetableComponent>(root);
            GetOrAdd<NetworkHealthState>(root);
            GetOrAdd<NetworkTestCommands>(root);
            GetOrAdd<WorldHealthLabel>(root);
            GetOrAdd<WorldCombatFeedbackComponent>(root);
            GetOrAdd<NetworkPlayerMovement>(root);
            NetworkProjectileLauncher launcher = GetOrAdd<NetworkProjectileLauncher>(root);
            AutoProjectileAttackComponent projectileAttack = GetOrAdd<AutoProjectileAttackComponent>(root);
            RemoveIfExists<CombatComponent>(root);
            PlayerDirectionalBasicAttackComponent basicAttack = GetOrAdd<PlayerDirectionalBasicAttackComponent>(root);
            PlayerActiveSkillComponent activeSkill = GetOrAdd<PlayerActiveSkillComponent>(root);
            GetOrAdd<PlayerKnockbackComponent>(root);
            GetOrAdd<PlayerEnemyContactDamageComponent>(root);
            GetOrAdd<PlayerSeparationComponent>(root);
            CombatRangeIndicator rangeIndicator = GetOrAdd<CombatRangeIndicator>(root);
            GetOrAdd<PlayerMoveDirectionDebugIndicator>(root);
            GetOrAdd<LocalCameraFollow>(root);
            GetOrAdd<NetworkPlayerLabel>(root);
            GetOrAdd<NetworkPlayerSpawnOffset>(root);
            GetOrAdd<PlayerJobComponent>(root);
            NetworkPlayerJobSelector jobSelector = GetOrAdd<NetworkPlayerJobSelector>(root);
            RespawnComponent respawn = GetOrAdd<RespawnComponent>(root);

            AssignBaseStats(stats, playerStats);
            AssignPlayerTeam(player, TeamId.Player);
            AssignTargetableTeam(targetable, TeamId.Player);
            AssignAutoProjectileAttack(projectileAttack, TeamId.Player, projectilePrefab, true);
            AssignProjectileLauncher(launcher, TeamId.Player, projectilePrefab);
            AssignPlayerBasicAttack(basicAttack, TeamId.Player);
            AssignJobSelector(jobSelector, jobs);
            AssignActiveSkill(activeSkill);
            AssignRangeIndicator(rangeIndicator, true, true, false);
            AssignPlayerRespawn(respawn);
            EnsureBoxCollider(root, Vector2.one);

            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void AssignEnemyPrefabReferences(GameObject prefab, EntityStatsDefinition enemyStats, string spritePath, Color color, Vector3 scale, GameObject goldPrefab, int goldAmount)
        {
            string path = AssetDatabase.GetAssetPath(prefab);
            GameObject root = PrefabUtility.LoadPrefabContents(path);

            GetOrAdd<NetworkObject>(root);
            GetOrAdd<NetworkTransform>(root);
            StatsComponent stats = GetOrAdd<StatsComponent>(root);
            GetOrAdd<HealthComponent>(root);
            GetOrAdd<CharacterStateComponent>(root);
            GetOrAdd<EnemyEntity>(root);
            TargetableComponent targetable = GetOrAdd<TargetableComponent>(root);
            GetOrAdd<NetworkHealthState>(root);
            GetOrAdd<WorldHealthLabel>(root);
            GetOrAdd<WorldCombatFeedbackComponent>(root);
            GetOrAdd<EnemyTargetingComponent>(root);
            EnemyMoveToCastleComponent enemyMovement = GetOrAdd<EnemyMoveToCastleComponent>(root);
            GetOrAdd<EnemyCastleAttackComponent>(root);
            GetOrAdd<EnemyDetectionRangeIndicator>(root);
            CombatRangeIndicator rangeIndicator = GetOrAdd<CombatRangeIndicator>(root);
            GetOrAdd<DespawnOnDeathComponent>(root);
            EnemyGoldDropComponent goldDrop = GetOrAdd<EnemyGoldDropComponent>(root);
            EnemyExperienceRewardComponent experienceReward = GetOrAdd<EnemyExperienceRewardComponent>(root);
            GetOrAdd<ItemDropComponent>(root);
            EnsureSpriteRenderer(root, spritePath, color, 1, scale);
            EnsureBoxCollider(root, Vector2.one);

            AssignBaseStats(stats, enemyStats);
            AssignTargetableTeam(targetable, TeamId.Enemy);
            AssignEnemyMovement(enemyMovement);
            AssignRangeIndicator(rangeIndicator, true, false, false);
            AssignGoldDrop(goldDrop, goldPrefab, goldAmount);
            AssignExperienceReward(experienceReward, goldAmount);

            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void AssignCastlePrefabReferences(GameObject prefab, EntityStatsDefinition castleStats)
        {
            string path = AssetDatabase.GetAssetPath(prefab);
            GameObject root = PrefabUtility.LoadPrefabContents(path);

            GetOrAdd<NetworkObject>(root);
            GetOrAdd<NetworkTransform>(root);
            StatsComponent stats = GetOrAdd<StatsComponent>(root);
            GetOrAdd<HealthComponent>(root);
            CastleEntity castle = GetOrAdd<CastleEntity>(root);
            TargetableComponent targetable = GetOrAdd<TargetableComponent>(root);
            CombatComponent combat = GetOrAdd<CombatComponent>(root);
            CombatRangeIndicator rangeIndicator = GetOrAdd<CombatRangeIndicator>(root);
            GetOrAdd<NetworkHealthState>(root);
            GetOrAdd<WorldHealthLabel>(root);
            GetOrAdd<WorldCombatFeedbackComponent>(root);
            EnsureSpriteRenderer(root, CastleSpritePath, Color.gray, 0, new Vector3(1.5f, 1.5f, 1f));
            EnsureBoxCollider(root, Vector2.one);

            AssignBaseStats(stats, castleStats);
            AssignCastleTeam(castle, TeamId.Player);
            AssignTargetableTeam(targetable, TeamId.Player);
            AssignCombatTeam(combat, TeamId.Player);
            AssignRangeIndicator(rangeIndicator, true, false, false);

            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void AssignGoldPrefabReferences(GameObject prefab)
        {
            string path = AssetDatabase.GetAssetPath(prefab);
            GameObject root = PrefabUtility.LoadPrefabContents(path);

            GetOrAdd<NetworkObject>(root);
            GetOrAdd<NetworkTransform>(root);
            GetOrAdd<GoldPickupComponent>(root);
            EnsureKinematicRigidbody2D(root);
            EnsureSpriteRenderer(root, GoldSpritePath, new Color(1f, 0.75f, 0.15f), 2, Vector3.one * 0.3f);
            EnsureTriggerBoxCollider(root, Vector2.one);

            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void CreateCamera()
        {
            var gameObject = new GameObject("Main Camera");
            gameObject.tag = "MainCamera";
            gameObject.transform.position = new Vector3(0f, 0f, -10f);

            Camera camera = gameObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 9f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.06f, 0.07f, 0.08f);
        }

        private static void CreateMap()
        {
            CreateSpriteObject("MapFloor", Vector3.zero, new Vector3(28f, 16f, 1f), MapSpritePath, new Color(0.18f, 0.23f, 0.18f), -10);
            CreateSpriteObject("NorthBoundary", new Vector3(0f, 8f, 0f), new Vector3(28f, 0.25f, 1f), MapSpritePath, new Color(0.12f, 0.13f, 0.12f), -9);
            CreateSpriteObject("SouthBoundary", new Vector3(0f, -8f, 0f), new Vector3(28f, 0.25f, 1f), MapSpritePath, new Color(0.12f, 0.13f, 0.12f), -9);
            CreateSpriteObject("WestBoundary", new Vector3(-14f, 0f, 0f), new Vector3(0.25f, 16f, 1f), MapSpritePath, new Color(0.12f, 0.13f, 0.12f), -9);
            CreateSpriteObject("EastBoundary", new Vector3(14f, 0f, 0f), new Vector3(0.25f, 16f, 1f), MapSpritePath, new Color(0.12f, 0.13f, 0.12f), -9);
        }

        private static void CreateCastleSpawner(GameObject castlePrefab)
        {
            var gameObject = new GameObject("CastleSpawner");
            CastleSpawner spawner = gameObject.AddComponent<CastleSpawner>();
            AssignCastleSpawner(spawner, castlePrefab, Vector3.zero);
        }

        private static void CreateSpawnPointsAndSpawner(GameObject enemyPrefab)
        {
            Vector3[] positions =
            {
                new(-12f, 6f, 0f),
                new(12f, 6f, 0f),
                new(-12f, -6f, 0f),
                new(12f, -6f, 0f)
            };

            EnemySpawnPoint[] spawnPoints = new EnemySpawnPoint[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                var point = new GameObject($"EnemySpawnPoint_{i + 1}");
                point.transform.position = positions[i];
                spawnPoints[i] = point.AddComponent<EnemySpawnPoint>();
                AssignSpawnIndex(spawnPoints[i], i);
            }

            var spawnerObject = new GameObject("EnemySpawner");
            EnemySpawner spawner = spawnerObject.AddComponent<EnemySpawner>();
            AssignSpawner(spawner, enemyPrefab, spawnPoints);
        }

        private static void CreateNetworkManager(GameObject playerPrefab, GameObject castlePrefab, GameObject enemyPrefab, GameObject bossPrefab, GameObject projectilePrefab, GameObject goldPrefab)
        {
            var gameObject = new GameObject("NetworkManager");
            NetworkManager networkManager = gameObject.AddComponent<NetworkManager>();
            UnityTransport transport = gameObject.AddComponent<UnityTransport>();
            NetworkTestBootstrap bootstrap = gameObject.AddComponent<NetworkTestBootstrap>();

            networkManager.NetworkConfig.NetworkTransport = transport;
            networkManager.NetworkConfig.PlayerPrefab = playerPrefab;
            networkManager.NetworkConfig.ForceSamePrefabs = false;
            AssignNetworkPrefabs(bootstrap, castlePrefab, enemyPrefab, bossPrefab, projectilePrefab, goldPrefab);
        }

        private static void CreateSimulationRoot(StageDefinition stageDefinition)
        {
            var gameObject = new GameObject("NetworkSimulationRoot");
            StageFlowController stageFlow = gameObject.AddComponent<StageFlowController>();
            gameObject.AddComponent<SimulationAuthority>();
            gameObject.AddComponent<CombatSimulationRunner>();
            AssignStageFlow(stageFlow, stageDefinition);
        }

        private static void CreateHud()
        {
            var gameObject = new GameObject("CastleDefensePrototypeHud");
            gameObject.AddComponent<CastleDefensePrototypeHud>();
        }

        private static JobDefinition[] EnsureJobDefinitions()
        {
            return new[]
            {
                EnsureJob("knight_a", "\uae30\uc0ac A - \uc911\uc7a5 \uae30\uc0ac", JobCategory.Vanguard, 100f, 100f, 100f, 100f, 100f, 100f),
                EnsureJob("knight_b", "\uae30\uc0ac B - \uc18c\ud658 \uae30\uc0ac", JobCategory.Vanguard, 135f, 140f, 80f, 90f, 85f, 110f),
                EnsureJob("mage_a", "\ub9c8\ubc95\uc0ac A - \ud654\uc5fc \ub9c8\ubc95\uc0ac", JobCategory.Mystic, 70f, 60f, 90f, 130f, 80f, 130f),
                EnsureJob("mage_b", "\ub9c8\ubc95\uc0ac B - \uc81c\uc5b4 \ub9c8\ubc95\uc0ac", JobCategory.Mystic, 75f, 65f, 95f, 85f, 115f, 135f),
                EnsureJob("assassin_a", "\uc554\uc0b4\uc790 A - \uae30\ub3d9 \uc554\uc0b4\uc790", JobCategory.Striker, 70f, 50f, 135f, 90f, 130f, 80f),
                EnsureJob("assassin_b", "\uc554\uc0b4\uc790 B - \ucc98\ud615 \uc554\uc0b4\uc790", JobCategory.Striker, 75f, 55f, 115f, 140f, 105f, 75f)
            };
        }

        private static JobDefinition EnsureJob(string jobId, string displayName, JobCategory category, float maxHealth, float defense, float moveSpeed, float attackPower, float attackSpeed, float attackRange)
        {
            string path = $"Assets/Project/Data/Jobs/{jobId}.asset";
            JobDefinition job = AssetDatabase.LoadAssetAtPath<JobDefinition>(path);
            if (job == null)
            {
                job = ScriptableObject.CreateInstance<JobDefinition>();
                AssetDatabase.CreateAsset(job, path);
            }

            StatModifierDefinition[] modifiers =
            {
                EnsureModifier(jobId, StatId.MaxHealth, ConvertMaxHealth(maxHealth) - 100f),
                EnsureModifier(jobId, StatId.Defense, defense - 100f),
                EnsureModifier(jobId, StatId.MoveSpeed, ConvertMoveSpeed(moveSpeed) - 5f),
                EnsureModifier(jobId, StatId.AttackPower, ConvertAttackPower(attackPower) - 10f),
                EnsureModifier(jobId, StatId.AttackSpeed, ConvertAttackSpeed(attackSpeed) - 1f),
                EnsureModifier(jobId, StatId.AutoAttackRange, ConvertAutoAttackRange(attackRange) - 2.5f),
                EnsureModifier(jobId, StatId.AutoProjectileRange, ConvertAttackRange(attackRange) - 5f),
                EnsureModifier(jobId, StatId.ManualProjectileRange, ConvertManualProjectileRange(attackRange) - 7.5f)
            };

            var serializedJob = new SerializedObject(job);
            serializedJob.FindProperty("jobId").stringValue = jobId;
            serializedJob.FindProperty("displayName").stringValue = displayName;
            serializedJob.FindProperty("category").enumValueIndex = (int)category;
            SerializedProperty modifiersProperty = serializedJob.FindProperty("statModifiers");
            modifiersProperty.arraySize = modifiers.Length;
            for (int i = 0; i < modifiers.Length; i++)
            {
                modifiersProperty.GetArrayElementAtIndex(i).objectReferenceValue = modifiers[i];
            }

            serializedJob.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(job);
            return job;
        }

        private static StatModifierDefinition EnsureModifier(string jobId, StatId statId, float value)
        {
            string path = $"Assets/Project/Data/Jobs/Modifiers/{jobId}_{statId}.asset";
            StatModifierDefinition modifier = AssetDatabase.LoadAssetAtPath<StatModifierDefinition>(path);
            if (modifier == null)
            {
                modifier = ScriptableObject.CreateInstance<StatModifierDefinition>();
                AssetDatabase.CreateAsset(modifier, path);
            }

            var serializedModifier = new SerializedObject(modifier);
            serializedModifier.FindProperty("statId").enumValueIndex = (int)statId;
            serializedModifier.FindProperty("type").enumValueIndex = (int)StatModifierType.Flat;
            serializedModifier.FindProperty("value").floatValue = value;
            serializedModifier.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(modifier);
            return modifier;
        }

        private static StatEntry[] CreatePlayerBaseStats()
        {
            return new[]
            {
                new StatEntry(StatId.MaxHealth, 100f, new StatBounds(1f, 1000f)),
                new StatEntry(StatId.Defense, 100f, new StatBounds(0f, 1000f)),
                new StatEntry(StatId.AttackPower, 10f, new StatBounds(0f, 1000f)),
                new StatEntry(StatId.AttackSpeed, 1f, new StatBounds(0f, 20f)),
                new StatEntry(StatId.AutoAttackRange, 2.5f, new StatBounds(0f, 30f)),
                new StatEntry(StatId.AutoProjectileRange, 5f, new StatBounds(0f, 30f)),
                new StatEntry(StatId.ManualProjectileRange, 7.5f, new StatBounds(0f, 45f)),
                new StatEntry(StatId.MoveSpeed, 5f, new StatBounds(0f, 20f)),
                new StatEntry(StatId.RespawnDelay, 10f, new StatBounds(0f, 60f))
            };
        }

        private static StatEntry[] CreateEnemyBaseStats()
        {
            return new[]
            {
                new StatEntry(StatId.MaxHealth, 60f, new StatBounds(1f, 1000f)),
                new StatEntry(StatId.Defense, 80f, new StatBounds(0f, 1000f)),
                new StatEntry(StatId.AttackPower, 8f, new StatBounds(0f, 1000f)),
                new StatEntry(StatId.AttackSpeed, 0.7f, new StatBounds(0f, 20f)),
                new StatEntry(StatId.AutoAttackRange, 1.2f, new StatBounds(0f, 30f)),
                new StatEntry(StatId.AutoProjectileRange, 0f, new StatBounds(0f, 30f)),
                new StatEntry(StatId.ManualProjectileRange, 0f, new StatBounds(0f, 45f)),
                new StatEntry(StatId.MoveSpeed, 1.6f, new StatBounds(0f, 20f)),
                new StatEntry(StatId.RespawnDelay, 3f, new StatBounds(0f, 60f))
            };
        }

        private static StatEntry[] CreateBossBaseStats()
        {
            return new[]
            {
                new StatEntry(StatId.MaxHealth, 220f, new StatBounds(1f, 3000f)),
                new StatEntry(StatId.Defense, 120f, new StatBounds(0f, 1000f)),
                new StatEntry(StatId.AttackPower, 16f, new StatBounds(0f, 1000f)),
                new StatEntry(StatId.AttackSpeed, 0.55f, new StatBounds(0f, 20f)),
                new StatEntry(StatId.AutoAttackRange, 1.6f, new StatBounds(0f, 30f)),
                new StatEntry(StatId.AutoProjectileRange, 0f, new StatBounds(0f, 30f)),
                new StatEntry(StatId.ManualProjectileRange, 0f, new StatBounds(0f, 45f)),
                new StatEntry(StatId.MoveSpeed, 1.1f, new StatBounds(0f, 20f)),
                new StatEntry(StatId.RespawnDelay, 3f, new StatBounds(0f, 60f))
            };
        }

        private static StatEntry[] CreateCastleBaseStats()
        {
            return new[]
            {
                new StatEntry(StatId.MaxHealth, 500f, new StatBounds(1f, 5000f)),
                new StatEntry(StatId.Defense, 120f, new StatBounds(0f, 1000f)),
                new StatEntry(StatId.AttackPower, 12f, new StatBounds(0f, 1000f)),
                new StatEntry(StatId.AttackSpeed, 0.8f, new StatBounds(0f, 20f)),
                new StatEntry(StatId.AutoAttackRange, 4f, new StatBounds(0f, 30f)),
                new StatEntry(StatId.AutoProjectileRange, 0f, new StatBounds(0f, 30f)),
                new StatEntry(StatId.ManualProjectileRange, 0f, new StatBounds(0f, 45f)),
                new StatEntry(StatId.MoveSpeed, 0f, new StatBounds(0f, 20f)),
                new StatEntry(StatId.RespawnDelay, 0f, new StatBounds(0f, 60f))
            };
        }

        private static EntityStatsDefinition EnsureStatsDefinition(string path, StatEntry[] stats)
        {
            EntityStatsDefinition definition = AssetDatabase.LoadAssetAtPath<EntityStatsDefinition>(path);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<EntityStatsDefinition>();
                AssetDatabase.CreateAsset(definition, path);
            }

            var serializedDefinition = new SerializedObject(definition);
            SerializedProperty statsProperty = serializedDefinition.FindProperty("stats");
            statsProperty.arraySize = stats.Length;
            for (int i = 0; i < stats.Length; i++)
            {
                SerializedProperty entry = statsProperty.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("statId").enumValueIndex = (int)stats[i].StatId;
                entry.FindPropertyRelative("baseValue").floatValue = stats[i].BaseValue;
                entry.FindPropertyRelative("bounds").FindPropertyRelative("minimum").floatValue = stats[i].Bounds.Minimum;
                entry.FindPropertyRelative("bounds").FindPropertyRelative("maximum").floatValue = stats[i].Bounds.Maximum;
            }

            serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static StageDefinition EnsureStageDefinition(GameObject enemyPrefab, GameObject bossPrefab)
        {
            StageDefinition definition = AssetDatabase.LoadAssetAtPath<StageDefinition>(StageDefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<StageDefinition>();
                AssetDatabase.CreateAsset(definition, StageDefinitionPath);
            }

            var serializedDefinition = new SerializedObject(definition);
            serializedDefinition.FindProperty("stageId").stringValue = "prototype_stage";
            serializedDefinition.FindProperty("displayName").stringValue = "Prototype Stage";
            serializedDefinition.FindProperty("startingGold").intValue = 0;
            serializedDefinition.FindProperty("startingExperience").intValue = 0;

            SerializedProperty waves = serializedDefinition.FindProperty("waves");
            waves.arraySize = 10;
            AssignWave(waves.GetArrayElementAtIndex(0), "Wave 1", 18f, 12f, 2.6f, 8, false, null, 0f, enemyPrefab);
            AssignWave(waves.GetArrayElementAtIndex(1), "Wave 2", 18f, 13f, 2.3f, 10, false, null, 0f, enemyPrefab);
            AssignWave(waves.GetArrayElementAtIndex(2), "Wave 3", 18f, 14f, 2.0f, 12, false, null, 0f, enemyPrefab);
            AssignWave(waves.GetArrayElementAtIndex(3), "Wave 4", 18f, 15f, 1.8f, 14, false, null, 0f, enemyPrefab);
            AssignWave(waves.GetArrayElementAtIndex(4), "Mid Boss Wave", 18f, 14f, 1.7f, 16, true, bossPrefab, 6f, enemyPrefab);
            AssignWave(waves.GetArrayElementAtIndex(5), "Wave 6", 18f, 15f, 1.6f, 16, false, null, 0f, enemyPrefab);
            AssignWave(waves.GetArrayElementAtIndex(6), "Wave 7", 18f, 15f, 1.45f, 18, false, null, 0f, enemyPrefab);
            AssignWave(waves.GetArrayElementAtIndex(7), "Wave 8", 18f, 16f, 1.3f, 20, false, null, 0f, enemyPrefab);
            AssignWave(waves.GetArrayElementAtIndex(8), "Wave 9", 18f, 16f, 1.15f, 22, false, null, 0f, enemyPrefab);
            AssignWave(waves.GetArrayElementAtIndex(9), "Final Boss Wave", 18f, 16f, 1.0f, 24, true, bossPrefab, 5f, enemyPrefab);

            serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static void AssignWave(
            SerializedProperty wave,
            string displayName,
            float waveDuration,
            float spawnDuration,
            float spawnInterval,
            int maxAliveEnemies,
            bool bossWave,
            GameObject bossPrefab,
            float bossSpawnTime,
            GameObject enemyPrefab)
        {
            wave.FindPropertyRelative("displayName").stringValue = displayName;
            wave.FindPropertyRelative("waveDuration").floatValue = waveDuration;
            wave.FindPropertyRelative("spawnDuration").floatValue = spawnDuration;
            wave.FindPropertyRelative("spawnInterval").floatValue = spawnInterval;
            wave.FindPropertyRelative("maxAliveEnemies").intValue = maxAliveEnemies;
            wave.FindPropertyRelative("bossWave").boolValue = bossWave;
            wave.FindPropertyRelative("bossPrefab").objectReferenceValue = bossPrefab;
            wave.FindPropertyRelative("bossSpawnTime").floatValue = bossSpawnTime;

            SerializedProperty spawnEntries = wave.FindPropertyRelative("spawnEntries");
            spawnEntries.arraySize = 1;
            SerializedProperty entry = spawnEntries.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("enemyPrefab").objectReferenceValue = enemyPrefab;
            entry.FindPropertyRelative("weight").floatValue = 1f;
        }

        private static void AssignBaseStats(StatsComponent statsComponent, EntityStatsDefinition stats)
        {
            var serializedStats = new SerializedObject(statsComponent);
            serializedStats.FindProperty("baseStats").objectReferenceValue = stats;
            serializedStats.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignStageFlow(StageFlowController stageFlow, StageDefinition stageDefinition)
        {
            var serializedStageFlow = new SerializedObject(stageFlow);
            serializedStageFlow.FindProperty("stageDefinition").objectReferenceValue = stageDefinition;
            serializedStageFlow.FindProperty("autoStart").boolValue = true;
            serializedStageFlow.FindProperty("playerStartRadius").floatValue = 3f;
            serializedStageFlow.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignCombatTeam(CombatComponent combat, TeamId team)
        {
            var serializedCombat = new SerializedObject(combat);
            serializedCombat.FindProperty("team").enumValueIndex = (int)team;
            serializedCombat.FindProperty("autoAttack").boolValue = true;
            serializedCombat.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignAutoProjectileAttack(AutoProjectileAttackComponent projectileAttack, TeamId team, GameObject projectilePrefab, bool playerStyle)
        {
            var serializedAttack = new SerializedObject(projectileAttack);
            serializedAttack.FindProperty("team").enumValueIndex = (int)team;
            serializedAttack.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab;
            serializedAttack.FindProperty("autoFire").boolValue = true;
            serializedAttack.FindProperty("useAutoAttackRange").boolValue = false;
            serializedAttack.FindProperty("autoAttackRangeMultiplier").floatValue = 1f;
            serializedAttack.FindProperty("fireInMoveDirectionWhenNoTarget").boolValue = playerStyle;
            serializedAttack.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignActiveSkill(PlayerActiveSkillComponent activeSkill)
        {
            var serializedSkill = new SerializedObject(activeSkill);
            serializedSkill.FindProperty("radius").floatValue = 1.875f;
            serializedSkill.FindProperty("debugEffectDuration").floatValue = 1.5f;
            serializedSkill.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignProjectileLauncher(NetworkProjectileLauncher launcher, TeamId team, GameObject projectilePrefab)
        {
            var serializedLauncher = new SerializedObject(launcher);
            serializedLauncher.FindProperty("team").enumValueIndex = (int)team;
            serializedLauncher.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab;
            serializedLauncher.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignPlayerBasicAttack(PlayerDirectionalBasicAttackComponent basicAttack, TeamId team)
        {
            var serializedAttack = new SerializedObject(basicAttack);
            serializedAttack.FindProperty("team").enumValueIndex = (int)team;
            serializedAttack.FindProperty("autoAttack").boolValue = true;
            serializedAttack.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignGoldDrop(EnemyGoldDropComponent goldDrop, GameObject goldPrefab, int goldAmount)
        {
            var serializedGoldDrop = new SerializedObject(goldDrop);
            serializedGoldDrop.FindProperty("goldAmount").intValue = Mathf.Max(0, goldAmount);
            serializedGoldDrop.FindProperty("goldPrefab").objectReferenceValue = goldPrefab;
            serializedGoldDrop.FindProperty("scatterRadius").floatValue = 0.35f;
            serializedGoldDrop.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignEnemyMovement(EnemyMoveToCastleComponent movement)
        {
            var serializedMovement = new SerializedObject(movement);
            serializedMovement.FindProperty("contactDistance").floatValue = 0f;
            serializedMovement.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignExperienceReward(EnemyExperienceRewardComponent reward, int experienceAmount)
        {
            var serializedReward = new SerializedObject(reward);
            serializedReward.FindProperty("experienceAmount").intValue = Mathf.Max(0, experienceAmount);
            serializedReward.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignRangeIndicator(CombatRangeIndicator indicator, bool showAutoAttackRange, bool showProjectileRanges, bool showActiveSkillRange = false)
        {
            var serializedIndicator = new SerializedObject(indicator);
            serializedIndicator.FindProperty("showAutoAttackRange").boolValue = showAutoAttackRange;
            serializedIndicator.FindProperty("showAutoProjectileRange").boolValue = showProjectileRanges;
            serializedIndicator.FindProperty("showManualProjectileRange").boolValue = showProjectileRanges;
            serializedIndicator.FindProperty("showActiveSkillRange").boolValue = showActiveSkillRange;
            serializedIndicator.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignPlayerTeam(PlayerEntity player, TeamId team)
        {
            var serializedPlayer = new SerializedObject(player);
            serializedPlayer.FindProperty("team").enumValueIndex = (int)team;
            serializedPlayer.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignCastleTeam(CastleEntity castle, TeamId team)
        {
            var serializedCastle = new SerializedObject(castle);
            serializedCastle.FindProperty("team").enumValueIndex = (int)team;
            serializedCastle.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignTargetableTeam(TargetableComponent targetable, TeamId team)
        {
            var serializedTargetable = new SerializedObject(targetable);
            serializedTargetable.FindProperty("team").enumValueIndex = (int)team;
            serializedTargetable.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignJobSelector(NetworkPlayerJobSelector selector, JobDefinition[] jobs)
        {
            var serializedSelector = new SerializedObject(selector);
            SerializedProperty jobsProperty = serializedSelector.FindProperty("availableJobs");
            jobsProperty.arraySize = jobs.Length;
            for (int i = 0; i < jobs.Length; i++)
            {
                jobsProperty.GetArrayElementAtIndex(i).objectReferenceValue = jobs[i];
            }

            serializedSelector.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignPlayerRespawn(RespawnComponent respawn)
        {
            var serializedRespawn = new SerializedObject(respawn);
            serializedRespawn.FindProperty("autoRespawnOnDeath").boolValue = true;
            serializedRespawn.FindProperty("respawnNearCastle").boolValue = true;
            serializedRespawn.FindProperty("castleRespawnRadius").floatValue = 3f;
            serializedRespawn.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignNetworkPrefabs(NetworkTestBootstrap bootstrap, params GameObject[] prefabs)
        {
            var serializedBootstrap = new SerializedObject(bootstrap);
            SerializedProperty prefabsProperty = serializedBootstrap.FindProperty("networkPrefabs");
            prefabsProperty.arraySize = prefabs.Length;
            for (int i = 0; i < prefabs.Length; i++)
            {
                prefabsProperty.GetArrayElementAtIndex(i).objectReferenceValue = prefabs[i];
            }

            serializedBootstrap.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignSpawnIndex(EnemySpawnPoint spawnPoint, int index)
        {
            var serializedSpawnPoint = new SerializedObject(spawnPoint);
            serializedSpawnPoint.FindProperty("spawnIndex").intValue = index;
            serializedSpawnPoint.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignCastleSpawner(CastleSpawner spawner, GameObject castlePrefab, Vector3 spawnPosition)
        {
            var serializedSpawner = new SerializedObject(spawner);
            serializedSpawner.FindProperty("castlePrefab").objectReferenceValue = castlePrefab;
            serializedSpawner.FindProperty("spawnPosition").vector3Value = spawnPosition;
            serializedSpawner.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignSpawner(EnemySpawner spawner, GameObject enemyPrefab, EnemySpawnPoint[] spawnPoints)
        {
            var serializedSpawner = new SerializedObject(spawner);
            serializedSpawner.FindProperty("enemyPrefab").objectReferenceValue = enemyPrefab;
            serializedSpawner.FindProperty("targetCastle").objectReferenceValue = null;
            serializedSpawner.FindProperty("spawnInterval").floatValue = 2.5f;
            serializedSpawner.FindProperty("maxAliveEnemies").intValue = 16;
            serializedSpawner.FindProperty("spawnOnStart").boolValue = false;

            SerializedProperty spawnPointsProperty = serializedSpawner.FindProperty("spawnPoints");
            spawnPointsProperty.arraySize = spawnPoints.Length;
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                spawnPointsProperty.GetArrayElementAtIndex(i).objectReferenceValue = spawnPoints[i];
            }

            serializedSpawner.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject CreateSpriteObject(string name, Vector3 position, Vector3 scale, string spritePath, Color color, int sortingOrder)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.position = position;
            EnsureSpriteRenderer(gameObject, spritePath, color, sortingOrder, scale);
            return gameObject;
        }

        private static void EnsureSpriteRenderer(GameObject gameObject, string spritePath, Color color, int sortingOrder, Vector3 scale)
        {
            SpriteRenderer renderer = gameObject.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = gameObject.AddComponent<SpriteRenderer>();
            }

            if (renderer == null)
            {
                throw new MissingComponentException($"{gameObject.name} could not create a SpriteRenderer.");
            }

            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            gameObject.transform.localScale = scale;
        }

        private static void EnsureBoxCollider(GameObject gameObject, Vector2 size)
        {
            BoxCollider2D collider = gameObject.GetComponent<BoxCollider2D>();
            if (collider == null)
            {
                collider = gameObject.AddComponent<BoxCollider2D>();
            }

            collider.isTrigger = false;
            collider.size = size;
        }

        private static void EnsureTriggerBoxCollider(GameObject gameObject, Vector2 size)
        {
            BoxCollider2D collider = gameObject.GetComponent<BoxCollider2D>();
            if (collider == null)
            {
                collider = gameObject.AddComponent<BoxCollider2D>();
            }

            collider.isTrigger = true;
            collider.size = size;
        }

        private static void EnsureKinematicRigidbody2D(GameObject gameObject)
        {
            Rigidbody2D body = gameObject.GetComponent<Rigidbody2D>();
            if (body == null)
            {
                body = gameObject.AddComponent<Rigidbody2D>();
            }

            body.bodyType = RigidbodyType2D.Kinematic;
            body.simulated = true;
            body.gravityScale = 0f;
        }

        private static void EnsureSpriteTexture(string path, Color32 color)
        {
            if (!AssetDatabase.LoadAssetAtPath<Texture2D>(path))
            {
                var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
                Color32[] pixels = new Color32[32 * 32];
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = color;
                }

                texture.SetPixels32(pixels);
                texture.Apply();
                System.IO.File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);
            }

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 32f;
            importer.SaveAndReimport();
        }

        private static void EnsureFolder(string parent, string folderName)
        {
            string path = parent + "/" + folderName;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }

        private static T GetOrAdd<T>(GameObject gameObject) where T : Component
        {
            return gameObject.TryGetComponent(out T component) ? component : gameObject.AddComponent<T>();
        }

        private static void RemoveIfExists<T>(GameObject gameObject) where T : Component
        {
            if (gameObject.TryGetComponent(out T component))
            {
                Object.DestroyImmediate(component);
            }
        }

        private static float ConvertMaxHealth(float rating)
        {
            return rating;
        }

        private static float ConvertMoveSpeed(float rating)
        {
            return rating / 20f;
        }

        private static float ConvertAttackPower(float rating)
        {
            return rating / 10f;
        }

        private static float ConvertAttackSpeed(float rating)
        {
            return rating / 100f;
        }

        private static float ConvertAttackRange(float rating)
        {
            return rating / 20f;
        }

        private static float ConvertManualProjectileRange(float rating)
        {
            return ConvertAttackRange(rating) * 1.5f;
        }

        private static float ConvertAutoAttackRange(float rating)
        {
            return ConvertAttackRange(rating) * 0.5f;
        }
    }
}
