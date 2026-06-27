using System;
using UnityEngine;

namespace MP.Items
{
    [Serializable]
    public sealed class ItemInstance : ISerializationCallbackReceiver
    {
        [SerializeField] private ItemDefinition definition;
        [SerializeField] private string instanceId;

        public ItemDefinition Definition => definition;
        public string InstanceId => instanceId;

        public ItemInstance(ItemDefinition definition)
        {
            this.definition = ValidateDefinition(definition);
            EnsureInstanceId();
        }

        public ItemInstance(ItemDefinition definition, string instanceId)
        {
            this.definition = ValidateDefinition(definition);
            this.instanceId = instanceId;
            EnsureInstanceId();
        }

        public void EnsureInstanceId()
        {
            if (string.IsNullOrWhiteSpace(instanceId))
            {
                instanceId = Guid.NewGuid().ToString("N");
            }
        }

        public bool IsValid()
        {
            return IsValid(out _);
        }

        public bool IsValid(out string reason)
        {
            if (definition == null)
            {
                reason = "ItemInstance definition is missing.";
                return false;
            }

            if (definition.IsStackable)
            {
                reason = "ItemInstance cannot reference a stackable item definition.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(instanceId))
            {
                reason = "ItemInstance id is missing.";
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

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            EnsureInstanceId();
        }

        private static ItemDefinition ValidateDefinition(ItemDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (definition.IsStackable)
            {
                throw new ArgumentException("Stackable items do not use ItemInstance.", nameof(definition));
            }

            return definition;
        }
    }
}
