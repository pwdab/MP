using System;
using UnityEngine;

namespace MP.Items
{
    [Serializable]
    public sealed class InventorySlot : ISerializationCallbackReceiver
    {
        [SerializeField] private ItemDefinition definition;
        [SerializeField] private ItemInstance item;
        [SerializeField, Min(1)] private int quantity = 1;

        public ItemInstance Item => item;
        public ItemDefinition Definition => item != null ? item.Definition : GetStackableDefinition();
        public int Quantity => quantity;
        public int MaxQuantity => Definition != null ? Definition.MaxStackSize : 1;
        public bool HasItemInstance => item != null;
        public bool IsEmpty => Definition == null || quantity <= 0;

        public InventorySlot(ItemDefinition definition, int quantity)
        {
            this.definition = definition != null ? definition : throw new ArgumentNullException(nameof(definition));
            if (!this.definition.IsStackable)
            {
                throw new ArgumentException("Non-stackable items must be stored as an ItemInstance.", nameof(definition));
            }

            item = null;
            this.quantity = ClampQuantity(quantity);
        }

        public InventorySlot(ItemInstance item, int quantity)
        {
            this.item = item != null ? item : throw new ArgumentNullException(nameof(item));
            this.item.EnsureInstanceId();
            definition = this.item.Definition != null ? this.item.Definition : throw new ArgumentException("ItemInstance must have an ItemDefinition.", nameof(item));

            if (definition.IsStackable)
            {
                throw new ArgumentException("Stackable items must be stored as definition-only slots.", nameof(item));
            }

            this.quantity = ClampQuantity(quantity);
        }

        internal int Add(int amount)
        {
            if (amount <= 0)
            {
                return 0;
            }

            int addedAmount = Mathf.Min(amount, MaxQuantity - quantity);
            quantity += Mathf.Max(0, addedAmount);
            return addedAmount;
        }

        internal bool TryRemove(int amount)
        {
            if (amount <= 0 || quantity < amount)
            {
                return false;
            }

            quantity -= amount;
            return true;
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            if (item != null)
            {
                item.EnsureInstanceId();
                definition = item.Definition;
            }

            if (definition != null && definition.IsStackable)
            {
                item = null;
            }

            if (item == null && definition != null && !definition.IsStackable)
            {
                definition = null;
            }

            quantity = ClampQuantity(quantity);
        }

        private int ClampQuantity(int value)
        {
            return Mathf.Clamp(value, 1, MaxQuantity);
        }

        private ItemDefinition GetStackableDefinition()
        {
            return definition != null && definition.IsStackable ? definition : null;
        }
    }
}
