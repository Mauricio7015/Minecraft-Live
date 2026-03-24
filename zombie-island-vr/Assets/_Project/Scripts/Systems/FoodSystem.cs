using UnityEngine;
using ZombieIslandVR.Player;

namespace ZombieIslandVR.Systems
{
    [CreateAssetMenu(fileName = "FoodItem_New", menuName = "ZombieIsland/Food Item")]
    public class FoodItemData : ScriptableObject
    {
        public string itemId;
        public string displayName;
        public float hungerRestore;
        public float thirstRestore;
        public float healthRestore;
        public float hungerPenalty;
        public bool requiresCooking;
        public FoodItemData cookedVersion;
        public AudioClip eatSound;
        public ParticleSystem eatVFX;
    }

    /// <summary>
    /// Handles consuming food items from inventory.
    /// Physical eating: bring food to mouth (within 0.2m of head) to consume.
    /// </summary>
    public class FoodSystem : MonoBehaviour
    {
        [Header("References")]
        public PlayerStats playerStats;
        public PlayerInventory inventory;
        public Transform headTransform;

        [Header("References")]
        public ZombieIslandVR.Player.PlayerHands playerHands;

        [Header("Eat Config")]
        public float eatDistance = 0.2f;        // distance from head to trigger eat
        public float drinkDistance = 0.25f;
        public float eatConfirmTime = 1f;       // seconds item must be near mouth

        private float _eatTimer;
        private string _pendingItemId;

        // ─── Food Table ───────────────────────────────────────────────────────
        // All food items: matches design doc

        public static readonly (string id, string name, float hunger, float thirst, float hp, float hungerPenalty)[]
            FoodItems = {
                ("canned_food",     "Canned Food",          30f,  0f,  0f,  0f),
                ("water_bottle",    "Water Bottle",          0f, 40f,  0f,  0f),
                ("wild_fruit",      "Wild Fruit",           15f, 10f,  0f,  0f),
                ("military_ration", "Military Ration",      50f,  0f, 10f, -5f),
                ("medical_herb",    "Medical Herb",          0f,  0f, 25f,  0f),
                ("boar_meat_raw",   "Raw Boar Meat",        20f,  0f, -5f,  0f),  // bad raw
                ("boar_meat_cooked","Cooked Boar Meat",     60f,  0f,  5f,  0f),
            };

        // ─── Physical Eating ──────────────────────────────────────────────────

        private void Update()
        {
            if (playerHands == null || headTransform == null) return;

            // Check if either hand is holding a food item near the player's mouth
            string heldItemId = GetHeldFoodItemId();

            if (heldItemId != null)
            {
                if (heldItemId == _pendingItemId)
                {
                    _eatTimer += Time.deltaTime;
                    if (_eatTimer >= eatConfirmTime)
                    {
                        ConsumeFood(heldItemId);
                        _eatTimer = 0f;
                        _pendingItemId = null;
                    }
                }
                else
                {
                    _pendingItemId = heldItemId;
                    _eatTimer = 0f;
                }
            }
            else
            {
                _pendingItemId = null;
                _eatTimer = 0f;
            }
        }

        private string GetHeldFoodItemId()
        {
            // Check right hand weapon — if it's an interactable food item held near mouth
            // In practice, food items have an InventoryItem component with a food itemId.
            // We check both hands via PlayerHands hand anchors proximity to headTransform.
            Transform right = playerHands.rightHandAnchor;
            Transform left = playerHands.leftHandAnchor;

            if (right != null && Vector3.Distance(right.position, headTransform.position) <= eatDistance)
            {
                // FoodPickup is the MonoBehaviour on world food objects (FoodItemData is a ScriptableObject)
                var cols = Physics.OverlapSphere(right.position, 0.1f);
                foreach (var col in cols)
                {
                    var pickup = col.GetComponent<FoodPickup>();
                    if (pickup != null && CanEat(pickup.itemId)) return pickup.itemId;
                }
            }

            if (left != null && Vector3.Distance(left.position, headTransform.position) <= drinkDistance)
            {
                var cols = Physics.OverlapSphere(left.position, 0.1f);
                foreach (var col in cols)
                {
                    var pickup = col.GetComponent<FoodPickup>();
                    if (pickup != null && CanEat(pickup.itemId)) return pickup.itemId;
                }
            }

            return null;
        }

        /// <summary>
        /// Called when player brings food prefab close to their face.
        /// Triggered by food item's proximity detection.
        /// </summary>
        public void ConsumeFood(string itemId)
        {
            if (!inventory.HasItem(itemId)) return;

            foreach (var food in FoodItems)
            {
                if (food.id != itemId) continue;

                playerStats.Eat(food.hunger, food.hp);
                playerStats.Drink(food.thirst, food.hungerPenalty);
                inventory.RemoveItem(itemId, 1);

                Debug.Log($"[FoodSystem] Consumed: {food.name} | " +
                          $"+{food.hunger} hunger, +{food.thirst} thirst, +{food.hp} HP");
                return;
            }
        }

        public bool CanEat(string itemId)
        {
            foreach (var food in FoodItems)
                if (food.id == itemId) return inventory.HasItem(itemId);
            return false;
        }
    }
}
