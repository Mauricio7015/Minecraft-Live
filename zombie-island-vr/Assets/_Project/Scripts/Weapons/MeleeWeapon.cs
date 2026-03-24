using UnityEngine;
using ZombieIslandVR.Player;

namespace ZombieIslandVR.Weapons
{
    /// <summary>
    /// Physics-based melee: damage scales with real hand swing velocity.
    /// Durability system, blocking, and ragdoll force application.
    /// </summary>
    public class MeleeWeapon : WeaponBase
    {
        [Header("Melee Config")]
        public float baseDamage = 45f;
        public float maxDurability = 200f;
        public float currentDurability;
        public float knockbackForce = 400f;
        public bool canBlock = false;
        public bool isFuelBased = false;   // e.g. chainsaw
        public float fuelAmount = 80f;

        [Header("Physics Damage")]
        public float minSwingSpeed = 0.5f;    // m/s minimum to deal damage
        public float criticalSwingSpeed = 2f; // m/s for critical hit (1.5x)
        public float velocityDamageMultiplier = 1.5f;

        [Header("Grip")]
        public Vector3 meleeGripOffset = new Vector3(0f, -0.1f, 0f);
        public Vector3 meleeGripEuler = new Vector3(0f, 0f, 0f);

        [Header("Audio")]
        public AudioClip swingSound;
        public AudioClip hitFleshSound;
        public AudioClip hitObjectSound;
        public AudioClip breakSound;

        [Header("VFX")]
        public ParticleSystem bloodParticles;
        public TrailRenderer swingTrail;

        // ─── State ────────────────────────────────────────────────────────────
        private bool _isBlocking;
        private float _lastSwingTime;
        private const float SwingCooldown = 0.15f;

        protected override void Awake()
        {
            base.Awake();
            currentDurability = maxDurability;
        }

        protected override Vector3 GetGripOffset() => meleeGripOffset;
        protected override Quaternion GetGripRotation() => Quaternion.Euler(meleeGripEuler);

        public override void TriggerPulled()
        {
            // Trigger = block toggle for melee (optional)
            if (canBlock) _isBlocking = !_isBlocking;
        }

        private void Update()
        {
            if (!IsHeld || _playerHands == null) return;

            UpdateSwingTrail();

            // Automatic swing detection via velocity
            float speed = _playerHands.RightHandVelocity.magnitude;
            if (speed >= minSwingSpeed && Time.time > _lastSwingTime + SwingCooldown)
            {
                AttemptSwingDamage(speed);
            }
        }

        // ─── Swing Damage ─────────────────────────────────────────────────────

        private void AttemptSwingDamage(float swingSpeed)
        {
            _lastSwingTime = Time.time;
            PlaySound(swingSound);

            // Spherecast in swing direction
            Vector3 dir = _playerHands.RightHandVelocity.normalized;
            if (Physics.SphereCast(transform.position, 0.15f, dir, out RaycastHit hit, 0.6f))
            {
                float dmg = CalculateDamage(swingSpeed);
                ApplyDamageToHit(hit, dmg, dir);
                ConsumeDurability();
            }
        }

        private float CalculateDamage(float swingSpeed)
        {
            float multiplier = 1f;
            if (swingSpeed >= criticalSwingSpeed)
                multiplier = velocityDamageMultiplier;
            else if (swingSpeed > minSwingSpeed)
                multiplier = Mathf.Lerp(0.5f, 1f, (swingSpeed - minSwingSpeed) / (criticalSwingSpeed - minSwingSpeed));

            return baseDamage * multiplier;
        }

        private void ApplyDamageToHit(RaycastHit hit, float damage, Vector3 direction)
        {
            var zombieHealth = hit.collider.GetComponentInParent<ZombieIslandVR.Zombie.ZombieHealth>();
            if (zombieHealth != null)
            {
                zombieHealth.TakeDamage(damage, direction * knockbackForce, hit.point);
                bloodParticles?.Play();
                PlaySound(hitFleshSound);
            }
            else
            {
                // Hit environment object
                if (hit.rigidbody != null)
                    hit.rigidbody.AddForceAtPosition(direction * knockbackForce * 0.5f, hit.point);
                PlaySound(hitObjectSound);
            }
        }

        private void ConsumeDurability()
        {
            if (maxDurability <= 0f) return;  // indestructible (iron bar)

            float cost = isFuelBased ? 2f : 1f;
            currentDurability = Mathf.Max(0f, currentDurability - cost);

            if (currentDurability <= 0f)
            {
                PlaySound(breakSound);
                // Notify and destroy
                Invoke(nameof(DestroyWeapon), 0.5f);
            }
        }

        private void DestroyWeapon()
        {
            if (IsHeld) OnRelease();
            Destroy(gameObject);
        }

        private void UpdateSwingTrail()
        {
            if (swingTrail == null || _playerHands == null) return;
            swingTrail.emitting = _playerHands.RightHandVelocity.magnitude > minSwingSpeed;
        }

        public float DurabilityPercent => maxDurability > 0 ? currentDurability / maxDurability : 1f;
    }
}
