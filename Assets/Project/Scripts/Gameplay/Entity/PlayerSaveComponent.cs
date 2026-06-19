using MP.Items;
using MP.Progression.Jobs;
using MP.Progression.Level;
using MP.Progression.SkillTree;
using UnityEngine;

namespace MP.Gameplay.Entity
{
    public sealed class PlayerSaveComponent : MonoBehaviour
    {
        private PlayerJobComponent job;
        private PlayerProgressionComponent progression;
        private SkillTreeComponent skillTree;
        private InventoryComponent inventory;

        private void Awake()
        {
            job = GetComponent<PlayerJobComponent>();
            progression = GetComponent<PlayerProgressionComponent>();
            skillTree = GetComponent<SkillTreeComponent>();
            inventory = GetComponent<InventoryComponent>();
        }

        public PlayerSaveData CreateSaveData()
        {
            var saveData = new PlayerSaveData
            {
                jobId = job != null ? job.CurrentJobId : string.Empty,
                level = progression != null ? progression.Level : 1,
                experience = progression != null ? progression.Experience : 0,
                remainingGrowthPoints = progression != null ? progression.RemainingGrowthPoints : 0
            };

            if (skillTree != null)
            {
                for (int i = 0; i < skillTree.UnlockedSkillIds.Count; i++)
                {
                    saveData.unlockedSkillIds.Add(skillTree.UnlockedSkillIds[i]);
                }
            }

            if (inventory != null)
            {
                for (int i = 0; i < inventory.Slots.Count; i++)
                {
                    InventorySlot slot = inventory.Slots[i];
                    if (slot == null || slot.Definition == null)
                    {
                        continue;
                    }

                    string itemId = slot.Definition.ItemId;
                    saveData.items.Add(new SavedInventorySlot(itemId, slot.Quantity));
                }
            }

            return saveData;
        }

        public string CreateSaveJson()
        {
            return JsonUtility.ToJson(CreateSaveData(), true);
        }
    }
}
