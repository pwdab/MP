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
        public bool IsValid => item != null && DropChance > 0f;

        public bool ShouldDrop()
        {
            return IsValid && (DropChance >= 1f || UnityEngine.Random.value < DropChance);
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
