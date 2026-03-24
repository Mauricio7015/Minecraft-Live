using NUnit.Framework;
using UnityEngine;
using ZombieIslandVR.Player;

namespace ZombieIslandVR.Tests
{
    /// <summary>
    /// Play Mode tests for PlayerInventory: weight limits, stacking, and crafting.
    /// Run via: Window ▸ General ▸ Test Runner ▸ PlayMode
    /// </summary>
    public class InventoryTests
    {
        private GameObject _go;
        private PlayerInventory _inventory;

        private static InventoryItem MakeItem(string id, float weight = 1f, int qty = 1)
            => new InventoryItem(id, id, qty, weight, ItemCategory.Misc);

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("TestInventory");
            _inventory = _go.AddComponent<PlayerInventory>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
        }

        // ─── Basic Add/Remove ─────────────────────────────────────────────────

        [Test]
        public void AddItem_StoresItemInBackpack()
        {
            bool result = _inventory.AddItem(MakeItem("pistol", 1f));
            Assert.IsTrue(result, "AddItem should return true for valid item.");
            Assert.IsTrue(_inventory.HasItem("pistol"), "Backpack should contain the added item.");
        }

        [Test]
        public void RemoveItem_RemovesFromBackpack()
        {
            _inventory.AddItem(MakeItem("herb", 0.1f));
            bool removed = _inventory.RemoveItem("herb", 1);
            Assert.IsTrue(removed, "RemoveItem should succeed.");
            Assert.IsFalse(_inventory.HasItem("herb"), "Herb should be gone after removal.");
        }

        [Test]
        public void AddItem_StacksIdenticalItems()
        {
            _inventory.AddItem(MakeItem("ammo", 0.2f, 15));
            _inventory.AddItem(MakeItem("ammo", 0.2f, 10));
            var item = System.Linq.Enumerable.FirstOrDefault(
                _inventory.Backpack, i => i.itemId == "ammo");
            Assert.IsNotNull(item);
            Assert.AreEqual(25, item.quantity, "Identical items should stack.");
        }

        // ─── Weight System ────────────────────────────────────────────────────

        [Test]
        public void AddItem_ReturnsFalse_WhenOverweight()
        {
            // maxWeightKg is 30 by default
            var heavyItem = MakeItem("boulder", 31f);
            bool result = _inventory.AddItem(heavyItem);
            Assert.IsFalse(result, "Should reject item that exceeds maxWeightKg.");
            Assert.IsFalse(_inventory.HasItem("boulder"), "Overweight item should not be in inventory.");
        }

        [Test]
        public void CurrentWeight_AccumulatesCorrectly()
        {
            _inventory.AddItem(MakeItem("gun", 2f));
            _inventory.AddItem(MakeItem("food", 0.5f));
            Assert.AreEqual(2.5f, _inventory.CurrentWeight, 0.01f,
                "CurrentWeight should be sum of all item weights.");
        }

        [Test]
        public void IsOverloaded_TrueWhenWeightExceedsMax()
        {
            _inventory.maxWeightKg = 5f;
            _inventory.AddItem(MakeItem("item_a", 3f));
            _inventory.AddItem(MakeItem("item_b", 2f)); // total 5 — exactly at limit
            Assert.IsFalse(_inventory.IsOverloaded, "At exactly maxWeight should not be overloaded.");
        }

        // ─── Crafting ─────────────────────────────────────────────────────────

        [Test]
        public void Craft_ConsumesIngredientsAndAddsResult()
        {
            _inventory.AddItem(MakeItem("cloth", 0.2f, 2));
            _inventory.AddItem(MakeItem("alcohol", 0.3f, 1));

            var recipe = ScriptableObject.CreateInstance<CraftingRecipe>();
            recipe.recipeName = "Bandage";
            recipe.ingredients = new[]
            {
                new CraftingIngredient { itemId = "cloth",   quantity = 2 },
                new CraftingIngredient { itemId = "alcohol", quantity = 1 },
            };
            recipe.result = MakeItem("bandage", 0.1f, 1);

            bool crafted = _inventory.Craft(recipe);

            Assert.IsTrue(crafted, "Crafting should succeed with correct ingredients.");
            Assert.IsFalse(_inventory.HasItem("cloth"), "Cloth ingredient should be consumed.");
            Assert.IsFalse(_inventory.HasItem("alcohol"), "Alcohol ingredient should be consumed.");
            Assert.IsTrue(_inventory.HasItem("bandage"), "Result item should be in inventory.");

            Object.DestroyImmediate(recipe);
        }

        [Test]
        public void Craft_ReturnsFalse_WhenMissingIngredient()
        {
            _inventory.AddItem(MakeItem("cloth", 0.2f, 1)); // need 2

            var recipe = ScriptableObject.CreateInstance<CraftingRecipe>();
            recipe.recipeName = "Bandage";
            recipe.ingredients = new[]
            {
                new CraftingIngredient { itemId = "cloth", quantity = 2 },
            };
            recipe.result = MakeItem("bandage", 0.1f, 1);

            bool crafted = _inventory.Craft(recipe);
            Assert.IsFalse(crafted, "Crafting should fail when ingredients are insufficient.");
            Assert.IsTrue(_inventory.HasItem("cloth"), "Cloth should remain if craft failed.");

            Object.DestroyImmediate(recipe);
        }

        // ─── Belt ─────────────────────────────────────────────────────────────

        [Test]
        public void AssignToBelt_PutsItemInSlot()
        {
            _inventory.AddItem(MakeItem("pistol", 1f));
            _inventory.AssignToBelt("pistol", 0);
            var beltItem = _inventory.GetBeltItem(0);
            Assert.IsNotNull(beltItem, "Belt slot 0 should contain the pistol.");
            Assert.AreEqual("pistol", beltItem.itemId);
        }
    }
}
