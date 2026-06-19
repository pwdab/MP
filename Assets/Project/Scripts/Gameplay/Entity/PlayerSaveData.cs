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
    }

    [Serializable]
    public sealed class SavedInventorySlot
    {
        public string itemId;
        public int quantity;

        public SavedInventorySlot(string itemId, int quantity)
        {
            this.itemId = itemId;
            this.quantity = quantity;
        }
    }
}
