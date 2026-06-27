using System.Collections.Generic;
using MP.Gameplay.Stats;
using UnityEngine;

namespace MP.Items
{
    public sealed class ItemSystemTestRunner : MonoBehaviour
    {
        [SerializeField] private InventoryComponent inventory;
        [SerializeField] private EquipComponent equip;
        [SerializeField] private StatsComponent stats;
        [SerializeField] private ItemDefinition potion;
        [SerializeField] private ItemDefinition sword;
        [SerializeField] private ItemDefinition armor;
        [SerializeField] private GameObject droppedItemPrefab;

        private readonly List<string> results = new();
        private bool passed;

        private void Start()
        {
            RunTests();
        }

        private void OnGUI()
        {
            const int width = 720;
            GUILayout.BeginArea(new Rect(16, 16, width, Screen.height - 32), GUI.skin.box);
            GUILayout.Label(passed ? "Item System Test: PASS" : "Item System Test: FAIL");
            GUILayout.Label($"Results: {CountPassedResults()} / {results.Count}");

            int startIndex = Mathf.Max(0, results.Count - 8);
            for (int i = startIndex; i < results.Count; i++)
            {
                GUILayout.Label(results[i]);
            }

            if (startIndex > 0)
            {
                GUILayout.Label($"... {startIndex} earlier results were written to the Console.");
            }

            GUILayout.EndArea();
        }

        private void RunTests()
        {
            results.Clear();
            passed = true;

            TestRequiredReferences();
            if (!passed)
            {
                return;
            }

            TestStackableInventory();
            TestNonStackableInventory();
            TestEquipSlotsAndStats();
            TestDroppedItemInitialization();
        }

        private void TestRequiredReferences()
        {
            Expect(inventory != null, "InventoryComponent reference exists.");
            Expect(equip != null, "EquipComponent reference exists.");
            Expect(stats != null, "StatsComponent reference exists.");
            Expect(potion != null, "Potion ItemDefinition reference exists.");
            Expect(sword != null, "Sword ItemDefinition reference exists.");
            Expect(armor != null, "Armor ItemDefinition reference exists.");
            Expect(droppedItemPrefab != null, "Dropped item prefab reference exists.");
        }

        private void TestStackableInventory()
        {
            InventoryAddResult addResult = inventory.AddItem(potion, potion.MaxStackSize + 2);
            Expect(addResult.FullyAdded, "Stackable potion add fully succeeds.");
            Expect(inventory.GetItemCount(potion) == potion.MaxStackSize + 2, "Potion count matches added quantity.");
            Expect(CountSlots(potion) == 2, "Potion splits into multiple slots when max stack is exceeded.");

            int droppedPotionCount = CountDroppedItems(potion);
            InventoryDropResult dropResult = inventory.DropItem(potion, 2);
            Expect(dropResult.FullyDropped, "Partial potion drop request fully drops requested quantity.");
            Expect(inventory.GetItemCount(potion) == potion.MaxStackSize, "Potion count updates after drop.");
            Expect(CountDroppedItems(potion) == droppedPotionCount + 1, "Dropping stackable potion creates one DroppedItem.");
            Expect(FindDroppedItem(potion, 2) != null, "Dropped potion stores removed quantity.");
        }

        private void TestNonStackableInventory()
        {
            InventoryAddResult addResult = inventory.AddItem(sword, 2);
            Expect(addResult.FullyAdded, "Non-stackable sword add fully succeeds.");
            Expect(inventory.GetItemCount(sword) == 2, "Sword count tracks non-stackable slots.");
            Expect(CountSlots(sword) == 2, "Non-stackable swords create separate slots.");
            InventorySlot swordSlot = FindFirstSlot(sword);
            Expect(swordSlot != null && swordSlot.HasItemInstance, "Non-stackable sword slot has ItemInstance.");
        }

        private void TestEquipSlotsAndStats()
        {
            float baseAttackPower = stats.GetValue(StatId.AttackPower);
            float baseMaxHealth = stats.GetValue(StatId.MaxHealth);

            InventorySlot swordSlot = FindFirstSlot(sword);
            if (swordSlot == null || !swordSlot.HasItemInstance)
            {
                Expect(false, "Sword slot exists before equip test.");
                return;
            }

            ItemInstance swordInstance = swordSlot.Item;
            ItemInstance armorInstance = new(armor);
            InventoryAddResult armorAddResult = inventory.AddItemInstance(armorInstance);

            Expect(armorAddResult.FullyAdded, "Armor ItemInstance add succeeds.");
            Expect(equip.TryEquip(swordInstance), "Sword equips.");
            Expect(equip.TryEquip(armorInstance), "Armor equips.");
            Expect(equip.GetEquippedItem(EquipSlotId.Weapon) == swordInstance, "Sword is equipped in Weapon slot.");
            Expect(equip.GetEquippedItem(EquipSlotId.Armor) == armorInstance, "Armor is equipped in Armor slot.");
            Expect(stats.GetValue(StatId.AttackPower) > baseAttackPower, "Sword modifier increases AttackPower.");
            Expect(stats.GetValue(StatId.MaxHealth) > baseMaxHealth, "Armor modifier increases MaxHealth.");

            Expect(equip.Unequip(EquipSlotId.Weapon), "Weapon slot unequips.");
            Expect(Mathf.Approximately(stats.GetValue(StatId.AttackPower), baseAttackPower), "AttackPower returns to base after weapon unequip.");
            Expect(inventory.TryDropItemInstance(armorInstance), "Dropping equipped armor ItemInstance creates a world item.");
            Expect(equip.GetEquippedItem(EquipSlotId.Armor) == null, "Dropping equipped armor unequips Armor slot.");
            Expect(Mathf.Approximately(stats.GetValue(StatId.MaxHealth), baseMaxHealth), "MaxHealth returns to base after equipped armor drop.");
            DroppedItem droppedArmor = FindDroppedItem(armor, 1);
            Expect(droppedArmor != null && droppedArmor.Instance == armorInstance, "Dropped armor preserves ItemInstance identity.");
        }

        private void TestDroppedItemInitialization()
        {
            GameObject droppedObject = Instantiate(droppedItemPrefab, new Vector3(2f, 0f, 0f), Quaternion.identity);
            bool hasDroppedItem = droppedObject.TryGetComponent(out DroppedItem droppedItem);
            Expect(hasDroppedItem, "Dropped item prefab has DroppedItem component.");

            if (hasDroppedItem)
            {
                droppedItem.Initialize(potion, 3);
                Expect(droppedItem.Definition == potion, "DroppedItem stores initialized item definition.");
                Expect(droppedItem.Quantity == 3, "DroppedItem stores initialized quantity.");
            }
        }

        private InventorySlot FindFirstSlot(ItemDefinition item)
        {
            for (int i = 0; i < inventory.Slots.Count; i++)
            {
                InventorySlot slot = inventory.Slots[i];
                if (slot != null && slot.Definition == item)
                {
                    return slot;
                }
            }

            return null;
        }

        private int CountSlots(ItemDefinition item)
        {
            int count = 0;
            for (int i = 0; i < inventory.Slots.Count; i++)
            {
                InventorySlot slot = inventory.Slots[i];
                if (slot != null && slot.Definition == item)
                {
                    count++;
                }
            }

            return count;
        }

        private int CountDroppedItems(ItemDefinition item)
        {
            int count = 0;
            DroppedItem[] droppedItems = FindObjectsByType<DroppedItem>(FindObjectsSortMode.None);
            for (int i = 0; i < droppedItems.Length; i++)
            {
                if (droppedItems[i].Definition == item)
                {
                    count++;
                }
            }

            return count;
        }

        private DroppedItem FindDroppedItem(ItemDefinition item, int quantity)
        {
            DroppedItem[] droppedItems = FindObjectsByType<DroppedItem>(FindObjectsSortMode.None);
            for (int i = 0; i < droppedItems.Length; i++)
            {
                DroppedItem droppedItem = droppedItems[i];
                if (droppedItem.Definition == item && droppedItem.Quantity == quantity)
                {
                    return droppedItem;
                }
            }

            return null;
        }

        private void Expect(bool condition, string message)
        {
            if (condition)
            {
                string result = "[PASS] " + message;
                results.Add(result);
                Debug.Log(result, this);
                return;
            }

            passed = false;
            string failedResult = "[FAIL] " + message;
            results.Add(failedResult);
            Debug.LogError(failedResult, this);
        }

        private int CountPassedResults()
        {
            int count = 0;
            for (int i = 0; i < results.Count; i++)
            {
                if (results[i].StartsWith("[PASS]"))
                {
                    count++;
                }
            }

            return count;
        }
    }
}
