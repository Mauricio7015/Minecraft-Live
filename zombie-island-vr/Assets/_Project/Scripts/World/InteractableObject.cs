using UnityEngine;
using UnityEngine.Events;
using ZombieIslandVR.Player;

namespace ZombieIslandVR.World
{
    /// <summary>
    /// Base for all world interactables: doors, drawers, vehicles, campfires, generators.
    /// </summary>
    public class InteractableObject : MonoBehaviour
    {
        public enum InteractionType { Grab, Use, Open, Toggle, Eat, Drink, Cook }

        [Header("Interaction")]
        public InteractionType interactionType;
        public bool requiresTwoHands = false;
        public string interactionPrompt = "Use";
        public float interactRange = 1.5f;

        [Header("Loot (optional)")]
        public LootTable lootTable;
        public bool searched = false;

        [Header("Food/Drink (for Eat/Drink type)")]
        public float hungerRestore = 0f;
        public float thirstRestore = 0f;
        public float healthRestore = 0f;

        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip interactSound;

        [Header("Events")]
        public UnityEvent onInteract;
        public UnityEvent onOpen;
        public UnityEvent onClose;

        // ─── State ────────────────────────────────────────────────────────────
        protected bool _isOpen = false;
        protected bool _isLocked = false;
        protected Animator _animator;

        protected virtual void Awake()
        {
            _animator = GetComponent<Animator>();
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
        }

        // ─── Public API ───────────────────────────────────────────────────────

        public virtual void Interact(PlayerInventory inventory)
        {
            if (_isLocked) return;

            switch (interactionType)
            {
                case InteractionType.Open:
                    ToggleOpen();
                    break;
                case InteractionType.Toggle:
                    Toggle();
                    break;
                case InteractionType.Use:
                    Use(inventory);
                    break;
                case InteractionType.Eat:
                    Eat(inventory);
                    break;
                case InteractionType.Drink:
                    Drink(inventory);
                    break;
            }

            onInteract?.Invoke();
        }

        public virtual void Search(PlayerInventory inventory)
        {
            if (searched || lootTable == null || inventory == null) return;
            searched = true;

            foreach (var drop in lootTable.GetDrops())
            {
                inventory.AddItem(drop);
                Debug.Log($"[Loot] Found: {drop.quantity}x {drop.displayName}");
            }
        }

        // ─── Actions ──────────────────────────────────────────────────────────

        protected virtual void ToggleOpen()
        {
            _isOpen = !_isOpen;
            _animator?.SetBool("IsOpen", _isOpen);
            if (_isOpen) onOpen?.Invoke();
            else onClose?.Invoke();
        }

        protected virtual void Toggle()
        {
            _isOpen = !_isOpen;
            _animator?.SetBool("IsActive", _isOpen);
            PlayInteractSound();
        }

        protected virtual void Use(PlayerInventory inventory)
        {
            PlayInteractSound();
            onInteract?.Invoke();
        }

        protected virtual void Eat(PlayerInventory inventory)
        {
            var stats = inventory?.GetComponent<ZombieIslandVR.Player.PlayerStats>();
            if (stats == null) return;
            stats.Eat(hungerRestore, healthRestore);
            PlayInteractSound();
            Debug.Log($"[Interactable] Ate {gameObject.name}: +{hungerRestore} hunger, +{healthRestore} HP");
        }

        protected virtual void Drink(PlayerInventory inventory)
        {
            var stats = inventory?.GetComponent<ZombieIslandVR.Player.PlayerStats>();
            if (stats == null) return;
            stats.Drink(thirstRestore);
            PlayInteractSound();
            Debug.Log($"[Interactable] Drank {gameObject.name}: +{thirstRestore} thirst");
        }

        protected void PlayInteractSound()
        {
            if (audioSource != null && interactSound != null)
                audioSource.PlayOneShot(interactSound);
        }

        // ─── Prompt ───────────────────────────────────────────────────────────

        public string GetPrompt()
        {
            if (_isLocked) return "Locked";
            if (searched) return "Empty";
            return interactionPrompt;
        }
    }

    // ─── Loot Table ───────────────────────────────────────────────────────────

    [CreateAssetMenu(fileName = "LootTable_New", menuName = "ZombieIsland/Loot Table")]
    public class LootTable : ScriptableObject
    {
        [System.Serializable]
        public class LootEntry
        {
            public InventoryItem item;
            [Range(0f, 1f)] public float dropChance;
            public int minQuantity = 1;
            public int maxQuantity = 1;
        }

        public LootEntry[] entries;

        public System.Collections.Generic.List<InventoryItem> GetDrops()
        {
            var drops = new System.Collections.Generic.List<InventoryItem>();
            foreach (var e in entries)
            {
                if (Random.value <= e.dropChance)
                {
                    var item = new InventoryItem(
                        e.item.itemId, e.item.displayName,
                        Random.Range(e.minQuantity, e.maxQuantity + 1),
                        e.item.weight, e.item.category);
                    drops.Add(item);
                }
            }
            return drops;
        }
    }
}
