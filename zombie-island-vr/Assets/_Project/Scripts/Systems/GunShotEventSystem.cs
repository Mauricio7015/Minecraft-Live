using UnityEngine;

namespace ZombieIslandVR.Systems
{
    /// <summary>
    /// Singleton. Broadcasts gunshot sounds to nearby zombies via OverlapSphere.
    /// FirearmWeapon calls ReportGunShot() on every shot fired.
    /// </summary>
    public class GunShotEventSystem : MonoBehaviour
    {
        public static GunShotEventSystem Instance { get; private set; }

        [Header("Config")]
        public float hearingRangeMultiplier = 25f;  // loudness * this = alert radius in metres
        public LayerMask zombieLayer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Called by FirearmWeapon.Fire() with the muzzle world position.
        /// <paramref name="loudness"/> is a 0-1 scale where 1 = pistol, values > 1 for louder weapons.
        /// </summary>
        public void ReportGunShot(Vector3 position, float loudness = 1f)
        {
            float alertRadius = loudness * hearingRangeMultiplier;

            Collider[] hits = Physics.OverlapSphere(position, alertRadius, zombieLayer);
            foreach (var hit in hits)
            {
                var ai = hit.GetComponentInParent<ZombieIslandVR.Zombie.ZombieAI>();
                ai?.OnGunshotHeard(position);
            }
        }
    }
}
