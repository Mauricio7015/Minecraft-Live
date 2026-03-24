using UnityEngine;
using ZombieIslandVR.Player;

namespace ZombieIslandVR.Weapons
{
    /// <summary>
    /// Firearm: pistol, shotgun, rifle, sniper. Physical VR reload mechanic.
    /// </summary>
    public class FirearmWeapon : WeaponBase
    {
        // ─── Firearm Config ───────────────────────────────────────────────────
        [Header("Firearm Config")]
        public int magazineSize = 15;
        public int currentAmmo = 15;
        public int reserveAmmo = 90;
        public float fireRate = 8f;       // shots per second
        public float damage = 25f;
        public float range = 100f;
        public float headshotMultiplier = 2.5f;
        public bool isAutomatic = false;
        public float recoilForce = 0.05f;

        [Header("Grip Offsets")]
        public Vector3 gripOffset = new Vector3(0f, -0.1f, 0.05f);
        public Vector3 gripEuler = new Vector3(0f, 0f, 0f);

        [Header("VFX")]
        public ParticleSystem muzzleFlash;
        public GameObject bulletCasingPrefab;
        public Transform casingEjectPoint;
        public LineRenderer bulletTracer;

        [Header("Audio")]
        public AudioClip fireSound;
        public AudioClip emptyClickSound;
        public AudioClip reloadSound;

        [Header("Haptics")]
        [Range(0f, 1f)] public float fireHapticAmplitude = 0.8f;
        public float fireHapticDuration = 0.05f;

        // ─── State ────────────────────────────────────────────────────────────
        private float _nextFireTime;
        private bool _triggerHeld;
        private bool _isReloading;

        // ─── Overrides ────────────────────────────────────────────────────────

        protected override Vector3 GetGripOffset() => gripOffset;
        protected override Quaternion GetGripRotation() => Quaternion.Euler(gripEuler);

        public override void TriggerPulled()
        {
            if (_isReloading) return;
            if (!isAutomatic && _triggerHeld) return;
            _triggerHeld = true;
            Fire();
        }

        public void TriggerReleased() => _triggerHeld = false;

        private void Update()
        {
            if (IsHeld && isAutomatic && _triggerHeld)
                Fire();
        }

        // ─── Fire ─────────────────────────────────────────────────────────────

        private void Fire()
        {
            if (Time.time < _nextFireTime) return;
            _nextFireTime = Time.time + 1f / fireRate;

            if (currentAmmo <= 0)
            {
                PlaySound(emptyClickSound);
                TriggerHaptics(0.2f, 0.03f);
                return;
            }

            currentAmmo--;
            PlaySound(fireSound);
            muzzleFlash?.Play();
            EjectCasing();
            TriggerHaptics(fireHapticAmplitude, fireHapticDuration);
            CastBullet();

            // Propagate gunshot sound to nearby zombies
            ZombieIslandVR.Systems.GunShotEventSystem.Instance?.ReportGunShot(transform.position, 1f);
        }

        private void CastBullet()
        {
            Transform origin = muzzleFlash != null ? muzzleFlash.transform : transform;
            Ray ray = new Ray(origin.position, origin.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, range))
            {
                // Draw tracer
                if (bulletTracer != null)
                    DrawTracer(origin.position, hit.point);

                // Apply damage
                var health = hit.collider.GetComponentInParent<ZombieIslandVR.Zombie.ZombieHealth>();
                if (health != null)
                {
                    bool headshot = hit.collider.CompareTag("Head");
                    float dmg = headshot ? damage * headshotMultiplier : damage;
                    health.TakeDamage(dmg, ray.direction * recoilForce * 200f, hit.point);
                }

                // Apply force to rigidbodies (barrels, debris)
                if (hit.rigidbody != null)
                    hit.rigidbody.AddForceAtPosition(ray.direction * recoilForce * 500f, hit.point);
            }
        }

        // ─── Reload (physical) ────────────────────────────────────────────────

        /// <summary>Called by magazine snap-zone when new magazine inserted.</summary>
        public void InsertMagazine()
        {
            if (_isReloading) return;
            StartCoroutine(ReloadSequence());
        }

        private System.Collections.IEnumerator ReloadSequence()
        {
            _isReloading = true;
            PlaySound(reloadSound);
            yield return new WaitForSeconds(2f);

            int needed = magazineSize - currentAmmo;
            int loaded = Mathf.Min(needed, reserveAmmo);
            currentAmmo += loaded;
            reserveAmmo -= loaded;

            _isReloading = false;
        }

        // ─── Helpers ──────────────────────────────────────────────────────────

        private void EjectCasing()
        {
            if (bulletCasingPrefab == null || casingEjectPoint == null) return;
            var casing = Instantiate(bulletCasingPrefab, casingEjectPoint.position, casingEjectPoint.rotation);
            var rb = casing.GetComponent<Rigidbody>();
            if (rb != null)
                rb.linearVelocity = casingEjectPoint.right * 2f + Vector3.up * 1f;
            Destroy(casing, 5f);
        }

        private void DrawTracer(Vector3 from, Vector3 to)
        {
            bulletTracer.enabled = true;
            bulletTracer.SetPosition(0, from);
            bulletTracer.SetPosition(1, to);
            Invoke(nameof(HideTracer), 0.05f);
        }

        private void HideTracer() { if (bulletTracer) bulletTracer.enabled = false; }

        private void TriggerHaptics(float amplitude, float duration)
        {
            // Meta XR haptics via UnityEngine.XR.InputDevice
            // Requires com.unity.xr.openxr or Meta XR SDK
            var devices = new System.Collections.Generic.List<UnityEngine.XR.InputDevice>();
            UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(
                UnityEngine.XR.InputDeviceCharacteristics.Right |
                UnityEngine.XR.InputDeviceCharacteristics.Controller, devices);
            if (devices.Count > 0)
                devices[0].SendHapticImpulse(0, amplitude, duration);
        }
    }
}
