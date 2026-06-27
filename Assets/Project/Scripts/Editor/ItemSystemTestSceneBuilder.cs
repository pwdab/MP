using System;
using System.IO;
using MP.Gameplay.Stats;
using MP.Items;
using MP.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MP.Editor
{
    public static class ItemSystemTestSceneBuilder
    {
        private const string ScenePath = "Assets/Project/Scenes/Test/ItemSystemTest.unity";
        private const string TestDataFolder = "Assets/Project/Data/Items/Test";
        private const string TestPrefabFolder = "Assets/Project/Prefabs/Items/Test";

        private const string StatCatalogPath = "Assets/Project/Data/Stats/PrototypeStatCatalog.asset";
        private const string BaseStatsPath = TestDataFolder + "/ItemSystemTestBaseStats.asset";
        private const string PotionPath = TestDataFolder + "/TestPotion.asset";
        private const string SwordPath = TestDataFolder + "/TestSword.asset";
        private const string ArmorPath = TestDataFolder + "/TestArmor.asset";
        private const string SwordModifierPath = TestDataFolder + "/TestSwordAttackModifier.asset";
        private const string ArmorModifierPath = TestDataFolder + "/TestArmorHealthModifier.asset";
        private const string DroppedItemPrefabPath = TestPrefabFolder + "/TestDroppedItem.prefab";

        [MenuItem("MP/Test Scenes/Create Item System Test Scene")]
        public static void CreateItemSystemTestScene()
        {
            EnsureFolders();

            EntityStatsDefinition baseStats = EnsureBaseStats();
            StatModifierDefinition swordModifier = EnsureStatModifierDefinition(SwordModifierPath, StatId.AttackPower, StatModifierType.Flat, 5f);
            StatModifierDefinition armorModifier = EnsureStatModifierDefinition(ArmorModifierPath, StatId.MaxHealth, StatModifierType.Flat, 25f);
            GameObject droppedItemPrefab = EnsureDroppedItemPrefab();

            ItemDefinition potion = EnsureItemDefinition(PotionPath, "test_potion", "Test Potion", droppedItemPrefab, true, 3, false, EquipSlotId.None, null);
            ItemDefinition sword = EnsureItemDefinition(SwordPath, "test_sword", "Test Sword", droppedItemPrefab, false, 1, true, EquipSlotId.Weapon, new[] { swordModifier });
            ItemDefinition armor = EnsureItemDefinition(ArmorPath, "test_armor", "Test Armor", droppedItemPrefab, false, 1, true, EquipSlotId.Armor, new[] { armorModifier });

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "ItemSystemTest";

            RemoveCombatNetworkTestHud();
            CreateCamera();
            CreateGroundMarkers();
            CreateTestObject(baseStats, potion, sword, armor, droppedItemPrefab);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Created item system test scene at {ScenePath}.");
        }

        [MenuItem("MP/Test Scenes/Repair Open Item System Test Scene")]
        public static void RepairOpenItemSystemTestScene()
        {
            EnsureFolders();

            EntityStatsDefinition baseStats = EnsureBaseStats();
            StatModifierDefinition swordModifier = EnsureStatModifierDefinition(SwordModifierPath, StatId.AttackPower, StatModifierType.Flat, 5f);
            StatModifierDefinition armorModifier = EnsureStatModifierDefinition(ArmorModifierPath, StatId.MaxHealth, StatModifierType.Flat, 25f);
            GameObject droppedItemPrefab = EnsureDroppedItemPrefab();

            ItemDefinition potion = EnsureItemDefinition(PotionPath, "test_potion", "Test Potion", droppedItemPrefab, true, 3, false, EquipSlotId.None, null);
            ItemDefinition sword = EnsureItemDefinition(SwordPath, "test_sword", "Test Sword", droppedItemPrefab, false, 1, true, EquipSlotId.Weapon, new[] { swordModifier });
            ItemDefinition armor = EnsureItemDefinition(ArmorPath, "test_armor", "Test Armor", droppedItemPrefab, false, 1, true, EquipSlotId.Armor, new[] { armorModifier });

            RemoveCombatNetworkTestHud();

            ItemSystemTestRunner runner = UnityEngine.Object.FindFirstObjectByType<ItemSystemTestRunner>();
            if (runner == null)
            {
                Debug.LogWarning("Open scene has no ItemSystemTestRunner. Creating one.");
                CreateTestObject(baseStats, potion, sword, armor, droppedItemPrefab);
            }
            else
            {
                GameObject testObject = runner.gameObject;
                StatsComponent stats = testObject.GetComponent<StatsComponent>();
                if (stats == null)
                {
                    stats = testObject.AddComponent<StatsComponent>();
                }

                InventoryComponent inventory = testObject.GetComponent<InventoryComponent>();
                if (inventory == null)
                {
                    inventory = testObject.AddComponent<InventoryComponent>();
                }

                EquipComponent equip = testObject.GetComponent<EquipComponent>();
                if (equip == null)
                {
                    equip = testObject.AddComponent<EquipComponent>();
                }

                SetObject(stats, "baseStats", baseStats);
                SetObject(runner, "inventory", inventory);
                SetObject(runner, "equip", equip);
                SetObject(runner, "stats", stats);
                SetObject(runner, "potion", potion);
                SetObject(runner, "sword", sword);
                SetObject(runner, "armor", armor);
                SetObject(runner, "droppedItemPrefab", droppedItemPrefab);
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Repaired open item system test scene references.");
        }

        private static void RemoveCombatNetworkTestHud()
        {
            CombatNetworkTestHud hud = UnityEngine.Object.FindFirstObjectByType<CombatNetworkTestHud>();
            if (hud != null)
            {
                UnityEngine.Object.DestroyImmediate(hud.gameObject);
            }
        }

        private static void EnsureFolders()
        {
            Directory.CreateDirectory("Assets/Project/Scenes/Test");
            Directory.CreateDirectory("Assets/Project/Data/Stats");
            Directory.CreateDirectory(TestDataFolder);
            Directory.CreateDirectory(TestPrefabFolder);
        }

        private static EntityStatsDefinition EnsureBaseStats()
        {
            StatCatalogDefinition statCatalog = EnsureStatCatalogDefinition();
            EntityStatsDefinition baseStats = EnsureAsset<EntityStatsDefinition>(BaseStatsPath);

            var serializedStats = new SerializedObject(baseStats);
            serializedStats.FindProperty("statCatalog").objectReferenceValue = statCatalog;
            serializedStats.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(baseStats);
            return baseStats;
        }

        private static StatCatalogDefinition EnsureStatCatalogDefinition()
        {
            StatCatalogDefinition catalog = AssetDatabase.LoadAssetAtPath<StatCatalogDefinition>(StatCatalogPath);
            if (catalog != null)
            {
                return catalog;
            }

            catalog = ScriptableObject.CreateInstance<StatCatalogDefinition>();
            AssetDatabase.CreateAsset(catalog, StatCatalogPath);

            var serializedCatalog = new SerializedObject(catalog);
            SerializedProperty statsProperty = serializedCatalog.FindProperty("stats");
            Array statIds = Enum.GetValues(typeof(StatId));
            statsProperty.arraySize = statIds.Length;
            for (int i = 0; i < statIds.Length; i++)
            {
                StatDefinition definition = StatCatalogDefinition.CreateDefaultDefinition((StatId)statIds.GetValue(i));
                SerializedProperty entry = statsProperty.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("statId").enumValueIndex = (int)definition.StatId;
                entry.FindPropertyRelative("bounds").FindPropertyRelative("minimum").floatValue = definition.Bounds.Minimum;
                entry.FindPropertyRelative("bounds").FindPropertyRelative("maximum").floatValue = definition.Bounds.Maximum;
            }

            serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static void CreateCamera()
        {
            var cameraObject = new GameObject("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.backgroundColor = new Color(0.08f, 0.09f, 0.11f);
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            cameraObject.tag = "MainCamera";
        }

        private static void CreateGroundMarkers()
        {
            CreateMarker("Inventory Area", new Vector3(-2.5f, 0f, 0f), new Color(0.2f, 0.45f, 1f));
            CreateMarker("Equip Area", new Vector3(0f, 0f, 0f), new Color(0.2f, 0.8f, 0.35f));
            CreateMarker("Drop Area", new Vector3(2.5f, 0f, 0f), new Color(1f, 0.75f, 0.2f));
        }

        private static void CreateMarker(string name, Vector3 position, Color color)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = name;
            marker.transform.position = position;
            marker.transform.localScale = new Vector3(1.25f, 1.25f, 0.1f);
            marker.GetComponent<Renderer>().sharedMaterial = CreateMaterial(color);
        }

        private static void CreateTestObject(EntityStatsDefinition baseStats, ItemDefinition potion, ItemDefinition sword, ItemDefinition armor, GameObject droppedItemPrefab)
        {
            var testObject = new GameObject("ItemSystemTestRunner");
            StatsComponent stats = testObject.AddComponent<StatsComponent>();
            InventoryComponent inventory = testObject.AddComponent<InventoryComponent>();
            EquipComponent equip = testObject.AddComponent<EquipComponent>();
            ItemSystemTestRunner runner = testObject.AddComponent<ItemSystemTestRunner>();

            SetObject(stats, "baseStats", baseStats);
            SetObject(runner, "inventory", inventory);
            SetObject(runner, "equip", equip);
            SetObject(runner, "stats", stats);
            SetObject(runner, "potion", potion);
            SetObject(runner, "sword", sword);
            SetObject(runner, "armor", armor);
            SetObject(runner, "droppedItemPrefab", droppedItemPrefab);
        }

        private static GameObject EnsureDroppedItemPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DroppedItemPrefabPath);
            if (prefab != null)
            {
                return prefab;
            }

            GameObject droppedItem = GameObject.CreatePrimitive(PrimitiveType.Cube);
            droppedItem.name = "TestDroppedItem";
            droppedItem.transform.localScale = new Vector3(0.5f, 0.5f, 0.1f);
            droppedItem.GetComponent<Renderer>().sharedMaterial = CreateMaterial(new Color(1f, 0.85f, 0.25f));
            droppedItem.AddComponent<DroppedItem>();

            prefab = PrefabUtility.SaveAsPrefabAsset(droppedItem, DroppedItemPrefabPath);
            UnityEngine.Object.DestroyImmediate(droppedItem);
            return prefab;
        }

        private static ItemDefinition EnsureItemDefinition(
            string path,
            string itemId,
            string displayName,
            GameObject dropPrefab,
            bool isStackable,
            int maxStackSize,
            bool canEquip,
            EquipSlotId equipSlot,
            StatModifierDefinition[] equipStatModifiers)
        {
            ItemDefinition itemDefinition = EnsureAsset<ItemDefinition>(path);
            var serializedObject = new SerializedObject(itemDefinition);
            serializedObject.FindProperty("itemId").stringValue = itemId;
            serializedObject.FindProperty("displayName").stringValue = displayName;
            serializedObject.FindProperty("dropPrefab").objectReferenceValue = dropPrefab;
            serializedObject.FindProperty("isStackable").boolValue = isStackable;
            serializedObject.FindProperty("maxStackSize").intValue = maxStackSize;
            serializedObject.FindProperty("canEquip").boolValue = canEquip;
            serializedObject.FindProperty("equipSlot").enumValueIndex = (int)equipSlot;

            SerializedProperty modifiers = serializedObject.FindProperty("equipStatModifiers");
            int modifierCount = equipStatModifiers != null ? equipStatModifiers.Length : 0;
            modifiers.arraySize = modifierCount;
            for (int i = 0; i < modifierCount; i++)
            {
                modifiers.GetArrayElementAtIndex(i).objectReferenceValue = equipStatModifiers[i];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(itemDefinition);
            return itemDefinition;
        }

        private static StatModifierDefinition EnsureStatModifierDefinition(string path, StatId statId, StatModifierType type, float value)
        {
            StatModifierDefinition modifier = EnsureAsset<StatModifierDefinition>(path);
            var serializedObject = new SerializedObject(modifier);
            serializedObject.FindProperty("statId").enumValueIndex = (int)statId;
            serializedObject.FindProperty("type").enumValueIndex = (int)type;
            serializedObject.FindProperty("value").floatValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(modifier);
            return modifier;
        }

        private static T EnsureAsset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static Material CreateMaterial(Color color)
        {
            var material = new Material(Shader.Find("Sprites/Default"));
            material.color = color;
            return material;
        }

        private static void SetObject(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            var serializedObject = new SerializedObject(target);
            serializedObject.Update();

            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError($"{target.name} is missing serialized property '{propertyName}'.", target);
                return;
            }

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
    }
}
