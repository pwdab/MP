using System;
using System.Collections.Generic;

namespace MP.Gameplay.Entity
{
    [Serializable]
    public sealed class PlayerSaveData
    {
        public string jobId;
        public int level;
        public int experience;
        public int remainingGrowthPoints;
        public List<string> unlockedSkillIds = new();
        public List<SavedInventorySlot> items = new();

        public bool IsValid()
        {
            return IsValid(out _);
        }

        public bool IsValid(out string reason)
        {
            if (level < 1)
            {
                reason = $"PlayerSaveData has invalid level '{level}'.";
                return false;
            }

            if (experience < 0)
            {
                reason = $"PlayerSaveData has invalid experience '{experience}'.";
                return false;
            }

            if (remainingGrowthPoints < 0)
            {
                reason = $"PlayerSaveData has invalid remaining growth points '{remainingGrowthPoints}'.";
                return false;
            }

            if (unlockedSkillIds == null)
            {
                reason = "PlayerSaveData unlocked skill list is missing.";
                return false;
            }

            if (items == null)
            {
                reason = "PlayerSaveData item list is missing.";
                return false;
            }

            for (int i = 0; i < items.Count; i++)
            {
                SavedInventorySlot item = items[i];
                if (item == null)
                {
                    reason = $"PlayerSaveData has an empty item slot at index {i}.";
                    return false;
                }

                if (!item.IsValid(out string itemReason))
                {
                    reason = $"PlayerSaveData item slot {i} is invalid: {itemReason}";
                    return false;
                }
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
    }

    [Serializable]
    public sealed class SavedInventorySlot
    {
        public string itemId;
        public int quantity;

        public SavedInventorySlot(string itemId, int quantity)
        {
            this.itemId = itemId ?? string.Empty;
            this.quantity = Math.Max(1, quantity);
        }

        public bool IsValid()
        {
            return IsValid(out _);
        }

        public bool IsValid(out string reason)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                reason = "SavedInventorySlot item id is missing.";
                return false;
            }

            if (quantity < 1)
            {
                reason = $"SavedInventorySlot has invalid quantity '{quantity}'.";
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
    }
}
