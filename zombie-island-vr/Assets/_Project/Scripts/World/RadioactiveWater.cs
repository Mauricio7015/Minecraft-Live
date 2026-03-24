using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ZombieIslandVR.World
{
    /// <summary>
    /// Radioactive water border. Damages player and dissolves zombies.
    /// Attach to a large trigger collider surrounding the island.
    /// Requires a Global Volume with ColorAdjustments and Vignette in the scene.
    /// </summary>
    public class RadioactiveWater : MonoBehaviour
    {
        [Header("Damage")]
        public float playerDamagePerSecond = 15f;
        public float zombieDissolveDuration = 3f;

        [Header("Warning")]
        public float warningDistance = 30f;
        public AudioClip geigercounterSound;
        public ParticleSystem radioactiveParticles;

        [Header("Visual Feedback")]
        public Material radioactiveMaterial;
        public Color borderWarningColor = new Color(0.5f, 1f, 0.2f, 0.3f);

        [Header("Post-Processing (URP)")]
        [Tooltip("Assign the Global Volume in the scene. Needs ColorAdjustments + Vignette overrides.")]
        public Volume postProcessVolume;
        public Color radiationTint = new Color(0.55f, 1f, 0.35f);   // sickly green
        public float maxVignetteIntensity = 0.55f;
        public float pulseSpeed = 2.5f;

        // ─── State ────────────────────────────────────────────────────────────
        private Transform _player;
        private bool _playerInWater;
        private AudioSource _audioSource;

        // Post-processing overrides
        private ColorAdjustments _colorAdj;
        private Vignette _vignette;
        private ChromaticAberration _chromatic;

        private void Start()
        {
            _player = GameObject.FindWithTag("Player")?.transform;
            _audioSource = GetComponent<AudioSource>();

            // Auto-find volume if not assigned
            if (postProcessVolume == null)
                postProcessVolume = FindObjectOfType<Volume>();

            if (postProcessVolume != null && postProcessVolume.profile != null)
            {
                postProcessVolume.profile.TryGet(out _colorAdj);
                postProcessVolume.profile.TryGet(out _vignette);
                postProcessVolume.profile.TryGet(out _chromatic);
            }
            else
            {
                Debug.LogWarning("[RadioactiveWater] No post-process Volume found — " +
                                 "add a Global Volume with ColorAdjustments + Vignette to the scene.");
            }
        }

        private void Update()
        {
            if (_player == null) return;

            float dist = Vector3.Distance(transform.position, _player.position);

            if (dist < warningDistance && !_playerInWater)
            {
                float proximity = 1f - (dist / warningDistance);   // 0 = far, 1 = at edge

                // Geiger counter: pitch increases as player nears the water
                if (_audioSource != null && geigercounterSound != null)
                {
                    if (!_audioSource.isPlaying) _audioSource.clip = geigercounterSound;
                    _audioSource.pitch = 0.6f + proximity * 2f;
                    if (!_audioSource.isPlaying) _audioSource.Play();
                }

                radioactiveParticles?.gameObject.SetActive(proximity > 0.3f);

                // Subtle green tint warning in post-processing
                ApplyRadiationEffect(proximity * 0.4f);   // max 40% intensity in warning zone
            }
            else if (!_playerInWater)
            {
                // Outside warning zone — clear effects
                if (_audioSource != null && _audioSource.isPlaying)
                    _audioSource.Stop();
                radioactiveParticles?.gameObject.SetActive(false);
                ApplyRadiationEffect(0f);
            }

            // Full radiation effect when inside water
            if (_playerInWater)
            {
                float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;   // 0-1 sine wave
                ApplyRadiationEffect(0.6f + pulse * 0.4f);   // 60-100% intensity
            }
        }

        private void OnTriggerStay(Collider other)
        {
            // Damage player
            var stats = other.GetComponent<ZombieIslandVR.Player.PlayerStats>();
            if (stats != null)
            {
                _playerInWater = true;
                stats.TakeDamage(playerDamagePerSecond * Time.deltaTime);
                return;
            }

            // Dissolve zombies faster
            var zombie = other.GetComponentInParent<ZombieIslandVR.Zombie.ZombieHealth>();
            if (zombie != null && !zombie.isDead)
                zombie.TakeDamage(50f * Time.deltaTime, Vector3.zero, zombie.transform.position);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponent<ZombieIslandVR.Player.PlayerStats>() != null)
            {
                _playerInWater = false;
                ApplyRadiationEffect(0f);
            }
        }

        // ─── Post-Processing ──────────────────────────────────────────────────

        /// <summary>
        /// Applies green radiation tint, vignette, and chromatic aberration.
        /// intensity: 0 = none, 1 = full radiation.
        /// </summary>
        private void ApplyRadiationEffect(float intensity)
        {
            if (_colorAdj != null)
            {
                // Green color filter
                _colorAdj.colorFilter.overrideState = true;
                _colorAdj.colorFilter.value = Color.Lerp(Color.white, radiationTint, intensity);

                // Slight desaturation
                _colorAdj.saturation.overrideState = true;
                _colorAdj.saturation.value = Mathf.Lerp(0f, -30f, intensity);
            }

            if (_vignette != null)
            {
                _vignette.intensity.overrideState = true;
                _vignette.intensity.value = intensity * maxVignetteIntensity;

                _vignette.color.overrideState = true;
                _vignette.color.value = radiationTint;
            }

            if (_chromatic != null)
            {
                _chromatic.intensity.overrideState = true;
                _chromatic.intensity.value = intensity * 0.4f;
            }
        }
    }
}
