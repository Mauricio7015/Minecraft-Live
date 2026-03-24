using UnityEngine;
using UnityEngine.Events;

namespace ZombieIslandVR.World
{
    /// <summary>
    /// Controls the directional light to simulate day/night.
    /// One full cycle = cycleDuration seconds (default 20 min).
    /// </summary>
    public class DayNightCycle : MonoBehaviour
    {
        [Header("Config")]
        public Light sunLight;
        public float cycleDuration = 1200f;    // seconds for full 24h cycle
        [Range(0f, 1f)] public float timeOfDay = 0.25f;  // 0=midnight, 0.25=dawn, 0.5=noon

        [Header("Colors")]
        public Gradient sunColor;
        public Gradient ambientColor;
        public AnimationCurve intensityCurve;

        [Header("Fog")]
        public bool enableFog = true;
        public Gradient fogColor;
        public AnimationCurve fogDensityCurve;

        [Header("Events")]
        public UnityEvent onDawnBegin;
        public UnityEvent onDuskBegin;
        public UnityEvent onNightBegin;

        // ─── Properties ───────────────────────────────────────────────────────
        public bool IsNight => timeOfDay < 0.2f || timeOfDay > 0.8f;
        public float CurrentHour => timeOfDay * 24f;

        private bool _nightTriggered;
        private bool _dawnTriggered;
        private bool _duskTriggered;

        private void Update()
        {
            timeOfDay += Time.deltaTime / cycleDuration;
            if (timeOfDay >= 1f) { timeOfDay = 0f; ResetTriggers(); }

            UpdateSun();
            UpdateAmbient();
            UpdateFog();
            CheckEvents();
        }

        private void UpdateSun()
        {
            if (sunLight == null) return;
            // Rotate sun: full 360° per cycle
            sunLight.transform.rotation = Quaternion.Euler(timeOfDay * 360f - 90f, 170f, 0f);
            sunLight.color = sunColor.Evaluate(timeOfDay);
            sunLight.intensity = intensityCurve.Evaluate(timeOfDay);
        }

        private void UpdateAmbient()
        {
            RenderSettings.ambientLight = ambientColor.Evaluate(timeOfDay);
        }

        private void UpdateFog()
        {
            if (!enableFog) return;
            RenderSettings.fogColor = fogColor.Evaluate(timeOfDay);
            RenderSettings.fogDensity = fogDensityCurve.Evaluate(timeOfDay);
        }

        private void CheckEvents()
        {
            // Dawn ~6:00
            if (!_dawnTriggered && timeOfDay > 0.22f && timeOfDay < 0.26f)
            { _dawnTriggered = true; onDawnBegin?.Invoke(); }
            // Dusk ~18:00
            if (!_duskTriggered && timeOfDay > 0.72f && timeOfDay < 0.76f)
            { _duskTriggered = true; onDuskBegin?.Invoke(); }
            // Night ~20:00
            if (!_nightTriggered && timeOfDay > 0.82f)
            { _nightTriggered = true; onNightBegin?.Invoke(); }
        }

        private void ResetTriggers()
        {
            _nightTriggered = false;
            _dawnTriggered = false;
            _duskTriggered = false;
        }
    }
}
