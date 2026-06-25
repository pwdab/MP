using MP.Gameplay.Stats;
using MP.Items;
using MP.Progression.Jobs;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace MP.Editor
{
    [CustomEditor(typeof(EntityStatsDefinition))]
    public sealed class EntityStatsDefinitionEditor : UnityEditor.Editor
    {
        private SerializedProperty stats;
        private ReorderableList statsList;

        private void OnEnable()
        {
            stats = serializedObject.FindProperty("stats");
            statsList = new ReorderableList(serializedObject, stats, true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, $"Stats ({stats.arraySize})"),
                drawElementCallback = DrawStatElement,
                elementHeightCallback = index => EditorGUI.GetPropertyHeight(stats.GetArrayElementAtIndex(index), true) + 6f
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            statsList.DoLayoutList();
            EditorGUILayout.HelpBox("Each StatId should exist once. Base Value is clamped between Min and Max.", MessageType.Info);
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawStatElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty stat = stats.GetArrayElementAtIndex(index);
            rect.y += 2f;
            EditorGUI.PropertyField(rect, stat, CreateStatLabel(stat), true);
        }

        private static GUIContent CreateStatLabel(SerializedProperty stat)
        {
            string statName = stat.FindPropertyRelative("statId").enumDisplayNames[stat.FindPropertyRelative("statId").enumValueIndex];
            float baseValue = stat.FindPropertyRelative("baseValue").floatValue;
            SerializedProperty bounds = stat.FindPropertyRelative("bounds");
            float minimum = bounds.FindPropertyRelative("minimum").floatValue;
            float maximum = bounds.FindPropertyRelative("maximum").floatValue;
            return new GUIContent($"{statName}  Base {baseValue:0.##}  [{minimum:0.##}, {FormatMaximum(maximum)}]");
        }

        private static string FormatMaximum(float maximum)
        {
            return float.IsPositiveInfinity(maximum) || maximum >= float.MaxValue ? "inf" : maximum.ToString("0.##");
        }
    }

    [CustomEditor(typeof(ItemDefinition))]
    public sealed class ItemDefinitionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            Draw("itemId");
            Draw("displayName");
            Draw("dropPrefab");
            Draw("isStackable");
            Draw("maxStackSize");
            Draw("canEquip");

            if (serializedObject.FindProperty("canEquip").boolValue)
            {
                Draw("equipSlot");
                Draw("equipStatModifiers", true);
            }

            DrawItemSummary();
            serializedObject.ApplyModifiedProperties();
        }

        private void Draw(string propertyName, bool includeChildren = false)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty(propertyName), includeChildren);
        }

        private void DrawItemSummary()
        {
            SerializedProperty itemId = serializedObject.FindProperty("itemId");
            SerializedProperty canEquip = serializedObject.FindProperty("canEquip");
            SerializedProperty isStackable = serializedObject.FindProperty("isStackable");
            SerializedProperty maxStackSize = serializedObject.FindProperty("maxStackSize");
            string mode = canEquip.boolValue ? "Equipment" : isStackable.boolValue ? $"Stackable x{Mathf.Max(1, maxStackSize.intValue)}" : "Single Item";
            EditorGUILayout.HelpBox($"Summary: {mode} | ItemId: {(string.IsNullOrWhiteSpace(itemId.stringValue) ? "(empty)" : itemId.stringValue)}", MessageType.Info);
        }
    }

    [CustomEditor(typeof(DropTableDefinition))]
    public sealed class DropTableDefinitionEditor : UnityEditor.Editor
    {
        private SerializedProperty entries;
        private SerializedProperty dropScatterRadius;
        private ReorderableList entriesList;

        private void OnEnable()
        {
            entries = serializedObject.FindProperty("entries");
            dropScatterRadius = serializedObject.FindProperty("dropScatterRadius");
            entriesList = new ReorderableList(serializedObject, entries, true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, $"Drop Entries ({entries.arraySize})"),
                drawElementCallback = DrawDropEntryElement,
                elementHeightCallback = index => EditorGUI.GetPropertyHeight(entries.GetArrayElementAtIndex(index), true) + 6f
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            entriesList.DoLayoutList();
            EditorGUILayout.PropertyField(dropScatterRadius);
            DrawDropSummary();
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawDropEntryElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(index);
            rect.y += 2f;
            EditorGUI.PropertyField(rect, entry, CreateDropEntryLabel(entry, index), true);
        }

        private void DrawDropSummary()
        {
            if (entries.arraySize == 0)
            {
                EditorGUILayout.HelpBox("This drop table has no entries.", MessageType.Warning);
                return;
            }

            EditorGUILayout.HelpBox($"Summary: {entries.arraySize} entries, scatter radius {Mathf.Max(0f, dropScatterRadius.floatValue):0.##}.", MessageType.Info);
        }

        private static GUIContent CreateDropEntryLabel(SerializedProperty entry, int index)
        {
            SerializedProperty item = entry.FindPropertyRelative("item");
            SerializedProperty chance = entry.FindPropertyRelative("dropChance");
            SerializedProperty minQuantity = entry.FindPropertyRelative("minQuantity");
            SerializedProperty maxQuantity = entry.FindPropertyRelative("maxQuantity");
            string itemName = item.objectReferenceValue != null ? item.objectReferenceValue.name : $"Entry {index + 1}";
            return new GUIContent($"{itemName}  {Mathf.Clamp01(chance.floatValue) * 100f:0.#}%  x{Mathf.Max(1, minQuantity.intValue)}-{Mathf.Max(minQuantity.intValue, maxQuantity.intValue)}");
        }
    }

    [CustomEditor(typeof(JobDefinition))]
    public sealed class JobDefinitionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("jobId"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("displayName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("category"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("parentJob"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("statModifiers"), true);
            DrawJobSummary();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawJobSummary()
        {
            SerializedProperty displayName = serializedObject.FindProperty("displayName");
            SerializedProperty category = serializedObject.FindProperty("category");
            SerializedProperty modifiers = serializedObject.FindProperty("statModifiers");
            string jobName = string.IsNullOrWhiteSpace(displayName.stringValue) ? target.name : displayName.stringValue;
            EditorGUILayout.HelpBox($"Summary: {jobName} | {category.enumDisplayNames[category.enumValueIndex]} | {modifiers.arraySize} stat modifiers.", MessageType.Info);
        }
    }

    [CustomEditor(typeof(StatModifierDefinition))]
    public sealed class StatModifierDefinitionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("statId"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("type"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("value"));
            DrawModifierSummary();
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawModifierSummary()
        {
            SerializedProperty statId = serializedObject.FindProperty("statId");
            SerializedProperty type = serializedObject.FindProperty("type");
            SerializedProperty value = serializedObject.FindProperty("value");
            string amount = type.enumValueIndex == (int)StatModifierType.Percent ? $"{value.floatValue * 100f:+0.##;-0.##;0}%" : $"{value.floatValue:+0.##;-0.##;0}";
            EditorGUILayout.HelpBox($"Summary: {statId.enumDisplayNames[statId.enumValueIndex]} {amount} ({type.enumDisplayNames[type.enumValueIndex]})", MessageType.Info);
        }
    }

    [CustomPropertyDrawer(typeof(StatEntry))]
    public sealed class StatEntryDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty statId = property.FindPropertyRelative("statId");
            SerializedProperty baseValue = property.FindPropertyRelative("baseValue");
            SerializedProperty bounds = property.FindPropertyRelative("bounds");

            EditorGUI.BeginProperty(position, label, property);
            Rect line = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(line, statId);

            line.y += EditorGUIUtility.singleLineHeight + 2f;
            float third = (position.width - 8f) / 3f;
            EditorGUI.PropertyField(new Rect(position.x, line.y, third, line.height), baseValue, new GUIContent("Base"));
            EditorGUI.PropertyField(new Rect(position.x + third + 4f, line.y, third, line.height), bounds.FindPropertyRelative("minimum"), new GUIContent("Min"));
            EditorGUI.PropertyField(new Rect(position.x + (third + 4f) * 2f, line.y, third, line.height), bounds.FindPropertyRelative("maximum"), new GUIContent("Max"));
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 2f + 4f;
        }
    }

    [CustomPropertyDrawer(typeof(DropTableEntry))]
    public sealed class DropTableEntryDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty item = property.FindPropertyRelative("item");
            SerializedProperty dropChance = property.FindPropertyRelative("dropChance");
            SerializedProperty minQuantity = property.FindPropertyRelative("minQuantity");
            SerializedProperty maxQuantity = property.FindPropertyRelative("maxQuantity");

            EditorGUI.BeginProperty(position, label, property);
            Rect line = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(line, item);

            line.y += EditorGUIUtility.singleLineHeight + 2f;
            float third = (position.width - 8f) / 3f;
            EditorGUI.PropertyField(new Rect(position.x, line.y, third, line.height), dropChance, new GUIContent("Chance"));
            EditorGUI.PropertyField(new Rect(position.x + third + 4f, line.y, third, line.height), minQuantity, new GUIContent("Min"));
            EditorGUI.PropertyField(new Rect(position.x + (third + 4f) * 2f, line.y, third, line.height), maxQuantity, new GUIContent("Max"));
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight * 2f + 4f;
        }
    }
}
