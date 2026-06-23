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
        private const string ProjectileSpritePath = SpriteFolder + "/PrototypeProjectile.png";
        private const string CastleSpritePath = SpriteFolder + "/PrototypeCastle.png";
        private const string MapSpritePath = SpriteFolder + "/PrototypeMapTile.png";

        private const string PlayerStatsPath = "Assets/Project/Data/Players/PrototypePlayerBaseStats.asset";
        private const string EnemyStatsPath = "Assets/Project/Data/Enemies/PrototypeEnemyBaseStats.asset";
        private const string CastleStatsPath = "Assets/Project/Data/Stages/PrototypeCastleBaseStats.asset";
        private const string PlayerPrefabPath = "Assets/Project/Prefabs/Players/PrototypePlayer.prefab";
        private const string EnemyPrefabPath = "Assets/Project/Prefabs/Enemies/PrototypeEnemy.prefab";
        private const string ProjectilePrefabPath = "Assets/Project/Prefabs/Projectiles/PrototypeProjectile.prefab";
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
            EntityStatsDefinition castleStats = EnsureStatsDefinition(CastleStatsPath, CreateCastleBaseStats());
            JobDefinition[] jobs = EnsureJobDefinitions();

            GameObject projectilePrefab = EnsureProjectilePrefab();
            GameObject castlePrefab = EnsureCastlePrefab(castleStats);
            GameObject enemyPrefab = EnsureEnemyPrefab(enemyStats);
            GameObject playerPrefab = EnsurePlayerPrefab(playerStats, projectilePrefab, jobs);

            CreateCamera();
            CreateMap();
            CreateCastleSpawner(castlePrefab);
            CreateSpawnPointsAndSpawner(enemyPrefab);
            CreateNetworkManager(playerPrefab, castlePrefab, enemyPrefab, projectilePrefab);
            CreateSimulationRoot();
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
            EnsureSpriteTexture(ProjectileSpritePath, new Color32(255, 235, 90, 255));
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

        private static GameObject EnsureEnemyPrefab(EntityStatsDefinition enemyStats)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPrefabPath);
            if (prefab != null)
            {
                AssignEnemyPrefabReferences(prefab, enemyStats);
                return prefab;
            }

            var gameObject = new GameObject("PrototypeEnemy");
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
                gameObject.AddComponent<EnemyTargetingComponent>();
                gameObject.AddComponent<EnemyMoveToCastleComponent>();
                gameObject.AddComponent<EnemyCastleAttackComponent>();
                gameObject.AddComponent<EnemyDetectionRangeIndicator>();
                CombatRangeIndicator rangeIndicator = gameObject.AddComponent<CombatRangeIndicator>();
                gameObject.AddComponent<DespawnOnDeathComponent>();
                gameObject.AddComponent<ItemDropComponent>();
                EnsureSpriteRenderer(gameObject, EnemySpritePath, Color.red, 1, Vector3.one * 0.65f);
                EnsureBoxCollider(gameObject, Vector2.one);

                AssignBaseStats(stats, enemyStats);
                AssignTargetableTeam(targetable, TeamId.Enemy);
                AssignRangeIndicator(rangeIndicator, true, false);

                prefab = PrefabUtility.SaveAsPrefabAsset(gameObject, EnemyPrefabPath);
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
                gameObject.AddComponent<NetworkHealthState>();
                gameObject.AddComponent<WorldHealthLabel>();
                EnsureSpriteRenderer(gameObject, CastleSpritePath, Color.gray, 0, new Vector3(1.5f, 1.5f, 1f));
                EnsureBoxCollider(gameObject, Vector2.one);

                AssignBaseStats(stats, castleStats);
                AssignCastleTeam(castle, TeamId.Player);
                AssignTargetableTeam(targetable, TeamId.Player);

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
                gameObject.AddComponent<NetworkPlayerMovement>();
                NetworkProjectileLauncher launcher = gameObject.AddComponent<NetworkProjectileLauncher>();
                CombatComponent combat = gameObject.AddComponent<CombatComponent>();
                CombatRangeIndicator rangeIndicator = gameObject.AddComponent<CombatRangeIndicator>();
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
                gameObject.AddComponent<RespawnComponent>();
                EnsureSpriteRenderer(gameObject, PlayerSpritePath, Color.blue, 1, Vector3.one * 0.7f);
                EnsureBoxCollider(gameObject, Vector2.one);

                AssignBaseStats(stats, playerStats);
                AssignPlayerTeam(player, TeamId.Player);
                AssignTargetableTeam(targetable, TeamId.Player);
                AssignProjectileLauncher(launcher, TeamId.Player, projectilePrefab);
                AssignCombatTeam(combat, TeamId.Player);
                AssignJobSelector(jobSelector, jobs);
                AssignRangeIndicator(rangeIndicator, true, true);

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
            GetOrAdd<NetworkPlayerMovement>(root);
            NetworkProjectileLauncher launcher = GetOrAdd<NetworkProjectileLauncher>(root);
            CombatComponent combat = GetOrAdd<CombatComponent>(root);
            CombatRangeIndicator rangeIndicator = GetOrAdd<CombatRangeIndicator>(root);
            GetOrAdd<LocalCameraFollow>(root);
            GetOrAdd<NetworkPlayerLabel>(root);
            GetOrAdd<NetworkPlayerSpawnOffset>(root);
            GetOrAdd<PlayerJobComponent>(root);
            NetworkPlayerJobSelector jobSelector = GetOrAdd<NetworkPlayerJobSelector>(root);

            AssignBaseStats(stats, playerStats);
            AssignPlayerTeam(player, TeamId.Player);
            AssignTargetableTeam(targetable, TeamId.Player);
            AssignProjectileLauncher(launcher, TeamId.Player, projectilePrefab);
            AssignCombatTeam(combat, TeamId.Player);
            AssignJobSelector(jobSelector, jobs);
            AssignRangeIndicator(rangeIndicator, true, true);
            EnsureBoxCollider(root, Vector2.one);

            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void AssignEnemyPrefabReferences(GameObject prefab, EntityStatsDefinition enemyStats)
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
            GetOrAdd<EnemyTargetingComponent>(root);
            GetOrAdd<EnemyMoveToCastleComponent>(root);
            GetOrAdd<EnemyCastleAttackComponent>(root);
            GetOrAdd<EnemyDetectionRangeIndicator>(root);
            CombatRangeIndicator rangeIndicator = GetOrAdd<CombatRangeIndicator>(root);
            GetOrAdd<DespawnOnDeathComponent>(root);
            GetOrAdd<ItemDropComponent>(root);
            EnsureSpriteRenderer(root, EnemySpritePath, Color.red, 1, Vector3.one * 0.65f);
            EnsureBoxCollider(root, Vector2.one);

            AssignBaseStats(stats, enemyStats);
            AssignTargetableTeam(targetable, TeamId.Enemy);
            AssignRangeIndicator(rangeIndicator, true, false);

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
            GetOrAdd<NetworkHealthState>(root);
            GetOrAdd<WorldHealthLabel>(root);
            EnsureSpriteRenderer(root, CastleSpritePath, Color.gray, 0, new Vector3(1.5f, 1.5f, 1f));
            EnsureBoxCollider(root, Vector2.one);

            AssignBaseStats(stats, castleStats);
            AssignCastleTeam(castle, TeamId.Player);
            AssignTargetableTeam(targetable, TeamId.Player);

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

        private static void CreateNetworkManager(GameObject playerPrefab, GameObject castlePrefab, GameObject enemyPrefab, GameObject projectilePrefab)
        {
            var gameObject = new GameObject("NetworkManager");
            NetworkManager networkManager = gameObject.AddComponent<NetworkManager>();
            UnityTransport transport = gameObject.AddComponent<UnityTransport>();
            NetworkTestBootstrap bootstrap = gameObject.AddComponent<NetworkTestBootstrap>();

            networkManager.NetworkConfig.NetworkTransport = transport;
            networkManager.NetworkConfig.PlayerPrefab = playerPrefab;
            networkManager.NetworkConfig.ForceSamePrefabs = false;
            AssignNetworkPrefabs(bootstrap, castlePrefab, enemyPrefab, projectilePrefab);
        }

        private static void CreateSimulationRoot()
        {
            var gameObject = new GameObject("NetworkSimulationRoot");
            gameObject.AddComponent<SimulationAuthority>();
            gameObject.AddComponent<CombatSimulationRunner>();
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
                EnsureModifier(jobId, StatId.ProjectileRange, ConvertAttackRange(attackRange) - 5f)
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
                new StatEntry(StatId.ProjectileRange, 5f, new StatBounds(0f, 30f)),
                new StatEntry(StatId.MoveSpeed, 5f, new StatBounds(0f, 20f)),
                new StatEntry(StatId.RespawnDelay, 3f, new StatBounds(0f, 60f))
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
                new StatEntry(StatId.ProjectileRange, 0f, new StatBounds(0f, 30f)),
                new StatEntry(StatId.MoveSpeed, 1.6f, new StatBounds(0f, 20f)),
                new StatEntry(StatId.RespawnDelay, 3f, new StatBounds(0f, 60f))
            };
        }

        private static StatEntry[] CreateCastleBaseStats()
        {
            return new[]
            {
                new StatEntry(StatId.MaxHealth, 500f, new StatBounds(1f, 5000f)),
                new StatEntry(StatId.Defense, 120f, new StatBounds(0f, 1000f)),
                new StatEntry(StatId.AttackPower, 0f, new StatBounds(0f, 1000f)),
                new StatEntry(StatId.AttackSpeed, 0f, new StatBounds(0f, 20f)),
                new StatEntry(StatId.AutoAttackRange, 0f, new StatBounds(0f, 30f)),
                new StatEntry(StatId.ProjectileRange, 0f, new StatBounds(0f, 30f)),
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

        private static void AssignBaseStats(StatsComponent statsComponent, EntityStatsDefinition stats)
        {
            var serializedStats = new SerializedObject(statsComponent);
            serializedStats.FindProperty("baseStats").objectReferenceValue = stats;
            serializedStats.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignProjectileLauncher(NetworkProjectileLauncher launcher, TeamId team, GameObject projectilePrefab)
        {
            var serializedLauncher = new SerializedObject(launcher);
            serializedLauncher.FindProperty("team").enumValueIndex = (int)team;
            serializedLauncher.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab;
            serializedLauncher.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignCombatTeam(CombatComponent combat, TeamId team)
        {
            var serializedCombat = new SerializedObject(combat);
            serializedCombat.FindProperty("team").enumValueIndex = (int)team;
            serializedCombat.FindProperty("autoAttack").boolValue = true;
            serializedCombat.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignRangeIndicator(CombatRangeIndicator indicator, bool showAutoAttackRange, bool showProjectileRange)
        {
            var serializedIndicator = new SerializedObject(indicator);
            serializedIndicator.FindProperty("showAutoAttackRange").boolValue = showAutoAttackRange;
            serializedIndicator.FindProperty("showProjectileRange").boolValue = showProjectileRange;
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
            serializedSpawner.FindProperty("spawnOnStart").boolValue = true;

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

        private static float ConvertAutoAttackRange(float rating)
        {
            return ConvertAttackRange(rating) * 0.5f;
        }
    }
}
