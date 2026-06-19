using UnityEngine;

namespace MP.Items
{
    [DisallowMultipleComponent]
    public sealed class DroppedItem : MonoBehaviour, ISerializationCallbackReceiver
    {
        [SerializeField] private ItemDefinition itemDefinition;
        [SerializeField] private ItemInstance itemInstance;
        [SerializeField, Min(1)] private int quantity = 1;

        public ItemDefinition Definition => itemInstance != null ? itemInstance.Definition : itemDefinition;
        public ItemInstance Instance => itemInstance;
        public int Quantity => quantity;
        public bool HasItem => Definition != null;

        public void Initialize(ItemDefinition itemDefinition, int itemQuantity)
        {
            if (itemDefinition == null)
            {
                throw new System.ArgumentNullException(nameof(itemDefinition));
            }

            itemInstance = null;
            if (itemDefinition.IsStackable)
            {
                this.itemDefinition = itemDefinition;
                quantity = NormalizeQuantity(itemQuantity);
                return;
            }

            itemInstance = new ItemInstance(itemDefinition);
            this.itemDefinition = null;
            quantity = 1;
        }

        public void Initialize(ItemInstance instance)
        {
            if (instance == null)
            {
                throw new System.ArgumentNullException(nameof(instance));
            }

            instance.EnsureInstanceId();
            itemInstance = instance;
            itemDefinition = null;
            quantity = 1;
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            NormalizeState();
        }

        private void OnValidate()
        {
            NormalizeState();
        }

        private static int NormalizeQuantity(int value)
        {
            return Mathf.Max(1, value);
        }

        private void NormalizeState()
        {
            if (itemInstance != null)
            {
                itemInstance.EnsureInstanceId();
                itemDefinition = null;
                quantity = 1;
                return;
            }

            if (itemDefinition != null && !itemDefinition.IsStackable)
            {
                quantity = 1;
                return;
            }

            quantity = NormalizeQuantity(quantity);
        }
    }
}
