using System.Collections.Generic;
using UnityEngine;

namespace MP.Progression.SkillTree
{
    public sealed class SkillTreeComponent : MonoBehaviour
    {
        [SerializeField] private List<string> unlockedSkillIds = new();

        public IReadOnlyList<string> UnlockedSkillIds => unlockedSkillIds;

        public bool HasSkill(string skillId)
        {
            return !string.IsNullOrEmpty(skillId) && unlockedSkillIds.Contains(skillId);
        }

        public bool UnlockSkill(string skillId)
        {
            if (string.IsNullOrEmpty(skillId) || unlockedSkillIds.Contains(skillId))
            {
                return false;
            }

            unlockedSkillIds.Add(skillId);
            return true;
        }
    }
}
