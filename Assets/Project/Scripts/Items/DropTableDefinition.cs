using System;
using System.Collections.Generic;
using UnityEngine;

namespace MP.Items
{
    [CreateAssetMenu(menuName = "MP/Data/Drop Table")]
    public sealed class DropTableDefinition : ScriptableObject
    {
        [Header("Drops")]
        [Tooltip("Drop entries rolled when this table is used.")]
        [SerializeField] private DropTableEntry[] entries;

        [Header("World Placement")]
        [Tooltip("Maximum random radius used when placing dropped items around the source.")]
        [SerializeField, Min(0f)] private float dropScatterRadius = 0.25f;

        public IReadOnlyList<DropTableEntry> Entries => entries ?? Array.Empty<DropTableEntry>();
        public float DropScatterRadius => Mathf.Max(0f, dropScatterRadius);

        public bool IsValid()
        {
            return IsValid(out _);
        }

        public bool IsValid(out string reason)
        {
            if (float.IsNaN(dropScatterRadius) || float.IsInfinity(dropScatterRadius) || dropScatterRadius < 0f)
            {
                reason = $"{name} has invalid drop scatter radius '{dropScatterRadius}'.";
                return false;
            }

            if (entries == null || entries.Length == 0)
            {
                reason = $"{name} has no drop entries.";
                return false;
            }

            for (int i = 0; i < entries.Length; i++)
            {
                DropTableEntry entry = entries[i];
                if (entry == null)
                {
                    reason = $"{name} has an empty drop entry at index {i}.";
                    return false;
                }

                if (!entry.IsValid(out string entryReason))
                {
                    reason = $"{name} drop entry {i} is invalid: {entryReason}";
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

        private void OnValidate()
        {
            Normalize();
            LogValidationWarnings();
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

        private void LogValidationWarnings()
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

                if (!entry.IsValid(out string reason))
                {
                    Debug.LogWarning($"{name} has an invalid drop entry at index {i}: {reason}", this);
                }
            }
        }
    }
}
