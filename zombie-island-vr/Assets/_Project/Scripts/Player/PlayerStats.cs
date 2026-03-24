using UnityEngine;
using UnityEngine.Events;

namespace ZombieIslandVR.Player
{
    /// <summary>
    /// Central hub for all player vital statistics: Health, Stamina, Hunger, Thirst.
    /// Attach to the XR Rig root GameObject.
    /// </summary>
    public class PlayerStats : MonoBehaviour
    {
        // ─── Health ───────────────────────────────────────────────────────────
        [Header("Health")]
        [Range(1f, 500f)] public float maxHealth = 100f;
        [HideInInspector] public float currentHealth = 100f;
        public float healthRegenRate = 0.5f;       // HP/s when well-fed & hydrated
        public float criticalHealthThreshold = 25f;

        // ─── Stamina ──────────────────────────────────────────────────────────
        [Header("Stamina")]
        [Range(1f, 500f)] public float maxStamina = 100f;
        [HideInInspector] public float currentStamina = 100f;
        public float staminaDrainSprint = 20f;     // per second while sprinting
        public float staminaDrainMelee = 25f;      // per swing
        public float staminaRegenRate = 10f;       // per second at rest
        public float staminaRegenDelay = 1.5f;     // seconds before regen starts
        private float _staminaRegenTimer;

        // ─── Hunger ───────────────────────────────────────────────────────────
        [Header("Hunger")]
        [Range(1f, 200f)] public float maxHunger = 100f;
        [HideInInspector] public float currentHunger = 100f;
        public float hungerDrainRate = 1f / 60f;   // per second (1 unit/minute)
        public float starvationDamage = 2f;        // HP/s when hunger < 10

        // ─── Thirst ───────────────────────────────────────────────────────────
        [Header("Thirst")]
        [Range(1f, 200f)] public float maxThirst = 100f;
        [HideInInspector] public float currentThirst = 100f;
        public float thirstDrainRate = 1.5f / 60f; // per second (1.5 units/minute)
        public float dehydrationDamage = 3f;       // HP/s when thirst < 10

        // ─── Events ───────────────────────────────────────────────────────────
        [Header("Events")]
        public UnityEvent<float> onHealthChanged;
        public UnityEvent<float> onStaminaChanged;
        public UnityEvent<float> onHungerChanged;
        public UnityEvent<float> onThirstChanged;
        public UnityEvent onPlayerDied;
        public UnityEvent onCriticalHealth;

        // ─── State ────────────────────────────────────────────────────────────
        public bool IsSprinting { get; set; }
        public bool IsDead { get; private set; }
        private bool _criticalHealthTriggered;

        // ─── Properties ───────────────────────────────────────────────────────
        public float HealthPercent => currentHealth / maxHealth;
        public float StaminaPercent => currentStamina / maxStamina;
        public float HungerPercent => currentHunger / maxHunger;
        public float ThirstPercent => currentThirst / maxThirst;
        public bool IsWellFed => currentHunger > 40f && currentThirst > 40f;

        private void Awake()
        {
            currentHealth = maxHealth;
            currentStamina = maxStamina;
            currentHunger = maxHunger;
            currentThirst = maxThirst;
        }

        private void Update()
        {
            if (IsDead) return;

            UpdateHunger();
            UpdateThirst();
            UpdateStamina();
            UpdateHealthRegen();
            CheckCriticalStates();
        }

        // ─── Public API ───────────────────────────────────────────────────────

        public void TakeDamage(float amount)
        {
            if (IsDead) return;
            currentHealth = Mathf.Max(0f, currentHealth - amount);
            onHealthChanged?.Invoke(currentHealth / maxHealth);
            if (currentHealth <= 0f) Die();
        }

        public void Heal(float amount)
        {
            if (IsDead) return;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            onHealthChanged?.Invoke(currentHealth / maxHealth);
        }

        public void Eat(float hungerAmount, float healthBonus = 0f)
        {
            currentHunger = Mathf.Min(maxHunger, currentHunger + hungerAmount);
            if (healthBonus > 0f) Heal(healthBonus);
            onHungerChanged?.Invoke(currentHunger / maxHunger);
        }

        public void Drink(float thirstAmount, float hungerPenalty = 0f)
        {
            currentThirst = Mathf.Min(maxThirst, currentThirst + thirstAmount);
            if (hungerPenalty > 0f)
                currentHunger = Mathf.Max(0f, currentHunger - hungerPenalty);
            onThirstChanged?.Invoke(currentThirst / maxThirst);
        }

        /// <summary>Drain stamina for a melee swing. Returns false if not enough.</summary>
        public bool UseStaminaMelee()
        {
            if (currentStamina < staminaDrainMelee * 0.5f) return false;
            currentStamina = Mathf.Max(0f, currentStamina - staminaDrainMelee);
            _staminaRegenTimer = staminaRegenDelay;
            onStaminaChanged?.Invoke(currentStamina / maxStamina);
            return true;
        }

        // ─── Private ──────────────────────────────────────────────────────────

        private void UpdateHunger()
        {
            currentHunger = Mathf.Max(0f, currentHunger - hungerDrainRate * Time.deltaTime);
            onHungerChanged?.Invoke(currentHunger / maxHunger);

            if (currentHunger < 10f)
                TakeDamage(starvationDamage * Time.deltaTime);
        }

        private void UpdateThirst()
        {
            currentThirst = Mathf.Max(0f, currentThirst - thirstDrainRate * Time.deltaTime);
            onThirstChanged?.Invoke(currentThirst / maxThirst);

            if (currentThirst < 10f)
                TakeDamage(dehydrationDamage * Time.deltaTime);
        }

        private void UpdateStamina()
        {
            if (IsSprinting && currentStamina > 0f)
            {
                currentStamina = Mathf.Max(0f, currentStamina - staminaDrainSprint * Time.deltaTime);
                _staminaRegenTimer = staminaRegenDelay;
                onStaminaChanged?.Invoke(currentStamina / maxStamina);
            }
            else
            {
                if (_staminaRegenTimer > 0f)
                {
                    _staminaRegenTimer -= Time.deltaTime;
                }
                else if (currentStamina < maxStamina)
                {
                    currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegenRate * Time.deltaTime);
                    onStaminaChanged?.Invoke(currentStamina / maxStamina);
                }
            }
        }

        private void UpdateHealthRegen()
        {
            if (IsWellFed && currentHealth < maxHealth)
                Heal(healthRegenRate * Time.deltaTime);
        }

        private void CheckCriticalStates()
        {
            if (!_criticalHealthTriggered && currentHealth <= criticalHealthThreshold)
            {
                _criticalHealthTriggered = true;
                onCriticalHealth?.Invoke();
            }
            if (_criticalHealthTriggered && currentHealth > criticalHealthThreshold)
                _criticalHealthTriggered = false;
        }

        private void Die()
        {
            IsDead = true;
            onPlayerDied?.Invoke();
            Debug.Log("[PlayerStats] Player has died.");
        }
    }
}
