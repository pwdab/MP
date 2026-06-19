using System;
using System.Collections.Generic;
using MP.Gameplay.Stats;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MP.Items
{
    [CreateAssetMenu(menuName = "MP/Data/Item Definition")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [SerializeField] private string itemId;
        [SerializeField] private string displayName;
        [SerializeField] private GameObject dropPrefab;
        [SerializeField] private bool isStackable = true;
        [SerializeField, Min(1)] private int maxStackSize = 99;
        [SerializeField] private bool canEquip;
        [SerializeField] private EquipSlotId equipSlot;
        [SerializeField] private StatModifierDefinition[] equipStatModifiers = Array.Empty<StatModifierDefinition>();

        public string ItemId => itemId;
        public string DisplayName => displayName;
        public GameObject DropPrefab => dropPrefab;
        public bool IsStackable => !canEquip && isStackable;
        public int MaxStackSize => IsStackable ? Mathf.Max(1, maxStackSize) : 1;
        public bool CanEquip => canEquip;
        public EquipSlotId EquipSlot => canEquip ? equipSlot : EquipSlotId.None;
        public IReadOnlyList<StatModifierDefinition> EquipStatModifiers => equipStatModifiers ?? Array.Empty<StatModifierDefinition>();

        private void OnValidate()
        {
            NormalizeItemId();
            NormalizeStackSettings();
            ValidateItemId();
            ValidateEquipSettings();
        }

        private void NormalizeItemId()
        {
            itemId = itemId != null ? itemId.Trim() : string.Empty;
        }

        private void NormalizeStackSettings()
        {
            if (canEquip)
            {
                isStackable = false;
            }
            else
            {
                equipSlot = EquipSlotId.None;
            }

            maxStackSize = IsStackable ? Mathf.Max(1, maxStackSize) : 1;
        }

        private void ValidateItemId()
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                Debug.LogWarning($"{name} has an empty itemId.", this);
                return;
            }

#if UNITY_EDITOR
            string[] itemAssetGuids = AssetDatabase.FindAssets("t:ItemDefinition");
            for (int i = 0; i < itemAssetGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(itemAssetGuids[i]);
                ItemDefinition itemDefinition = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
                if (itemDefinition != null && itemDefinition != this && string.Equals(itemDefinition.ItemId, itemId, StringComparison.Ordinal))
                {
                    Debug.LogWarning($"{name} has a duplicate itemId: {itemId}.", this);
                    return;
                }
            }
#endif
        }

        private void ValidateEquipSettings()
        {
            if (!canEquip && equipStatModifiers != null && equipStatModifiers.Length > 0)
            {
                Debug.LogWarning($"{name} has equip stat modifiers but is not equippable.", this);
            }

            if (canEquip && (equipStatModifiers == null || equipStatModifiers.Length == 0))
            {
                Debug.LogWarning($"{name} is equippable but has no equip stat modifiers.", this);
            }

            if (canEquip && equipSlot == EquipSlotId.None)
            {
                Debug.LogWarning($"{name} is equippable but has no equip slot.", this);
            }

            if (equipStatModifiers == null)
            {
                return;
            }

            for (int i = 0; i < equipStatModifiers.Length; i++)
            {
                if (equipStatModifiers[i] == null)
                {
                    Debug.LogWarning($"{name} has an empty equip stat modifier slot at index {i}.", this);
                }
            }
        }
    }
}
