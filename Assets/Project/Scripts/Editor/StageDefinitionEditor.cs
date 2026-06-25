using MP.Gameplay.Stages;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace MP.Editor
{
    [CustomEditor(typeof(StageDefinition))]
    public sealed class StageDefinitionEditor : UnityEditor.Editor
    {
        private SerializedProperty stageId;
        private SerializedProperty displayName;
        private SerializedProperty startingGold;
        private SerializedProperty startingExperience;
        private SerializedProperty waves;
        private ReorderableList wavesList;

        private void OnEnable()
        {
            stageId = serializedObject.FindProperty("stageId");
            displayName = serializedObject.FindProperty("displayName");
            startingGold = serializedObject.FindProperty("startingGold");
            startingExperience = serializedObject.FindProperty("startingExperience");
            waves = serializedObject.FindProperty("waves");

            wavesList = new ReorderableList(serializedObject, waves, true, true, true, true)
            {
                drawHeaderCallback = DrawWavesHeader,
                drawElementCallback = DrawWaveElement,
                elementHeightCallback = GetWaveElementHeight
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(stageId);
            EditorGUILayout.PropertyField(displayName);
            EditorGUILayout.PropertyField(startingGold);
            EditorGUILayout.PropertyField(startingExperience);

            EditorGUILayout.Space(8f);
            wavesList.DoLayoutList();
            DrawStageSummary();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawWavesHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, $"Waves ({waves.arraySize})");
        }

        private void DrawWaveElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty wave = waves.GetArrayElementAtIndex(index);
            rect.y += 2f;
            rect.height = EditorGUI.GetPropertyHeight(wave, true);
            EditorGUI.PropertyField(rect, wave, CreateWaveLabel(wave, index), true);
        }

        private float GetWaveElementHeight(int index)
        {
            SerializedProperty wave = waves.GetArrayElementAtIndex(index);
            return EditorGUI.GetPropertyHeight(wave, true) + 6f;
        }

        private GUIContent CreateWaveLabel(SerializedProperty wave, int index)
        {
            SerializedProperty nameProperty = wave.FindPropertyRelative("displayName");
            SerializedProperty waveDuration = wave.FindPropertyRelative("waveDuration");
            SerializedProperty spawnDuration = wave.FindPropertyRelative("spawnDuration");
            SerializedProperty spawnInterval = wave.FindPropertyRelative("spawnInterval");
            SerializedProperty maxAliveEnemies = wave.FindPropertyRelative("maxAliveEnemies");
            SerializedProperty bossWave = wave.FindPropertyRelative("bossWave");
            SerializedProperty bossSpawnTime = wave.FindPropertyRelative("bossSpawnTime");

            string waveName = string.IsNullOrWhiteSpace(nameProperty.stringValue) ? $"Wave {index + 1}" : nameProperty.stringValue;
            string bossText = bossWave.boolValue ? $" | Boss @ {Mathf.Max(0f, bossSpawnTime.floatValue):0.#}s" : string.Empty;
            string text = $"{index + 1}. {waveName}";
            string tooltip = $"Wave {Mathf.Max(0f, waveDuration.floatValue):0.#}s | Spawn {Mathf.Max(0f, spawnDuration.floatValue):0.#}s / {Mathf.Max(0.1f, spawnInterval.floatValue):0.##}s | Max {Mathf.Max(0, maxAliveEnemies.intValue)}{bossText}";
            return new GUIContent(text, tooltip);
        }

        private void DrawStageSummary()
        {
            if (waves.arraySize == 0)
            {
                EditorGUILayout.HelpBox("This stage has no waves. Add at least one wave before using it in gameplay.", MessageType.Warning);
                return;
            }

            float totalWaveTime = 0f;
            int bossWaveCount = 0;
            for (int i = 0; i < waves.arraySize; i++)
            {
                SerializedProperty wave = waves.GetArrayElementAtIndex(i);
                totalWaveTime += Mathf.Max(0f, wave.FindPropertyRelative("waveDuration").floatValue);
                if (wave.FindPropertyRelative("bossWave").boolValue)
                {
                    bossWaveCount++;
                }
            }

            EditorGUILayout.HelpBox($"Summary: {waves.arraySize} waves, {bossWaveCount} boss waves, {totalWaveTime:0.#}s total wave time.", MessageType.Info);
        }
    }

    [CustomPropertyDrawer(typeof(WaveDefinition))]
    public sealed class WaveDefinitionDrawer : PropertyDrawer
    {
        private const float VerticalSpacing = 2f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            Rect foldoutRect = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, CreateLabel(property, label), true);

            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            EditorGUI.indentLevel++;
            float y = foldoutRect.yMax + VerticalSpacing;
            DrawProperty(ref y, position, property.FindPropertyRelative("displayName"));
            DrawProperty(ref y, position, property.FindPropertyRelative("waveDuration"));
            DrawProperty(ref y, position, property.FindPropertyRelative("spawnDuration"));
            DrawProperty(ref y, position, property.FindPropertyRelative("spawnInterval"));
            DrawProperty(ref y, position, property.FindPropertyRelative("maxAliveEnemies"));

            y += VerticalSpacing;
            SerializedProperty bossWave = property.FindPropertyRelative("bossWave");
            DrawProperty(ref y, position, bossWave);
            if (bossWave.boolValue)
            {
                DrawProperty(ref y, position, property.FindPropertyRelative("bossPrefab"));
                DrawProperty(ref y, position, property.FindPropertyRelative("bossSpawnTime"));
            }

            y += VerticalSpacing;
            DrawProperty(ref y, position, property.FindPropertyRelative("spawnEntries"), true);
            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            float height = EditorGUIUtility.singleLineHeight + VerticalSpacing;
            height += GetHeight(property.FindPropertyRelative("displayName"));
            height += GetHeight(property.FindPropertyRelative("waveDuration"));
            height += GetHeight(property.FindPropertyRelative("spawnDuration"));
            height += GetHeight(property.FindPropertyRelative("spawnInterval"));
            height += GetHeight(property.FindPropertyRelative("maxAliveEnemies"));
            height += VerticalSpacing;
            height += GetHeight(property.FindPropertyRelative("bossWave"));
            if (property.FindPropertyRelative("bossWave").boolValue)
            {
                height += GetHeight(property.FindPropertyRelative("bossPrefab"));
                height += GetHeight(property.FindPropertyRelative("bossSpawnTime"));
            }

            height += VerticalSpacing;
            height += GetHeight(property.FindPropertyRelative("spawnEntries"), true);
            return height;
        }

        private static GUIContent CreateLabel(SerializedProperty property, GUIContent fallback)
        {
            SerializedProperty nameProperty = property.FindPropertyRelative("displayName");
            SerializedProperty waveDuration = property.FindPropertyRelative("waveDuration");
            SerializedProperty spawnDuration = property.FindPropertyRelative("spawnDuration");
            SerializedProperty spawnInterval = property.FindPropertyRelative("spawnInterval");
            SerializedProperty maxAliveEnemies = property.FindPropertyRelative("maxAliveEnemies");
            SerializedProperty bossWave = property.FindPropertyRelative("bossWave");
            SerializedProperty bossSpawnTime = property.FindPropertyRelative("bossSpawnTime");

            string waveName = string.IsNullOrWhiteSpace(nameProperty.stringValue) ? fallback.text : nameProperty.stringValue;
            string bossText = bossWave.boolValue ? $" | Boss @ {Mathf.Max(0f, bossSpawnTime.floatValue):0.#}s" : string.Empty;
            string text = $"{waveName}  ({Mathf.Max(0f, waveDuration.floatValue):0.#}s, spawn {Mathf.Max(0f, spawnDuration.floatValue):0.#}s/{Mathf.Max(0.1f, spawnInterval.floatValue):0.##}s, max {Mathf.Max(0, maxAliveEnemies.intValue)}{bossText})";
            return new GUIContent(text);
        }

        private static void DrawProperty(ref float y, Rect position, SerializedProperty property, bool includeChildren = false)
        {
            float height = EditorGUI.GetPropertyHeight(property, includeChildren);
            Rect rect = new(position.x, y, position.width, height);
            EditorGUI.PropertyField(rect, property, includeChildren);
            y += height + VerticalSpacing;
        }

        private static float GetHeight(SerializedProperty property, bool includeChildren = false)
        {
            return EditorGUI.GetPropertyHeight(property, includeChildren) + VerticalSpacing;
        }
    }

    [CustomPropertyDrawer(typeof(EnemySpawnEntry))]
    public sealed class EnemySpawnEntryDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty enemyPrefab = property.FindPropertyRelative("enemyPrefab");
            SerializedProperty weight = property.FindPropertyRelative("weight");

            EditorGUI.BeginProperty(position, label, property);
            Rect labelRect = new(position.x, position.y, EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight);
            Rect prefabRect = new(labelRect.xMax, position.y, position.width - EditorGUIUtility.labelWidth - 72f, EditorGUIUtility.singleLineHeight);
            Rect weightRect = new(prefabRect.xMax + 4f, position.y, 68f, EditorGUIUtility.singleLineHeight);

            string labelText = enemyPrefab.objectReferenceValue != null ? enemyPrefab.objectReferenceValue.name : label.text;
            EditorGUI.LabelField(labelRect, labelText);
            EditorGUI.PropertyField(prefabRect, enemyPrefab, GUIContent.none);
            EditorGUI.PropertyField(weightRect, weight, GUIContent.none);
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
