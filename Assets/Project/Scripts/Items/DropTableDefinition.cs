using System;
using System.Collections.Generic;
using UnityEngine;

namespace MP.Items
{
    [CreateAssetMenu(menuName = "MP/Data/Drop Table")]
    public sealed class DropTableDefinition : ScriptableObject
    {
        [SerializeField] private DropTableEntry[] entries;
        [SerializeField, Min(0f)] private float dropScatterRadius = 0.25f;

        public IReadOnlyList<DropTableEntry> Entries => entries ?? Array.Empty<DropTableEntry>();
        public float DropScatterRadius => Mathf.Max(0f, dropScatterRadius);

        private void OnValidate()
        {
            Normalize();
            ValidateEntries();
        }

        public void Normalize()
        {
            dropScatterRadius = DropScatterRadius;
            if (entries == null)
            {
                return;
            }

            for (int i = 0; i < entries.Length; i++)
            {
                entries[i]?.Normalize();
            }
        }

        private void ValidateEntries()
        {
            if (entries == null || entries.Length == 0)
            {
                Debug.LogWarning($"{name} has no drop entries.", this);
                return;
            }

            for (int i = 0; i < entries.Length; i++)
            {
                DropTableEntry entry = entries[i];
                if (entry == null)
                {
                    Debug.LogWarning($"{name} has an empty drop entry at index {i}.", this);
                    continue;
                }

                if (entry.Item == null)
                {
                    Debug.LogWarning($"{name} has a drop entry without an item at index {i}.", this);
                }
            }
        }
    }
}
