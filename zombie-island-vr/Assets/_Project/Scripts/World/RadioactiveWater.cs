using UnityEngine;

namespace ZombieIslandVR.World
{
    /// <summary>
    /// Radioactive water border. Damages player and dissolves zombies.
    /// Attach to a large trigger collider surrounding the island.
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

        private Transform _player;
        private bool _playerInWater;

        private void Start()
        {
            _player = GameObject.FindWithTag("Player")?.transform;
        }

        private void Update()
        {
            if (_player == null) return;

            float dist = Vector3.Distance(transform.position, _player.position);

            // Warning zone
            if (dist < warningDistance && !_playerInWater)
            {
                float t = 1f - (dist / warningDistance);
                if (geigercounterSound != null)
                {
                    // Play Geiger counter with pitch scaling
                    var src = GetComponent<AudioSource>();
                    if (src != null) src.pitch = 0.5f + t * 2f;
                }
                radioactiveParticles?.gameObject.SetActive(t > 0.3f);
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
                // TODO: apply screen radiation shader effect via post-processing volume
                return;
            }

            // Dissolve zombies
            var zombie = other.GetComponentInParent<ZombieIslandVR.Zombie.ZombieHealth>();
            if (zombie != null && !zombie.isDead)
            {
                zombie.TakeDamage(50f * Time.deltaTime, Vector3.zero, zombie.transform.position);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.GetComponent<ZombieIslandVR.Player.PlayerStats>() != null)
                _playerInWater = false;
        }
    }
}
