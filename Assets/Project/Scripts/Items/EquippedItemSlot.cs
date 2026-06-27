using System;
using UnityEngine;

namespace MP.Items
{
    [Serializable]
    public sealed class EquippedItemSlot : ISerializationCallbackReceiver
    {
        [SerializeField] private EquipSlotId slotId;
        [SerializeField] private ItemInstance item;

        public EquipSlotId SlotId => slotId;
        public ItemInstance Item => item;
        public bool HasItem => item != null;

        public bool IsValid()
        {
            return IsValid(out _);
        }

        public bool IsValid(out string reason)
        {
            if (slotId == EquipSlotId.None)
            {
                reason = "EquippedItemSlot cannot use None.";
                return false;
            }

            if (item != null && !CanAccept(item))
            {
                reason = $"EquippedItemSlot '{slotId}' cannot accept the current item.";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        public void ValidateOrThrow()
        {
            if (!IsValid(out string reason))
            {
                throw new InvalidOperationException(reason);
            }
        }

        public EquippedItemSlot(EquipSlotId slotId)
        {
            if (slotId == EquipSlotId.None)
            {
                throw new ArgumentException("Equipped item slot cannot use None.", nameof(slotId));
            }

            this.slotId = slotId;
        }

        public bool CanAccept(ItemInstance candidate)
        {
            return candidate != null
                && candidate.Definition != null
                && candidate.Definition.CanEquip
                && candidate.Definition.EquipSlot == slotId;
        }

        internal bool TrySetItem(ItemInstance candidate)
        {
            if (!CanAccept(candidate))
            {
                return false;
            }

            candidate.EnsureInstanceId();
            item = candidate;
            return true;
        }

        internal void Clear()
        {
            item = null;
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            if (item == null)
            {
                return;
            }

            if (!CanAccept(item))
            {
                item = null;
                return;
            }

            item.EnsureInstanceId();
        }
    }
}
