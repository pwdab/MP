using System.Collections.Generic;
using MP.Gameplay.Stats;
using UnityEngine;

namespace MP.Items
{
    [RequireComponent(typeof(StatsComponent))]
    public sealed class EquipComponent : MonoBehaviour
    {
        [SerializeField] private List<EquippedItemSlot> equippedSlots = new();

        private StatsComponent stats;

        public event System.Action<EquipSlotId, ItemInstance> SlotEquipped;
        public event System.Action<EquipSlotId, ItemInstance> SlotUnequipped;

        public IReadOnlyList<EquippedItemSlot> EquippedSlots => equippedSlots;

        private void Awake()
        {
            EnsureEquippedSlots();
            EnsureStats();
            RemoveInvalidSlots();
            ApplyEquippedModifiers();
        }

        private void OnValidate()
        {
            EnsureEquippedSlots();
            RemoveInvalidSlots();
        }

        public bool TryEquip(InventorySlot slot)
        {
            return slot != null && TryEquip(slot.Item);
        }

        public bool TryEquip(ItemInstance item)
        {
            if (item == null || item.Definition == null || !item.Definition.CanEquip || item.Definition.EquipSlot == EquipSlotId.None)
            {
                return false;
            }

            EquipSlotId slotId = item.Definition.EquipSlot;
            EquippedItemSlot slot = GetOrCreateSlot(slotId);
            if (slot == null || !slot.CanAccept(item))
            {
                return false;
            }

            if (ReferenceEquals(slot.Item, item))
            {
                return true;
            }

            Unequip(slotId);
            if (!slot.TrySetItem(item))
            {
                return false;
            }

            EnsureStats();
            stats.ReplaceModifiersFrom(CreateModifierSource(item), item.Definition.EquipStatModifiers);
            RaiseSlotEquipped(slotId, item);
            return true;
        }

        public bool Unequip()
        {
            EnsureEquippedSlots();
            bool removedAny = false;
            for (int i = 0; i < equippedSlots.Count; i++)
            {
                EquippedItemSlot slot = equippedSlots[i];
                if (slot == null || !slot.HasItem)
                {
                    continue;
                }

                EnsureStats();
                ItemInstance removedItem = slot.Item;
                stats.RemoveModifiersFrom(CreateModifierSource(removedItem));
                slot.Clear();
                RaiseSlotUnequipped(slot.SlotId, removedItem);
                removedAny = true;
            }

            return removedAny;
        }

        public bool Unequip(EquipSlotId slotId)
        {
            EquippedItemSlot slot = FindSlot(slotId);
            if (slot == null || !slot.HasItem)
            {
                return false;
            }

            EnsureStats();
            ItemInstance removedItem = slot.Item;
            stats.RemoveModifiersFrom(CreateModifierSource(removedItem));
            slot.Clear();
            RaiseSlotUnequipped(slotId, removedItem);
            return true;
        }

        public ItemInstance GetEquippedItem(EquipSlotId slotId)
        {
            EquippedItemSlot slot = FindSlot(slotId);
            return slot != null ? slot.Item : null;
        }

        public bool IsEquipped(ItemInstance item)
        {
            return FindSlotContaining(item) != null;
        }

        public bool Unequip(ItemInstance item)
        {
            EquippedItemSlot slot = FindSlotContaining(item);
            return slot != null && Unequip(slot.SlotId);
        }

        private EquippedItemSlot GetOrCreateSlot(EquipSlotId slotId)
        {
            if (slotId == EquipSlotId.None)
            {
                return null;
            }

            EquippedItemSlot slot = FindSlot(slotId);
            if (slot != null)
            {
                return slot;
            }

            slot = new EquippedItemSlot(slotId);
            equippedSlots.Add(slot);
            return slot;
        }

        private EquippedItemSlot FindSlot(EquipSlotId slotId)
        {
            if (slotId == EquipSlotId.None)
            {
                return null;
            }

            EnsureEquippedSlots();
            for (int i = 0; i < equippedSlots.Count; i++)
            {
                EquippedItemSlot slot = equippedSlots[i];
                if (slot != null && slot.SlotId == slotId)
                {
                    return slot;
                }
            }

            return null;
        }

        private EquippedItemSlot FindSlotContaining(ItemInstance item)
        {
            if (item == null)
            {
                return null;
            }

            item.EnsureInstanceId();
            EnsureEquippedSlots();
            for (int i = 0; i < equippedSlots.Count; i++)
            {
                EquippedItemSlot slot = equippedSlots[i];
                if (slot != null && slot.Item != null && slot.Item.InstanceId == item.InstanceId)
                {
                    return slot;
                }
            }

            return null;
        }

        private void RemoveInvalidSlots()
        {
            EnsureEquippedSlots();
            for (int i = equippedSlots.Count - 1; i >= 0; i--)
            {
                EquippedItemSlot slot = equippedSlots[i];
                if (slot == null || slot.SlotId == EquipSlotId.None || HasEarlierSlot(slot.SlotId, i))
                {
                    equippedSlots.RemoveAt(i);
                    continue;
                }

                if (slot.HasItem && !slot.CanAccept(slot.Item))
                {
                    slot.Clear();
                }
            }
        }

        private bool HasEarlierSlot(EquipSlotId slotId, int beforeIndex)
        {
            for (int i = 0; i < beforeIndex; i++)
            {
                EquippedItemSlot slot = equippedSlots[i];
                if (slot != null && slot.SlotId == slotId)
                {
                    return true;
                }
            }

            return false;
        }

        private void ApplyEquippedModifiers()
        {
            EnsureEquippedSlots();
            EnsureStats();
            for (int i = 0; i < equippedSlots.Count; i++)
            {
                EquippedItemSlot slot = equippedSlots[i];
                if (slot == null || !slot.HasItem || !slot.CanAccept(slot.Item))
                {
                    continue;
                }

                stats.ReplaceModifiersFrom(CreateModifierSource(slot.Item), slot.Item.Definition.EquipStatModifiers);
            }
        }

        private static StatModifierSource CreateModifierSource(ItemInstance item)
        {
            string displayName = item != null && item.Definition != null ? item.Definition.DisplayName : "Equipment";
            return new StatModifierSource(item, StatModifierSourceType.Equipment, displayName);
        }

        private void EnsureEquippedSlots()
        {
            equippedSlots ??= new List<EquippedItemSlot>();
        }

        private void EnsureStats()
        {
            stats ??= GetComponent<StatsComponent>();
        }

        private void RaiseSlotEquipped(EquipSlotId slotId, ItemInstance item)
        {
            SlotEquipped?.Invoke(slotId, item);
        }

        private void RaiseSlotUnequipped(EquipSlotId slotId, ItemInstance item)
        {
            SlotUnequipped?.Invoke(slotId, item);
        }
    }
}
