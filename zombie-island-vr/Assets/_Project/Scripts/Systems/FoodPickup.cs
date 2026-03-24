using UnityEngine;

namespace ZombieIslandVR.Systems
{
    /// <summary>
    /// MonoBehaviour wrapper attached to food item GameObjects in the world.
    /// FoodSystem.GetHeldFoodItemId() uses GetComponent<FoodPickup>() to detect
    /// which food the player is holding near their mouth.
    /// Set itemId to match one of the IDs in FoodSystem.FoodItems[].
    /// </summary>
    public class FoodPickup : MonoBehaviour
    {
        [Tooltip("Must match an ID in FoodSystem.FoodItems (e.g. 'canned_food', 'water_bottle')")]
        public string itemId;
    }
}
