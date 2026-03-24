using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using ZombieIslandVR.Player;

namespace ZombieIslandVR.Systems
{
    /// <summary>
    /// Manages crafting recipes and drives PlayerInventory.Craft().
    /// Attach to the same GameObject as PlayerInventory.
    /// </summary>
    public class CraftingSystem : MonoBehaviour
    {
        [Header("Recipes")]
        public CraftingRecipe[] recipes;

        [Header("Events")]
        public UnityEvent<CraftingRecipe> onCraftSuccess;
        public UnityEvent<string> onCraftFail;   // reason string

        private PlayerInventory _inventory;

        private void Awake()
        {
            _inventory = GetComponent<PlayerInventory>();
        }

        // ─── Public API ───────────────────────────────────────────────────────

        /// <summary>Attempt to craft recipe at given index. Returns true on success.</summary>
        public bool TryCraft(int recipeIndex)
        {
            if (recipeIndex < 0 || recipeIndex >= recipes.Length)
            {
                onCraftFail?.Invoke("Receita inválida.");
                return false;
            }
            return TryCraft(recipes[recipeIndex]);
        }

        /// <summary>Attempt to craft a specific recipe. Returns true on success.</summary>
        public bool TryCraft(CraftingRecipe recipe)
        {
            if (recipe == null)
            {
                onCraftFail?.Invoke("Receita nula.");
                return false;
            }

            // Validate all ingredients
            foreach (var req in recipe.ingredients)
            {
                if (!_inventory.HasItem(req.itemId, req.quantity))
                {
                    string reason = $"Faltando: {req.quantity}x {req.itemId}";
                    Debug.Log($"[CraftingSystem] {reason}");
                    onCraftFail?.Invoke(reason);
                    return false;
                }
            }

            bool result = _inventory.Craft(recipe);
            if (result)
            {
                Debug.Log($"[CraftingSystem] Crafted: {recipe.recipeName}");
                onCraftSuccess?.Invoke(recipe);
            }
            else
            {
                onCraftFail?.Invoke("Inventário cheio ou sobrepeso.");
            }
            return result;
        }

        /// <summary>Returns list of recipe indices that can currently be crafted.</summary>
        public List<int> GetAvailableRecipes()
        {
            var available = new List<int>();
            for (int i = 0; i < recipes.Length; i++)
            {
                bool canCraft = true;
                foreach (var req in recipes[i].ingredients)
                {
                    if (!_inventory.HasItem(req.itemId, req.quantity))
                    {
                        canCraft = false;
                        break;
                    }
                }
                if (canCraft) available.Add(i);
            }
            return available;
        }
    }
}
