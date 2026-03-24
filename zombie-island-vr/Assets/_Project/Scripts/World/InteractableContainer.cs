using System.Collections;
using UnityEngine;
using ZombieIslandVR.Player;

namespace ZombieIslandVR.World
{
    /// <summary>
    /// A searchable container (locker, crate, cabinet) with a loot table.
    /// Lid/door pops open with physics. Resets at dawn if configured.
    /// </summary>
    public class InteractableContainer : InteractableObject
    {
        [Header("Container Config")]
        public Transform lidOrDoor;              // the part that opens (can be null)
        public float lidOpenForce = 3f;          // impulse applied to lid Rigidbody on open
        public bool resetOnDawn = false;         // refill loot next day

        [Header("Visual")]
        public Renderer containerRenderer;
        public Material searchedMaterial;        // swapped in after looting
        private Material _originalMaterial;

        private DayNightCycle _dayNightCycle;

        protected override void Awake()
        {
            base.Awake();
            interactionType = InteractionType.Use;
            interactionPrompt = "Revistar";

            if (containerRenderer != null)
                _originalMaterial = containerRenderer.material;
        }

        private void Start()
        {
            _dayNightCycle = FindObjectOfType<DayNightCycle>();
            if (resetOnDawn && _dayNightCycle != null)
                _dayNightCycle.onDawnBegin.AddListener(ResetContainer);
        }

        protected override void Use(PlayerInventory inventory)
        {
            Search(inventory);
        }

        public override void Search(PlayerInventory inventory)
        {
            if (searched)
            {
                Debug.Log($"[Container] {gameObject.name} already searched.");
                return;
            }

            // Animate lid
            if (lidOrDoor != null)
                StartCoroutine(PopLid());

            // Drop loot into inventory
            if (lootTable != null && inventory != null)
            {
                var drops = lootTable.GetDrops();
                int added = 0;
                foreach (var drop in drops)
                {
                    if (inventory.AddItem(drop)) added++;
                    Debug.Log($"[Container] Found: {drop.quantity}x {drop.displayName}");
                }
                Debug.Log($"[Container] {gameObject.name} yielded {added} item(s).");
            }

            searched = true;
            interactionPrompt = "Vazio";
            PlayInteractSound();

            // Swap to "searched" material
            if (containerRenderer != null && searchedMaterial != null)
                containerRenderer.material = searchedMaterial;

            onInteract?.Invoke();
        }

        private IEnumerator PopLid()
        {
            var rb = lidOrDoor.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.AddForce(lidOrDoor.up * lidOpenForce + lidOrDoor.forward * (lidOpenForce * 0.5f),
                            ForceMode.Impulse);
            }
            else
            {
                // Simple tween if no Rigidbody
                Quaternion from = lidOrDoor.localRotation;
                Quaternion to = from * Quaternion.Euler(-90f, 0f, 0f);
                float t = 0f;
                while (t < 0.3f)
                {
                    t += Time.deltaTime;
                    lidOrDoor.localRotation = Quaternion.Lerp(from, to, t / 0.3f);
                    yield return null;
                }
                lidOrDoor.localRotation = to;
            }
        }

        private void ResetContainer()
        {
            searched = false;
            interactionPrompt = "Revistar";

            if (containerRenderer != null && _originalMaterial != null)
                containerRenderer.material = _originalMaterial;

            // Reset lid position
            if (lidOrDoor != null)
            {
                var rb = lidOrDoor.GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = true;
            }

            Debug.Log($"[Container] {gameObject.name} reset for new day.");
        }
    }
}
