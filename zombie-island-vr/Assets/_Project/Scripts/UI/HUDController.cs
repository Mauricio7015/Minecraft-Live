using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ZombieIslandVR.Player;

namespace ZombieIslandVR.UI
{
    /// <summary>
    /// World-space VR HUD — attached to the player's wrist (left hand).
    /// Shows health, hunger, thirst, stamina, and ammo count.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("References")]
        public PlayerStats playerStats;
        public Transform wristAnchor;

        [Header("Bars")]
        public Slider healthBar;
        public Slider staminaBar;
        public Slider hungerBar;
        public Slider thirstBar;

        [Header("Ammo")]
        public TextMeshProUGUI ammoText;
        public TextMeshProUGUI reserveAmmoText;

        [Header("Status Icons")]
        public GameObject criticalHealthIcon;
        public GameObject overloadedIcon;
        public GameObject starvingIcon;
        public GameObject dehydratedIcon;

        [Header("Day/Night")]
        public TextMeshProUGUI timeText;
        public TextMeshProUGUI dayText;

        [Header("Colors")]
        public Color healthColorFull = Color.green;
        public Color healthColorCritical = Color.red;
        public Color staminaColor = Color.cyan;
        public Color hungerColor = new Color(1f, 0.6f, 0f);
        public Color thirstColor = Color.blue;

        // ─── VR Wrist HUD Settings ────────────────────────────────────────────
        [Header("Wrist HUD")]
        public bool followWrist = true;
        public Vector3 wristOffset = new Vector3(0f, 0.05f, 0f);
        public Vector3 wristRotationOffset = new Vector3(-90f, 0f, 0f);
        public bool showOnlyWhenLookingAtWrist = true;
        public float wristLookAngleThreshold = 45f;

        private Canvas _canvas;
        private Camera _mainCam;
        private int _dayCount = 1;
        private ZombieIslandVR.World.DayNightCycle _dayNightCycle;

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _mainCam = Camera.main;
        }

        private void Start()
        {
            if (playerStats != null)
            {
                playerStats.onHealthChanged.AddListener(UpdateHealth);
                playerStats.onStaminaChanged.AddListener(UpdateStamina);
                playerStats.onHungerChanged.AddListener(UpdateHunger);
                playerStats.onThirstChanged.AddListener(UpdateThirst);
            }

            _dayNightCycle = FindObjectOfType<ZombieIslandVR.World.DayNightCycle>();
            if (_dayNightCycle != null)
                _dayNightCycle.onNightBegin.AddListener(IncrementDay);
        }

        private void IncrementDay()
        {
            _dayCount++;
        }

        private void Update()
        {
            if (followWrist && wristAnchor != null)
                UpdateWristPosition();

            if (showOnlyWhenLookingAtWrist)
                UpdateVisibility();

            UpdateTimeDisplay();
            UpdateStatusIcons();
        }

        // ─── Stat Listeners ───────────────────────────────────────────────────

        private void UpdateHealth(float normalized)
        {
            if (healthBar) healthBar.value = normalized;
            if (healthBar)
            {
                var fill = healthBar.fillRect?.GetComponent<Image>();
                if (fill) fill.color = Color.Lerp(healthColorCritical, healthColorFull, normalized);
            }
        }

        private void UpdateStamina(float normalized)
        {
            if (staminaBar) staminaBar.value = normalized;
        }

        private void UpdateHunger(float normalized)
        {
            if (hungerBar) hungerBar.value = normalized;
        }

        private void UpdateThirst(float normalized)
        {
            if (thirstBar) thirstBar.value = normalized;
        }

        public void UpdateAmmo(int current, int reserve)
        {
            if (ammoText) ammoText.text = current.ToString();
            if (reserveAmmoText) reserveAmmoText.text = $"/{reserve}";
        }

        // ─── Status Icons ─────────────────────────────────────────────────────

        private void UpdateStatusIcons()
        {
            if (playerStats == null) return;
            if (criticalHealthIcon)
                criticalHealthIcon.SetActive(playerStats.currentHealth <= playerStats.criticalHealthThreshold);
            if (starvingIcon)
                starvingIcon.SetActive(playerStats.currentHunger < 10f);
            if (dehydratedIcon)
                dehydratedIcon.SetActive(playerStats.currentThirst < 10f);

            var inv = GetComponentInParent<PlayerInventory>();
            if (overloadedIcon && inv)
                overloadedIcon.SetActive(inv.IsOverloaded);
        }

        // ─── Wrist Positioning ────────────────────────────────────────────────

        private void UpdateWristPosition()
        {
            transform.position = wristAnchor.position + wristAnchor.TransformDirection(wristOffset);
            transform.rotation = wristAnchor.rotation * Quaternion.Euler(wristRotationOffset);
        }

        private void UpdateVisibility()
        {
            if (!showOnlyWhenLookingAtWrist || _mainCam == null || wristAnchor == null) return;
            Vector3 toWrist = (wristAnchor.position - _mainCam.transform.position).normalized;
            float angle = Vector3.Angle(_mainCam.transform.forward, toWrist);
            _canvas.enabled = angle < wristLookAngleThreshold;
        }

        // ─── Time Display ─────────────────────────────────────────────────────

        private void UpdateTimeDisplay()
        {
            if (_dayNightCycle == null) return;

            float hours = _dayNightCycle.CurrentHour;
            int h = Mathf.FloorToInt(hours);
            int m = Mathf.FloorToInt((hours - h) * 60f);
            if (timeText) timeText.text = $"{h:D2}:{m:D2}";
            if (dayText) dayText.text = $"Day {_dayCount}";
        }
    }
}
