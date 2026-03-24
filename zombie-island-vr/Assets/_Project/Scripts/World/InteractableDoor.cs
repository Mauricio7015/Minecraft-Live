using System.Collections;
using UnityEngine;
using ZombieIslandVR.Player;

namespace ZombieIslandVR.World
{
    /// <summary>
    /// Physical door that swings open/closed via animation coroutine.
    /// Can be breached by a melee hit >= breachDamageThreshold.
    /// </summary>
    public class InteractableDoor : InteractableObject
    {
        [Header("Door Config")]
        public float openAngle = 90f;            // degrees to swing open
        public float animDuration = 0.4f;        // seconds for full swing
        public bool openInward = false;
        public float breachDamageThreshold = 50f; // melee damage needed to force open

        [Header("Audio")]
        public AudioClip openSound;
        public AudioClip closeSound;
        public AudioClip lockedSound;
        public AudioClip breachSound;

        private Quaternion _closedRotation;
        private Quaternion _openRotation;
        private bool _isAnimating;

        protected override void Awake()
        {
            base.Awake();
            interactionType = InteractionType.Open;
            interactionPrompt = "Abrir";
            _closedRotation = transform.localRotation;
            float sign = openInward ? -1f : 1f;
            _openRotation = _closedRotation * Quaternion.Euler(0f, sign * openAngle, 0f);
        }

        protected override void ToggleOpen()
        {
            if (_isAnimating) return;

            if (_isLocked)
            {
                if (audioSource != null && lockedSound != null)
                    audioSource.PlayOneShot(lockedSound);
                return;
            }

            _isOpen = !_isOpen;
            StartCoroutine(SwingDoor(_isOpen));

            if (_isOpen) onOpen?.Invoke();
            else onClose?.Invoke();
        }

        /// <summary>
        /// Called by MeleeWeapon when a heavy hit lands on the door.
        /// Forces it open regardless of lock state.
        /// </summary>
        public void ForceBreach(float damage)
        {
            if (damage < breachDamageThreshold || _isAnimating) return;
            _isLocked = false;
            _isOpen = true;
            StartCoroutine(SwingDoor(true));

            if (audioSource != null && breachSound != null)
                audioSource.PlayOneShot(breachSound);

            onOpen?.Invoke();
            Debug.Log($"[Door] {gameObject.name} breached!");
        }

        private IEnumerator SwingDoor(bool opening)
        {
            _isAnimating = true;

            Quaternion from = transform.localRotation;
            Quaternion to = opening ? _openRotation : _closedRotation;

            AudioClip sfx = opening ? openSound : closeSound;
            if (audioSource != null && sfx != null)
                audioSource.PlayOneShot(sfx);

            float elapsed = 0f;
            while (elapsed < animDuration)
            {
                elapsed += Time.deltaTime;
                transform.localRotation = Quaternion.Lerp(from, to, elapsed / animDuration);
                yield return null;
            }

            transform.localRotation = to;
            _animator?.SetBool("IsOpen", _isOpen);
            _isAnimating = false;
        }
    }
}
