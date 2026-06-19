using System;

namespace MP.Items
{
    [Serializable]
    public readonly struct InventoryAddResult
    {
        public int RequestedQuantity { get; }
        public int AddedQuantity { get; }
        public bool FullyAdded => RequestedQuantity > 0 && AddedQuantity >= RequestedQuantity;

        public InventoryAddResult(int requestedQuantity, int addedQuantity)
        {
            RequestedQuantity = Math.Max(0, requestedQuantity);
            AddedQuantity = Math.Min(RequestedQuantity, Math.Max(0, addedQuantity));
        }
    }

    [Serializable]
    public readonly struct InventoryDropResult
    {
        public int RequestedQuantity { get; }
        public int DroppedQuantity { get; }
        public bool FullyDropped => RequestedQuantity > 0 && DroppedQuantity >= RequestedQuantity;

        public InventoryDropResult(int requestedQuantity, int droppedQuantity)
        {
            RequestedQuantity = Math.Max(0, requestedQuantity);
            DroppedQuantity = Math.Min(RequestedQuantity, Math.Max(0, droppedQuantity));
        }
    }
}
