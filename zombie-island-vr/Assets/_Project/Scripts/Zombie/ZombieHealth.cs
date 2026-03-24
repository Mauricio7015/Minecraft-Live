using UnityEngine;
using UnityEngine.Events;

namespace ZombieIslandVR.Zombie
{
    /// <summary>
    /// Zombie health with ragdoll death, directional knockback, and dismemberment hooks.
    /// </summary>
    public class ZombieHealth : MonoBehaviour
    {
        [Header("Health")]
        public float maxHealth = 100f;
        public float currentHealth;
        public bool isDead { get; private set; }

        [Header("Ragdoll")]
        public Rigidbody[] ragdollBodies;
        public Collider[] ragdollColliders;
        public Animator zombieAnimator;

        [Header("Dismemberment")]
        public GameObject leftArmObject;
        public GameObject rightArmObject;
        public float dismemberThreshold = 60f;    // damage in one hit to dismember

        [Header("VFX")]
        public ParticleSystem bloodSplatterVFX;
        public ParticleSystem deathVFX;
        public GameObject dissolveOnDeath;

        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip[] hurtSounds;
        public AudioClip[] deathSounds;

        [Header("Events")]
        public UnityEvent onDeath;
        public UnityEvent<float> onDamaged;  // normalized health

        private ZombieAI _ai;

        private void Awake()
        {
            currentHealth = maxHealth;
            _ai = GetComponent<ZombieAI>();
            DisableRagdoll();
        }

        // ─── Public API ───────────────────────────────────────────────────────

        public void TakeDamage(float amount, Vector3 force, Vector3 hitPoint)
        {
            if (isDead) return;

            currentHealth -= amount;
            onDamaged?.Invoke(currentHealth / maxHealth);

            bloodSplatterVFX?.Play();
            PlayRandomSound(hurtSounds);

            // Alert AI
            _ai?.OnHit(hitPoint);

            // Check dismemberment
            if (amount >= dismemberThreshold)
                TryDismember(force);

            if (currentHealth <= 0f)
                Die(force, hitPoint);
        }

        private void Die(Vector3 force, Vector3 hitPoint)
        {
            isDead = true;
            _ai?.enabled = false;
            zombieAnimator.enabled = false;

            EnableRagdoll();

            // Apply death force
            Rigidbody closestRb = FindClosestRagdollBody(hitPoint);
            if (closestRb != null)
                closestRb.AddForce(force, ForceMode.Impulse);

            deathVFX?.Play();
            PlayRandomSound(deathSounds);
            onDeath?.Invoke();

            // Disable after a while
            Invoke(nameof(CleanUp), 30f);
        }

        private void TryDismember(Vector3 force)
        {
            // Randomly detach an arm based on which side was hit
            GameObject armToRemove = Random.value > 0.5f ? leftArmObject : rightArmObject;
            if (armToRemove != null && armToRemove.activeSelf)
            {
                armToRemove.transform.SetParent(null);
                var rb = armToRemove.AddComponent<Rigidbody>();
                rb.AddForce(force * 0.3f + Vector3.up * 2f, ForceMode.Impulse);
                Destroy(armToRemove, 10f);
            }
        }

        // ─── Ragdoll ──────────────────────────────────────────────────────────

        private void EnableRagdoll()
        {
            foreach (var rb in ragdollBodies)
            {
                rb.isKinematic = false;
                rb.detectCollisions = true;
            }
            foreach (var col in ragdollColliders)
                col.enabled = true;
        }

        private void DisableRagdoll()
        {
            foreach (var rb in ragdollBodies)
                rb.isKinematic = true;
            foreach (var col in ragdollColliders)
                col.enabled = false;
        }

        private Rigidbody FindClosestRagdollBody(Vector3 point)
        {
            Rigidbody closest = null;
            float min = float.MaxValue;
            foreach (var rb in ragdollBodies)
            {
                float d = Vector3.Distance(rb.position, point);
                if (d < min) { min = d; closest = rb; }
            }
            return closest;
        }

        private void CleanUp()
        {
            if (dissolveOnDeath != null)
                dissolveOnDeath.SetActive(true);
            Destroy(gameObject, 3f);
        }

        private void PlayRandomSound(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0 || audioSource == null) return;
            audioSource.PlayOneShot(clips[Random.Range(0, clips.Length)]);
        }
    }
}
