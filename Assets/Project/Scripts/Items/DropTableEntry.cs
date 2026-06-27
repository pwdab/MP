using System;
using UnityEngine;

namespace MP.Items
{
    [Serializable]
    public sealed class DropTableEntry
    {
        [Tooltip("Item that can be dropped.")]
        [SerializeField] private ItemDefinition item;

        [Tooltip("Chance to drop this item. 1 means 100%.")]
        [SerializeField, Range(0f, 1f)] private float dropChance = 1f;

        [Tooltip("Minimum dropped quantity.")]
        [SerializeField, Min(1)] private int minQuantity = 1;

        [Tooltip("Maximum dropped quantity.")]
        [SerializeField, Min(1)] private int maxQuantity = 1;

        public ItemDefinition Item => item;
        public float DropChance => Mathf.Clamp01(dropChance);
        public int MinQuantity => Mathf.Max(1, minQuantity);
        public int MaxQuantity => Mathf.Max(MinQuantity, maxQuantity);
        public bool IsValid()
        {
            return IsValid(out _);
        }

        public bool IsValid(out string reason)
        {
            if (item == null)
            {
                reason = "DropTableEntry item is missing.";
                return false;
            }

            if (float.IsNaN(dropChance) || float.IsInfinity(dropChance) || dropChance < 0f || dropChance > 1f)
            {
                reason = $"DropTableEntry has invalid drop chance '{dropChance}'.";
                return false;
            }

            if (minQuantity < 1)
            {
                reason = $"DropTableEntry has invalid minimum quantity '{minQuantity}'.";
                return false;
            }

            if (maxQuantity < minQuantity)
            {
                reason = $"DropTableEntry max quantity '{maxQuantity}' is smaller than min quantity '{minQuantity}'.";
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

        public bool ShouldDrop()
        {
            return IsValid() && DropChance > 0f && (DropChance >= 1f || UnityEngine.Random.value < DropChance);
        }

        public int RollQuantity()
        {
            if (item == null)
            {
                return 0;
            }

            return UnityEngine.Random.Range(MinQuantity, MaxQuantity + 1);
        }

        public void Normalize()
        {
            dropChance = DropChance;
            minQuantity = MinQuantity;
            maxQuantity = MaxQuantity;
        }
    }
}
