using System.Collections.Generic;
using UnityEngine;

namespace MP.Items
{
    public sealed class InventoryComponent : MonoBehaviour, ISerializationCallbackReceiver
    {
        [SerializeField] private List<InventorySlot> slots = new();
        [SerializeField] private Transform dropOrigin;
        [SerializeField, Min(0f)] private float dropScatterRadius = 0.5f;

        private EquipComponent equip;

        public event System.Action<InventorySlot> SlotAdded;
        public event System.Action<InventorySlot> SlotChanged;
        public event System.Action<InventorySlot> SlotRemoved;

        public IReadOnlyList<InventorySlot> Slots => slots;
        public Vector3 DropPosition => dropOrigin != null ? dropOrigin.position : transform.position;

        private void Awake()
        {
            NormalizeSlots();
            EnsureEquip();
        }

        private void OnValidate()
        {
            NormalizeSlots();
        }

        public InventoryAddResult AddItem(ItemDefinition item, int quantity)
        {
            EnsureSlots();
            if (item == null || quantity <= 0)
            {
                return new InventoryAddResult(quantity, 0);
            }

            if (item.IsStackable)
            {
                return AddStackableItem(item, quantity);
            }

            return AddNonStackableItem(item, quantity);
        }

        public InventoryAddResult AddItemInstance(ItemInstance item)
        {
            EnsureSlots();
            if (item == null || item.Definition == null)
            {
                return new InventoryAddResult(1, 0);
            }

            item.EnsureInstanceId();
            if (ContainsItemInstance(item))
            {
                return new InventoryAddResult(1, 0);
            }

            ItemDefinition definition = item.Definition;
            if (definition.IsStackable)
            {
                return new InventoryAddResult(1, 0);
            }

            InventorySlot slot = new(item, 1);
            slots.Add(slot);
            RaiseSlotAdded(slot);

            return new InventoryAddResult(1, 1);
        }

        public bool TryDropItem(ItemDefinition item, int quantity)
        {
            EnsureSlots();
            if (item == null || quantity <= 0 || GetItemCount(item) < quantity)
            {
                return false;
            }

            return DropItem(item, quantity).FullyDropped;
        }

        public InventoryDropResult DropItem(ItemDefinition item, int quantity)
        {
            EnsureSlots();
            if (item == null || quantity <= 0 || GetItemCount(item) < quantity || !CanCreateDroppedItem(item))
            {
                return new InventoryDropResult(quantity, 0);
            }

            if (item.IsStackable)
            {
                return DropStackableItem(item, quantity);
            }

            return DropNonStackableItems(item, quantity);
        }

        public bool TryDropSlot(InventorySlot slot, int quantity)
        {
            EnsureSlots();
            if (slot == null || quantity <= 0 || !slots.Contains(slot) || slot.Definition == null)
            {
                return false;
            }

            if (slot.HasItemInstance)
            {
                return quantity == 1 && TryDropItemInstance(slot.Item);
            }

            if (quantity > slot.Quantity || !CanCreateDroppedItem(slot.Definition))
            {
                return false;
            }

            if (!CreateDroppedItem(slot.Definition, quantity))
            {
                return false;
            }

            if (!slot.TryRemove(quantity))
            {
                return false;
            }

            if (slot.Quantity <= 0)
            {
                slots.Remove(slot);
                RaiseSlotRemoved(slot);
            }
            else
            {
                RaiseSlotChanged(slot);
            }

            return true;
        }

        public bool TryDropItemInstance(ItemInstance item)
        {
            EnsureSlots();
            if (item == null || item.Definition == null || !CanCreateDroppedItem(item.Definition))
            {
                return false;
            }

            item.EnsureInstanceId();
            for (int i = slots.Count - 1; i >= 0; i--)
            {
                InventorySlot slot = slots[i];
                if (slot == null || slot.Item == null || slot.Item.InstanceId != item.InstanceId)
                {
                    continue;
                }

                UnequipIfNeeded(item);
                if (!CreateDroppedItem(item))
                {
                    return false;
                }

                slots.RemoveAt(i);
                RaiseSlotRemoved(slot);
                return true;
            }

            return false;
        }

        private InventoryDropResult DropStackableItem(ItemDefinition item, int quantity)
        {
            if (!CreateDroppedItem(item, quantity))
            {
                return new InventoryDropResult(quantity, 0);
            }

            int remainingQuantity = quantity;
            int droppedQuantity = 0;
            for (int i = slots.Count - 1; i >= 0 && remainingQuantity > 0; i--)
            {
                InventorySlot slot = slots[i];
                if (slot == null || slot.Definition != item)
                {
                    continue;
                }

                int slotDroppedQuantity = Mathf.Min(remainingQuantity, slot.Quantity);
                slot.TryRemove(slotDroppedQuantity);
                remainingQuantity -= slotDroppedQuantity;
                droppedQuantity += slotDroppedQuantity;

                if (slot.Quantity <= 0)
                {
                    slots.RemoveAt(i);
                    RaiseSlotRemoved(slot);
                }
                else
                {
                    RaiseSlotChanged(slot);
                }
            }

            return new InventoryDropResult(quantity, droppedQuantity);
        }

        private InventoryDropResult DropNonStackableItems(ItemDefinition item, int quantity)
        {
            int droppedQuantity = 0;
            for (int i = slots.Count - 1; i >= 0; i--)
            {
                InventorySlot slot = slots[i];
                if (slot == null || slot.Item == null || slot.Definition != item)
                {
                    continue;
                }

                UnequipIfNeeded(slot.Item);
                if (!CreateDroppedItem(slot.Item))
                {
                    break;
                }

                slots.RemoveAt(i);
                RaiseSlotRemoved(slot);
                droppedQuantity++;
                if (droppedQuantity >= quantity)
                {
                    break;
                }
            }

            return new InventoryDropResult(quantity, droppedQuantity);
        }

        public bool ContainsItemInstance(ItemInstance item)
        {
            EnsureSlots();
            if (item == null)
            {
                return false;
            }

            item.EnsureInstanceId();
            for (int i = 0; i < slots.Count; i++)
            {
                InventorySlot slot = slots[i];
                if (slot != null && slot.Item != null && slot.Item.InstanceId == item.InstanceId)
                {
                    return true;
                }
            }

            return false;
        }

        public int GetItemCount(ItemDefinition item)
        {
            EnsureSlots();
            if (item == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < slots.Count; i++)
            {
                InventorySlot slot = slots[i];
                if (slot != null && slot.Definition == item)
                {
                    count += slot.Quantity;
                }
            }

            return count;
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            NormalizeSlots();
        }

        private InventoryAddResult AddStackableItem(ItemDefinition item, int quantity)
        {
            int remainingQuantity = quantity;
            int maxStackSize = item.MaxStackSize;
            int addedQuantity = 0;

            for (int i = 0; i < slots.Count && remainingQuantity > 0; i++)
            {
                InventorySlot slot = slots[i];
                if (slot == null || slot.Definition != item || slot.Quantity >= maxStackSize)
                {
                    continue;
                }

                int slotAddedQuantity = slot.Add(remainingQuantity);
                remainingQuantity -= slotAddedQuantity;
                addedQuantity += slotAddedQuantity;

                if (slotAddedQuantity > 0)
                {
                    RaiseSlotChanged(slot);
                }
            }

            while (remainingQuantity > 0)
            {
                int slotQuantity = Mathf.Min(remainingQuantity, maxStackSize);
                InventorySlot slot = new(item, slotQuantity);
                slots.Add(slot);
                remainingQuantity -= slotQuantity;
                addedQuantity += slotQuantity;
                RaiseSlotAdded(slot);
            }

            return new InventoryAddResult(quantity, addedQuantity);
        }

        private InventoryAddResult AddNonStackableItem(ItemDefinition item, int quantity)
        {
            int addedQuantity = 0;
            for (int i = 0; i < quantity; i++)
            {
                InventorySlot slot = new(new ItemInstance(item), 1);
                slots.Add(slot);
                addedQuantity++;
                RaiseSlotAdded(slot);
            }

            return new InventoryAddResult(quantity, addedQuantity);
        }

        private void RaiseSlotAdded(InventorySlot slot)
        {
            SlotAdded?.Invoke(slot);
        }

        private void RaiseSlotChanged(InventorySlot slot)
        {
            SlotChanged?.Invoke(slot);
        }

        private void RaiseSlotRemoved(InventorySlot slot)
        {
            SlotRemoved?.Invoke(slot);
        }

        private bool CreateDroppedItem(ItemDefinition item, int quantity)
        {
            if (!TryGetDroppedItemPrefab(item, out GameObject prefab))
            {
                return false;
            }

            GameObject droppedObject = Instantiate(prefab, GetDropPosition(), Quaternion.identity);
            DroppedItem droppedItem = droppedObject.GetComponent<DroppedItem>();
            droppedItem.Initialize(item, quantity);
            return true;
        }

        private bool CreateDroppedItem(ItemInstance item)
        {
            if (item == null || !TryGetDroppedItemPrefab(item.Definition, out GameObject prefab))
            {
                return false;
            }

            GameObject droppedObject = Instantiate(prefab, GetDropPosition(), Quaternion.identity);
            DroppedItem droppedItem = droppedObject.GetComponent<DroppedItem>();
            droppedItem.Initialize(item);
            return true;
        }

        private bool CanCreateDroppedItem(ItemDefinition item)
        {
            return TryGetDroppedItemPrefab(item, out _);
        }

        private bool TryGetDroppedItemPrefab(ItemDefinition item, out GameObject prefab)
        {
            prefab = item != null ? item.DropPrefab : null;
            return prefab != null && prefab.TryGetComponent(out DroppedItem _);
        }

        private Vector3 GetDropPosition()
        {
            Vector2 offset = Random.insideUnitCircle * Mathf.Max(0f, dropScatterRadius);
            return DropPosition + (Vector3)offset;
        }

        private void EnsureSlots()
        {
            slots ??= new List<InventorySlot>();
        }

        private void EnsureEquip()
        {
            equip ??= GetComponent<EquipComponent>();
        }

        private void UnequipIfNeeded(ItemInstance item)
        {
            EnsureEquip();
            if (equip != null)
            {
                equip.Unequip(item);
            }
        }

        private void NormalizeSlots()
        {
            EnsureSlots();
            for (int i = slots.Count - 1; i >= 0; i--)
            {
                InventorySlot slot = slots[i];
                if (slot == null || slot.IsEmpty || HasEarlierItemInstance(slot.Item, i))
                {
                    slots.RemoveAt(i);
                }
            }
        }

        private bool HasEarlierItemInstance(ItemInstance item, int beforeIndex)
        {
            if (item == null)
            {
                return false;
            }

            item.EnsureInstanceId();
            for (int i = 0; i < beforeIndex; i++)
            {
                InventorySlot slot = slots[i];
                if (slot != null && slot.Item != null && slot.Item.InstanceId == item.InstanceId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
