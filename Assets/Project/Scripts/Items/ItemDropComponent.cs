using MP.Gameplay.Entity;
using UnityEngine;

namespace MP.Items
{
    [RequireComponent(typeof(HealthComponent))]
    public sealed class ItemDropComponent : MonoBehaviour
    {
        [SerializeField] private DropTableDefinition dropTable;

        private HealthComponent health;

        private void Awake()
        {
            health = GetComponent<HealthComponent>();
        }

        private void OnValidate()
        {
            ValidateDropTable();
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Died += OnDied;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= OnDied;
            }
        }

        private void OnDied(HealthComponent _)
        {
            TryDropItems();
        }

        private void TryDropItems()
        {
            if (dropTable == null)
            {
                return;
            }

            for (int i = 0; i < dropTable.Entries.Count; i++)
            {
                DropTableEntry entry = dropTable.Entries[i];
                if (entry == null || !entry.ShouldDrop())
                {
                    continue;
                }

                SpawnDroppedItem(entry.Item, entry.RollQuantity());
            }
        }

        private void SpawnDroppedItem(ItemDefinition item, int quantity)
        {
            if (!TryGetDroppedItemPrefab(item, quantity, out GameObject prefab))
            {
                return;
            }

            if (!item.IsStackable)
            {
                for (int i = 0; i < quantity; i++)
                {
                    SpawnDroppedItemInstance(item, prefab);
                }

                return;
            }

            SpawnDroppedItemStack(item, quantity, prefab);
        }

        private void SpawnDroppedItemStack(ItemDefinition item, int quantity, GameObject prefab)
        {
            Vector2 offset = Random.insideUnitCircle * dropTable.DropScatterRadius;
            GameObject droppedObject = Instantiate(prefab, transform.position + (Vector3)offset, Quaternion.identity);
            DroppedItem droppedItem = droppedObject.GetComponent<DroppedItem>();
            droppedItem.Initialize(item, quantity);
        }

        private void SpawnDroppedItemInstance(ItemDefinition item, GameObject prefab)
        {
            Vector2 offset = Random.insideUnitCircle * dropTable.DropScatterRadius;
            GameObject droppedObject = Instantiate(prefab, transform.position + (Vector3)offset, Quaternion.identity);
            DroppedItem droppedItem = droppedObject.GetComponent<DroppedItem>();
            droppedItem.Initialize(new ItemInstance(item));
        }

        private bool TryGetDroppedItemPrefab(ItemDefinition item, int quantity, out GameObject prefab)
        {
            prefab = item != null ? item.DropPrefab : null;
            if (item == null || quantity <= 0 || prefab == null)
            {
                return false;
            }

            if (!prefab.TryGetComponent(out DroppedItem _))
            {
                Debug.LogWarning($"{name} tried to drop {item.name}, but its drop prefab has no DroppedItem component.", this);
                return false;
            }

            return true;
        }

        private void ValidateDropTable()
        {
            if (dropTable == null)
            {
                return;
            }

            for (int i = 0; i < dropTable.Entries.Count; i++)
            {
                DropTableEntry entry = dropTable.Entries[i];
                if (entry == null || entry.Item == null)
                {
                    continue;
                }

                GameObject prefab = entry.Item.DropPrefab;
                if (prefab == null)
                {
                    Debug.LogWarning($"{name} drop table entry '{entry.Item.name}' has no drop prefab.", this);
                    continue;
                }

                if (!prefab.TryGetComponent(out DroppedItem _))
                {
                    Debug.LogWarning($"{name} drop table entry '{entry.Item.name}' uses a prefab without DroppedItem.", this);
                }
            }
        }
    }
}
