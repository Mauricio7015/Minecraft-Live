using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ZombieIslandVR.Player
{
    [System.Serializable]
    public class InventoryItem
    {
        public string itemId;
        public string displayName;
        public Sprite icon;
        public int quantity;
        public float weight;          // kg
        public ItemCategory category;
        public GameObject prefab;

        public InventoryItem(string id, string name, int qty, float wt, ItemCategory cat)
        {
            itemId = id; displayName = name; quantity = qty; weight = wt; category = cat;
        }
    }

    public enum ItemCategory { Food, Water, Weapon, Ammo, Medical, Tool, Material, Misc }

    /// <summary>
    /// Manages player backpack and belt slots.
    /// Backpack: 20 slots, max 30 kg.
    /// Belt: 4 quick-access slots.
    /// </summary>
    public class PlayerInventory : MonoBehaviour
    {
        [Header("Capacity")]
        public int backpackSlots = 20;
        public float maxWeightKg = 30f;
        public int beltSlots = 4;

        private List<InventoryItem> _backpack = new List<InventoryItem>();
        private InventoryItem[] _belt;

        public UnityEvent<List<InventoryItem>> onInventoryChanged;
        public UnityEvent<float> onWeightChanged;   // current/max

        // ─── Properties ───────────────────────────────────────────────────────
        public float CurrentWeight
        {
            get
            {
                float w = 0f;
                foreach (var item in _backpack) w += item.weight * item.quantity;
                return w;
            }
        }

        public bool IsOverloaded => CurrentWeight > maxWeightKg;
        public IReadOnlyList<InventoryItem> Backpack => _backpack.AsReadOnly();
        public InventoryItem[] Belt => _belt;

        private void Awake()
        {
            _belt = new InventoryItem[beltSlots];
        }

        // ─── Public API ───────────────────────────────────────────────────────

        /// <summary>Returns true if item was added (or stacked).</summary>
        public bool AddItem(InventoryItem item)
        {
            if (_backpack.Count >= backpackSlots && !CanStack(item))
            {
                Debug.LogWarning("[Inventory] Backpack full.");
                return false;
            }

            if (CurrentWeight + item.weight * item.quantity > maxWeightKg)
            {
                Debug.LogWarning("[Inventory] Overweight — item rejected.");
                return false;
            }

            var existing = _backpack.Find(i => i.itemId == item.itemId);
            if (existing != null)
                existing.quantity += item.quantity;
            else
                _backpack.Add(item);

            NotifyChanged();
            return true;
        }

        public bool RemoveItem(string itemId, int quantity = 1)
        {
            var item = _backpack.Find(i => i.itemId == itemId);
            if (item == null || item.quantity < quantity) return false;

            item.quantity -= quantity;
            if (item.quantity <= 0) _backpack.Remove(item);
            NotifyChanged();
            return true;
        }

        public bool HasItem(string itemId, int quantity = 1)
        {
            var item = _backpack.Find(i => i.itemId == itemId);
            return item != null && item.quantity >= quantity;
        }

        /// <summary>Assign an item from backpack to a belt slot (0–3).</summary>
        public void AssignToBelt(string itemId, int slot)
        {
            if (slot < 0 || slot >= beltSlots) return;
            _belt[slot] = _backpack.Find(i => i.itemId == itemId);
        }

        public InventoryItem GetBeltItem(int slot)
        {
            if (slot < 0 || slot >= beltSlots) return null;
            return _belt[slot];
        }

        // ─── Crafting Helpers ─────────────────────────────────────────────────

        /// <summary>Check and consume all ingredients, then add result.</summary>
        public bool Craft(CraftingRecipe recipe)
        {
            foreach (var req in recipe.ingredients)
            {
                if (!HasItem(req.itemId, req.quantity))
                {
                    Debug.Log($"[Inventory] Missing {req.quantity}x {req.itemId}");
                    return false;
                }
            }
            foreach (var req in recipe.ingredients)
                RemoveItem(req.itemId, req.quantity);

            AddItem(new InventoryItem(recipe.result.itemId, recipe.result.displayName,
                recipe.result.quantity, recipe.result.weight, recipe.result.category));
            return true;
        }

        // ─── Private ──────────────────────────────────────────────────────────

        private bool CanStack(InventoryItem item)
        {
            return _backpack.Exists(i => i.itemId == item.itemId);
        }

        private void NotifyChanged()
        {
            onInventoryChanged?.Invoke(_backpack);
            onWeightChanged?.Invoke(CurrentWeight / maxWeightKg);
        }
    }

    // ─── Crafting Data ────────────────────────────────────────────────────────

    [System.Serializable]
    public class CraftingIngredient
    {
        public string itemId;
        public int quantity;
    }

    [CreateAssetMenu(fileName = "Recipe_New", menuName = "ZombieIsland/Crafting Recipe")]
    public class CraftingRecipe : ScriptableObject
    {
        public string recipeName;
        public CraftingIngredient[] ingredients;
        public InventoryItem result;
    }
}
