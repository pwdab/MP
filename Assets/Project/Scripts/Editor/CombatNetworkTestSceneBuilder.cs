using MP.Gameplay.Combat;
using MP.Gameplay.Entity;
using MP.Gameplay.Events;
using MP.Gameplay.Movement;
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
    public static class CombatNetworkTestSceneBuilder
    {
        private const string ScenePath = "Assets/Project/Scenes/Test/CombatNetworkTest.unity";
        private const string TestSpriteFolder = "Assets/Project/Art/Sprites/Test";
        private const string PlayerSpritePath = TestSpriteFolder + "/TestPlayerSquare.png";
        private const string EnemySpritePath = TestSpriteFolder + "/TestEnemySquare.png";
        private const string ProjectileSpritePath = TestSpriteFolder + "/TestProjectileSquare.png";
        private const string EnemyKilledEventChannelPath = "Assets/Project/Data/Events/EnemyKilledEventChannel.asset";
        private const string TestPlayerStatsPath = "Assets/Project/Data/Players/TestPlayerStats.asset";
        private const string TestEnemyStatsPath = "Assets/Project/Data/Enemies/TestEnemyStats.asset";
        private const string TestEnemyDropTablePath = "Assets/Project/Data/RewardTables/TestEnemyDropTable.asset";
        private const string PlayerPrefabPath = "Assets/Project/Prefabs/Players/TestPlayer.prefab";
        private const string ProjectilePrefabPath = "Assets/Project/Prefabs/Projectiles/TestProjectile.prefab";

        [MenuItem("MP/Test Scenes/Create Combat Network Test Scene")]
        public static void CreateScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "CombatNetworkTest";

            EnsureTestSprites();
            CreateMainCamera();
            GameObject projectilePrefab = EnsureProjectilePrefab();
            GameObject playerPrefab = EnsurePlayerPrefab(projectilePrefab);

            NetworkManager networkManager = CreateNetworkManager(playerPrefab, projectilePrefab);
            CreateSimulationRoot();

            RemoveItemSystemTestRunner();
            CreateTestEnemy(EnsureEnemyKilledEventChannel(), projectilePrefab);
            CreateCombatRunner();
            CreateHud();

            EditorSceneManager.SaveScene(scene, ScenePath);
            Selection.activeObject = networkManager.gameObject;
        }

        [MenuItem("MP/Test Scenes/Add Visuals To Open Combat Network Test Scene")]
        public static void AddVisualsToOpenScene()
        {
            EnsureTestSprites();
            GameObject projectilePrefab = EnsureProjectilePrefab();
            GameObject playerPrefab = EnsurePlayerPrefab(projectilePrefab);
            CreateMainCamera();

            RemoveNetworkObjectFromNetworkManager();
            RemoveSceneTestPlayer();
            RemoveItemSystemTestRunner();

            NetworkTestBootstrap bootstrap = Object.FindFirstObjectByType<NetworkTestBootstrap>();
            if (bootstrap != null)
            {
                AssignNetworkPrefabs(bootstrap, projectilePrefab);
            }

            NetworkManager networkManager = Object.FindFirstObjectByType<NetworkManager>();
            if (networkManager != null)
            {
                networkManager.NetworkConfig.PlayerPrefab = playerPrefab;
                networkManager.NetworkConfig.ForceSamePrefabs = false;
            }

            EnsureSimulationRootCommands();

            EnemyKilledEventChannel enemyKilledEventChannel = EnsureEnemyKilledEventChannel();
            EntityStatsDefinition enemyStats = EnsureTestEnemyStats();
            GameObject enemy = GameObject.Find("TestEnemy");
            if (enemy != null)
            {
                EnsureSpriteRenderer(enemy, EnemySpritePath, Color.red);
                AssignEnemyKilledEventChannel(enemy, enemyKilledEventChannel);
                AssignEnemyDropTable(enemy, EnsureTestEnemyDropTable());
                EnsureRespawnComponent(enemy);
                EnsureCharacterStateComponent(enemy);
                AssignBaseStats(enemy, enemyStats);
                EnsureEnemyCombatComponents(enemy, projectilePrefab);
            }

            CreateHud();
            Scene activeScene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
        }

        private static NetworkManager CreateNetworkManager(GameObject playerPrefab, GameObject projectilePrefab)
        {
            var gameObject = new GameObject("NetworkManager");
            NetworkManager networkManager = gameObject.AddComponent<NetworkManager>();
            UnityTransport transport = gameObject.AddComponent<UnityTransport>();
            NetworkTestBootstrap bootstrap = gameObject.AddComponent<NetworkTestBootstrap>();

            networkManager.NetworkConfig.NetworkTransport = transport;
            networkManager.NetworkConfig.PlayerPrefab = playerPrefab;
            networkManager.NetworkConfig.ForceSamePrefabs = false;
            AssignNetworkPrefabs(bootstrap, projectilePrefab);

            return networkManager;
        }

        private static void CreateSimulationRoot()
        {
            var gameObject = new GameObject("NetworkSimulationRoot");
            gameObject.AddComponent<NetworkObject>();
            gameObject.AddComponent<SimulationAuthority>();
            gameObject.AddComponent<NetworkCombatAuthority>();
            gameObject.AddComponent<NetworkTestCommands>();
        }

        private static void CreateTestEnemy(EnemyKilledEventChannel enemyKilledEventChannel, GameObject projectilePrefab)
        {
            var gameObject = new GameObject("TestEnemy");
            gameObject.transform.position = new Vector3(2f, 0f, 0f);
            gameObject.transform.localScale = Vector3.one;

            gameObject.AddComponent<NetworkObject>();
            gameObject.AddComponent<NetworkTransform>();
            StatsComponent stats = gameObject.AddComponent<StatsComponent>();
            gameObject.AddComponent<HealthComponent>();
            gameObject.AddComponent<CharacterStateComponent>();
            EnemyEntity enemy = gameObject.AddComponent<EnemyEntity>();
            gameObject.AddComponent<RespawnComponent>();
            ItemDropComponent itemDrop = gameObject.AddComponent<ItemDropComponent>();
            TargetableComponent targetable = gameObject.AddComponent<TargetableComponent>();
            gameObject.AddComponent<NetworkHealthState>();
            EnsureSpriteRenderer(gameObject, EnemySpritePath, Color.red);
            AssignBaseStats(stats, EnsureTestEnemyStats());
            AssignTargetableTeam(targetable, TeamId.Enemy);
            EnsureEnemyCombatComponents(gameObject, projectilePrefab);
            AssignEnemyKilledEventChannel(enemy, enemyKilledEventChannel);
            AssignEnemyDropTable(itemDrop, EnsureTestEnemyDropTable());
        }

        private static void CreateCombatRunner()
        {
            var gameObject = GameObject.Find("NetworkSimulationRoot");
            CombatSimulationRunner runner = gameObject.AddComponent<CombatSimulationRunner>();

            var serializedRunner = new SerializedObject(runner);
            serializedRunner.FindProperty("combatants").arraySize = 0;
            serializedRunner.FindProperty("tickInUpdate").boolValue = false;
            serializedRunner.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateMainCamera()
        {
            if (Camera.main != null)
            {
                return;
            }

            var gameObject = new GameObject("Main Camera");
            gameObject.tag = "MainCamera";
            gameObject.transform.position = new Vector3(0f, 0f, -10f);

            Camera camera = gameObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 4f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.08f, 0.1f);
        }

        private static void CreateHud()
        {
            if (Object.FindFirstObjectByType<CombatNetworkTestHud>() != null)
            {
                return;
            }

            var gameObject = new GameObject("CombatNetworkTestHud");
            gameObject.AddComponent<CombatNetworkTestHud>();
        }

        private static void EnsureSpriteRenderer(GameObject gameObject, string spritePath, Color fallbackColor)
        {
            SpriteRenderer renderer = gameObject.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = gameObject.AddComponent<SpriteRenderer>();
            }

            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            renderer.color = renderer.sprite != null ? Color.white : fallbackColor;
            renderer.sortingOrder = 0;
        }

        private static void EnsureTestSprites()
        {
            EnsureFolder("Assets/Project/Art/Sprites", "Test");
            EnsureSpriteTexture(PlayerSpritePath, new Color32(80, 150, 255, 255));
            EnsureSpriteTexture(EnemySpritePath, new Color32(255, 85, 85, 255));
            EnsureSpriteTexture(ProjectileSpritePath, new Color32(255, 235, 90, 255));
        }

        private static void EnsurePlayerGameplayComponents(GameObject player, GameObject projectilePrefab)
        {
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(player);

            LineRenderer legacyLineRenderer = player.GetComponent<LineRenderer>();
            if (legacyLineRenderer != null)
            {
                Object.DestroyImmediate(legacyLineRenderer);
            }

            if (!player.TryGetComponent(out NetworkTransform _))
            {
                player.AddComponent<NetworkTransform>();
            }

            if (!player.TryGetComponent(out NetworkPlayerSpawnOffset _))
            {
                player.AddComponent<NetworkPlayerSpawnOffset>();
            }

            if (!player.TryGetComponent(out NetworkPlayerLabel _))
            {
                player.AddComponent<NetworkPlayerLabel>();
            }

            StatsComponent stats = player.GetComponent<StatsComponent>();
            if (stats == null)
            {
                stats = player.AddComponent<StatsComponent>();
            }

            AssignBaseStats(stats, EnsureTestPlayerStats());

            if (!player.TryGetComponent(out HealthComponent _))
            {
                player.AddComponent<HealthComponent>();
            }

            if (!player.TryGetComponent(out CharacterStateComponent _))
            {
                player.AddComponent<CharacterStateComponent>();
            }

            if (!player.TryGetComponent(out RespawnComponent respawn))
            {
                respawn = player.AddComponent<RespawnComponent>();
            }

            AssignRespawnSettings(respawn, false);

            EnsurePlayerProgressionComponents(player);

            TargetableComponent targetable = player.GetComponent<TargetableComponent>();
            if (targetable == null)
            {
                targetable = player.AddComponent<TargetableComponent>();
            }

            AssignTargetableTeam(targetable, TeamId.Player);

            if (!player.TryGetComponent(out NetworkHealthState _))
            {
                player.AddComponent<NetworkHealthState>();
            }

            if (!player.TryGetComponent(out NetworkPlayerMovement _))
            {
                player.AddComponent<NetworkPlayerMovement>();
            }

            if (!player.TryGetComponent(out LocalCameraFollow _))
            {
                player.AddComponent<LocalCameraFollow>();
            }

            CombatComponent combat = player.GetComponent<CombatComponent>();
            if (combat != null)
            {
                var serializedCombat = new SerializedObject(combat);
                serializedCombat.FindProperty("autoAttack").boolValue = true;
                serializedCombat.ApplyModifiedPropertiesWithoutUndo();
            }

            if (!player.TryGetComponent(out CombatRangeIndicator _))
            {
                player.AddComponent<CombatRangeIndicator>();
            }

            NetworkProjectileLauncher launcher = player.GetComponent<NetworkProjectileLauncher>();
            if (launcher == null)
            {
                launcher = player.AddComponent<NetworkProjectileLauncher>();
            }

            var serializedLauncher = new SerializedObject(launcher);
            serializedLauncher.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab;
            serializedLauncher.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject EnsurePlayerPrefab(GameObject projectilePrefab)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (prefab == null)
            {
                var player = new GameObject("TestPlayer");
                player.transform.localScale = Vector3.one;
                player.AddComponent<NetworkObject>();
                player.AddComponent<NetworkTransform>();
                player.AddComponent<StatsComponent>();
                player.AddComponent<HealthComponent>();
                player.AddComponent<CharacterStateComponent>();
                RespawnComponent respawn = player.AddComponent<RespawnComponent>();
                AssignRespawnSettings(respawn, false);
                player.AddComponent<PlayerEntity>();
                EnsurePlayerProgressionComponents(player);
                player.AddComponent<TargetableComponent>();
                player.AddComponent<NetworkHealthState>();
                player.AddComponent<CombatComponent>();
                EnsureSpriteRenderer(player, PlayerSpritePath, Color.blue);
                EnsurePlayerGameplayComponents(player, projectilePrefab);

                prefab = PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);
                Object.DestroyImmediate(player);
            }
            else
            {
                GameObject prefabContents = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
                EnsurePlayerGameplayComponents(prefabContents, projectilePrefab);
                EnsureSpriteRenderer(prefabContents, PlayerSpritePath, Color.blue);
                PrefabUtility.SaveAsPrefabAsset(prefabContents, PlayerPrefabPath);
                PrefabUtility.UnloadPrefabContents(prefabContents);
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            }

            return prefab;
        }

        private static GameObject EnsureProjectilePrefab()
        {
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePrefabPath);
            if (existingPrefab != null)
            {
                return existingPrefab;
            }

            var projectile = new GameObject("TestProjectile");
            projectile.transform.localScale = new Vector3(0.35f, 0.35f, 1f);
            projectile.AddComponent<NetworkObject>();
            projectile.AddComponent<NetworkTransform>();
            projectile.AddComponent<NetworkProjectile>();
            EnsureSpriteRenderer(projectile, ProjectileSpritePath, Color.yellow);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(projectile, ProjectilePrefabPath);
            Object.DestroyImmediate(projectile);

            return prefab;
        }

        private static void AssignNetworkPrefabs(NetworkTestBootstrap bootstrap, GameObject projectilePrefab)
        {
            var serializedBootstrap = new SerializedObject(bootstrap);
            SerializedProperty prefabs = serializedBootstrap.FindProperty("networkPrefabs");
            prefabs.arraySize = 1;
            prefabs.GetArrayElementAtIndex(0).objectReferenceValue = projectilePrefab;
            serializedBootstrap.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void RemoveNetworkObjectFromNetworkManager()
        {
            NetworkManager networkManager = Object.FindFirstObjectByType<NetworkManager>();
            if (networkManager == null)
            {
                return;
            }

            NetworkObject networkObject = networkManager.GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                Object.DestroyImmediate(networkObject);
            }
        }

        private static void RemoveSceneTestPlayer()
        {
            GameObject player = GameObject.Find("TestPlayer");
            if (player != null && !PrefabUtility.IsPartOfPrefabAsset(player))
            {
                Object.DestroyImmediate(player);
            }
        }

        private static void RemoveItemSystemTestRunner()
        {
            ItemSystemTestRunner runner = Object.FindFirstObjectByType<ItemSystemTestRunner>();
            if (runner != null)
            {
                Object.DestroyImmediate(runner.gameObject);
            }
        }

        private static void EnsureSimulationRootCommands()
        {
            GameObject root = GameObject.Find("NetworkSimulationRoot");
            if (root == null)
            {
                return;
            }

            if (!root.TryGetComponent(out NetworkObject _))
            {
                root.AddComponent<NetworkObject>();
            }

            if (!root.TryGetComponent(out NetworkTestCommands _))
            {
                root.AddComponent<NetworkTestCommands>();
            }

            if (!root.TryGetComponent(out NetworkCombatAuthority _))
            {
                root.AddComponent<NetworkCombatAuthority>();
            }

            CombatSimulationRunner runner = root.GetComponent<CombatSimulationRunner>();
            if (runner == null)
            {
                runner = root.AddComponent<CombatSimulationRunner>();
            }

            var serializedRunner = new SerializedObject(runner);
            serializedRunner.FindProperty("combatants").arraySize = 0;
            serializedRunner.FindProperty("tickInUpdate").boolValue = false;
            serializedRunner.ApplyModifiedPropertiesWithoutUndo();
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
            if (importer == null || importer.textureType == TextureImporterType.Sprite)
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

        private static EnemyKilledEventChannel EnsureEnemyKilledEventChannel()
        {
            EnsureFolder("Assets/Project/Data", "Events");

            EnemyKilledEventChannel channel = AssetDatabase.LoadAssetAtPath<EnemyKilledEventChannel>(EnemyKilledEventChannelPath);
            if (channel != null)
            {
                return channel;
            }

            channel = ScriptableObject.CreateInstance<EnemyKilledEventChannel>();
            AssetDatabase.CreateAsset(channel, EnemyKilledEventChannelPath);
            AssetDatabase.SaveAssets();
            return channel;
        }

        private static EntityStatsDefinition EnsureTestPlayerStats()
        {
            EnsureFolder("Assets/Project/Data", "Players");
            return EnsureStatsDefinition(TestPlayerStatsPath);
        }

        private static EntityStatsDefinition EnsureTestEnemyStats()
        {
            EnsureFolder("Assets/Project/Data", "Enemies");
            return EnsureStatsDefinition(TestEnemyStatsPath);
        }

        private static DropTableDefinition EnsureTestEnemyDropTable()
        {
            EnsureFolder("Assets/Project/Data", "RewardTables");

            DropTableDefinition dropTable = AssetDatabase.LoadAssetAtPath<DropTableDefinition>(TestEnemyDropTablePath);
            if (dropTable != null)
            {
                return dropTable;
            }

            dropTable = ScriptableObject.CreateInstance<DropTableDefinition>();
            AssetDatabase.CreateAsset(dropTable, TestEnemyDropTablePath);
            AssetDatabase.SaveAssets();
            return dropTable;
        }

        private static EntityStatsDefinition EnsureStatsDefinition(string path)
        {
            EntityStatsDefinition stats = AssetDatabase.LoadAssetAtPath<EntityStatsDefinition>(path);
            if (stats != null)
            {
                return stats;
            }

            stats = ScriptableObject.CreateInstance<EntityStatsDefinition>();
            AssetDatabase.CreateAsset(stats, path);
            AssetDatabase.SaveAssets();
            return stats;
        }

        private static void AssignBaseStats(GameObject gameObject, EntityStatsDefinition stats)
        {
            StatsComponent statsComponent = gameObject.GetComponent<StatsComponent>();
            if (statsComponent != null)
            {
                AssignBaseStats(statsComponent, stats);
            }
        }

        private static void AssignBaseStats(StatsComponent statsComponent, EntityStatsDefinition stats)
        {
            var serializedStats = new SerializedObject(statsComponent);
            serializedStats.FindProperty("baseStats").objectReferenceValue = stats;
            serializedStats.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureEnemyCombatComponents(GameObject enemy, GameObject projectilePrefab)
        {
            CombatComponent combat = enemy.GetComponent<CombatComponent>();
            if (combat == null)
            {
                combat = enemy.AddComponent<CombatComponent>();
            }

            AssignCombatTeam(combat, TeamId.Enemy);

            AutoProjectileAttackComponent projectileAttack = enemy.GetComponent<AutoProjectileAttackComponent>();
            if (projectileAttack == null)
            {
                projectileAttack = enemy.AddComponent<AutoProjectileAttackComponent>();
            }

            AssignAutoProjectileAttack(projectileAttack, TeamId.Enemy, projectilePrefab);
        }

        private static void AssignTargetableTeam(TargetableComponent targetable, TeamId team)
        {
            var serializedTargetable = new SerializedObject(targetable);
            serializedTargetable.FindProperty("team").enumValueIndex = (int)team;
            serializedTargetable.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignCombatTeam(CombatComponent combat, TeamId team)
        {
            var serializedCombat = new SerializedObject(combat);
            serializedCombat.FindProperty("team").enumValueIndex = (int)team;
            serializedCombat.FindProperty("autoAttack").boolValue = true;
            serializedCombat.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignAutoProjectileAttack(AutoProjectileAttackComponent projectileAttack, TeamId team, GameObject projectilePrefab)
        {
            var serializedProjectileAttack = new SerializedObject(projectileAttack);
            serializedProjectileAttack.FindProperty("team").enumValueIndex = (int)team;
            serializedProjectileAttack.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab;
            serializedProjectileAttack.FindProperty("autoFire").boolValue = true;
            serializedProjectileAttack.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignEnemyKilledEventChannel(GameObject enemy, EnemyKilledEventChannel channel)
        {
            EnemyEntity enemyEntity = enemy.GetComponent<EnemyEntity>();
            if (enemyEntity != null)
            {
                AssignEnemyKilledEventChannel(enemyEntity, channel);
            }
        }

        private static void AssignEnemyKilledEventChannel(EnemyEntity enemy, EnemyKilledEventChannel channel)
        {
            var serializedEnemy = new SerializedObject(enemy);
            serializedEnemy.FindProperty("enemyKilledEventChannel").objectReferenceValue = channel;
            serializedEnemy.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignEnemyDropTable(GameObject enemy, DropTableDefinition dropTable)
        {
            ItemDropComponent itemDrop = enemy.GetComponent<ItemDropComponent>();
            if (itemDrop == null)
            {
                itemDrop = enemy.AddComponent<ItemDropComponent>();
            }

            AssignEnemyDropTable(itemDrop, dropTable);
        }

        private static void AssignEnemyDropTable(ItemDropComponent itemDrop, DropTableDefinition dropTable)
        {
            var serializedItemDrop = new SerializedObject(itemDrop);
            serializedItemDrop.FindProperty("dropTable").objectReferenceValue = dropTable;
            serializedItemDrop.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureRespawnComponent(GameObject gameObject)
        {
            if (!gameObject.TryGetComponent(out RespawnComponent respawn))
            {
                respawn = gameObject.AddComponent<RespawnComponent>();
            }

            AssignRespawnSettings(respawn, true);
        }

        private static void EnsureCharacterStateComponent(GameObject gameObject)
        {
            if (!gameObject.TryGetComponent(out CharacterStateComponent _))
            {
                gameObject.AddComponent<CharacterStateComponent>();
            }
        }

        private static void AssignRespawnSettings(RespawnComponent respawn, bool autoRespawnOnDeath)
        {
            var serializedRespawn = new SerializedObject(respawn);
            serializedRespawn.FindProperty("autoRespawnOnDeath").boolValue = autoRespawnOnDeath;
            serializedRespawn.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsurePlayerProgressionComponents(GameObject player)
        {
            if (!player.TryGetComponent(out PlayerJobComponent _))
            {
                player.AddComponent<PlayerJobComponent>();
            }

            if (!player.TryGetComponent(out SkillTreeComponent _))
            {
                player.AddComponent<SkillTreeComponent>();
            }

            if (!player.TryGetComponent(out PlayerProgressionComponent _))
            {
                player.AddComponent<PlayerProgressionComponent>();
            }

            if (!player.TryGetComponent(out InventoryComponent _))
            {
                player.AddComponent<InventoryComponent>();
            }

            if (!player.TryGetComponent(out EquipComponent _))
            {
                player.AddComponent<EquipComponent>();
            }

            if (!player.TryGetComponent(out PlayerSaveComponent _))
            {
                player.AddComponent<PlayerSaveComponent>();
            }
        }
    }
}
