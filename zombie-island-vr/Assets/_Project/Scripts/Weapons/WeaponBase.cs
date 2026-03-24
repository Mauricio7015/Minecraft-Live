using UnityEngine;
using ZombieIslandVR.Player;

namespace ZombieIslandVR.Weapons
{
    public enum WeaponType { Melee, Firearm }

    /// <summary>
    /// Abstract base for all weapons. Handles grab state, attachment to hand,
    /// and exposes virtual methods for subclasses.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public abstract class WeaponBase : MonoBehaviour
    {
        [Header("Weapon Info")]
        public string weaponId;
        public string weaponName;
        public WeaponType weaponType;
        public float weightKg = 1f;
        public Sprite weaponIcon;

        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip equipSound;
        public AudioClip unequipSound;

        // ─── State ────────────────────────────────────────────────────────────
        public bool IsHeld { get; protected set; }
        protected Transform _handAnchor;
        protected PlayerHands _playerHands;
        protected Rigidbody _rb;
        protected Collider[] _colliders;

        protected virtual void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _colliders = GetComponentsInChildren<Collider>();
        }

        // ─── Grab / Release ───────────────────────────────────────────────────

        public virtual void OnGrab(Transform handAnchor, PlayerHands hands)
        {
            IsHeld = true;
            _handAnchor = handAnchor;
            _playerHands = hands;

            // Attach to hand
            transform.SetParent(handAnchor);
            transform.localPosition = GetGripOffset();
            transform.localRotation = GetGripRotation();

            // Disable physics while held
            _rb.isKinematic = true;
            foreach (var col in _colliders)
                col.enabled = false;

            PlaySound(equipSound);
        }

        public virtual void OnRelease()
        {
            IsHeld = false;
            _handAnchor = null;

            transform.SetParent(null);

            // Re-enable physics, apply throw velocity
            _rb.isKinematic = false;
            if (_playerHands != null)
                _rb.linearVelocity = _playerHands.RightHandVelocity;

            foreach (var col in _colliders)
                col.enabled = true;

            PlaySound(unequipSound);
            _playerHands = null;
        }

        // ─── Abstract Interface ───────────────────────────────────────────────

        public abstract void TriggerPulled();

        protected virtual Vector3 GetGripOffset() => Vector3.zero;
        protected virtual Quaternion GetGripRotation() => Quaternion.identity;

        // ─── Helpers ──────────────────────────────────────────────────────────

        protected void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
                audioSource.PlayOneShot(clip);
        }
    }
}
