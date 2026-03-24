using UnityEngine;
using UnityEngine.XR;
using ZombieIslandVR.Weapons;

namespace ZombieIslandVR.Player
{
    /// <summary>
    /// Manages what each VR hand is holding, physical grab logic,
    /// and two-handed weapon support.
    /// Designed to work with Meta XR SDK OVRGrabber or XRI XRGrabInteractable.
    /// </summary>
    public class PlayerHands : MonoBehaviour
    {
        [Header("Hand Transforms")]
        public Transform leftHandAnchor;
        public Transform rightHandAnchor;

        [Header("Grab Settings")]
        public float grabRadius = 0.15f;
        public LayerMask grabbableLayer;

        // Currently held items
        public WeaponBase LeftHandWeapon { get; private set; }
        public WeaponBase RightHandWeapon { get; private set; }

        // Velocity tracking for melee physics
        private Vector3 _rightHandPrevPos;
        private Vector3 _leftHandPrevPos;
        public Vector3 RightHandVelocity { get; private set; }
        public Vector3 LeftHandVelocity { get; private set; }

        private InputDevice _leftDevice;
        private InputDevice _rightDevice;

        private void Start()
        {
            RefreshDevices();
        }

        private void Update()
        {
            RefreshDevices();
            TrackHandVelocity();
            HandleGrabInput();
            HandleFireInput();
        }

        // ─── Velocity Tracking ────────────────────────────────────────────────

        private void TrackHandVelocity()
        {
            if (rightHandAnchor != null)
            {
                RightHandVelocity = (rightHandAnchor.position - _rightHandPrevPos) / Time.deltaTime;
                _rightHandPrevPos = rightHandAnchor.position;
            }
            if (leftHandAnchor != null)
            {
                LeftHandVelocity = (leftHandAnchor.position - _leftHandPrevPos) / Time.deltaTime;
                _leftHandPrevPos = leftHandAnchor.position;
            }
        }

        // ─── Grab ─────────────────────────────────────────────────────────────

        private void HandleGrabInput()
        {
            // Right hand grab (grip button)
            if (_rightDevice.TryGetFeatureValue(CommonUsages.gripButton, out bool rightGrip))
            {
                if (rightGrip && RightHandWeapon == null)
                    TryGrab(rightHandAnchor, ref RightHandWeapon, "right");
                else if (!rightGrip && RightHandWeapon != null)
                    Release(ref RightHandWeapon, "right");
            }

            // Left hand grab
            if (_leftDevice.TryGetFeatureValue(CommonUsages.gripButton, out bool leftGrip))
            {
                if (leftGrip && LeftHandWeapon == null)
                    TryGrab(leftHandAnchor, ref LeftHandWeapon, "left");
                else if (!leftGrip && LeftHandWeapon != null)
                    Release(ref LeftHandWeapon, "left");
            }
        }

        private void TryGrab(Transform handAnchor, ref WeaponBase slot, string hand)
        {
            Collider[] hits = Physics.OverlapSphere(handAnchor.position, grabRadius, grabbableLayer);
            WeaponBase closest = null;
            float minDist = float.MaxValue;
            foreach (var hit in hits)
            {
                var weapon = hit.GetComponent<WeaponBase>();
                if (weapon == null || weapon.IsHeld) continue;
                float d = Vector3.Distance(handAnchor.position, hit.transform.position);
                if (d < minDist) { minDist = d; closest = weapon; }
            }

            if (closest != null)
            {
                slot = closest;
                slot.OnGrab(handAnchor, this);
                Debug.Log($"[PlayerHands] {hand} hand grabbed {closest.weaponName}");
            }
        }

        private void Release(ref WeaponBase slot, string hand)
        {
            if (slot != null)
            {
                slot.OnRelease();
                Debug.Log($"[PlayerHands] {hand} hand released {slot.weaponName}");
                slot = null;
            }
        }

        // ─── Fire ─────────────────────────────────────────────────────────────

        private bool _rightTriggerWasHeld;
        private bool _leftTriggerWasHeld;

        private void HandleFireInput()
        {
            // Trigger = fire for firearms, swing detection handled in MeleeWeapon
            _rightDevice.TryGetFeatureValue(CommonUsages.triggerButton, out bool rightTrigger);
            _leftDevice.TryGetFeatureValue(CommonUsages.triggerButton, out bool leftTrigger);

            if (rightTrigger)
                RightHandWeapon?.TriggerPulled();
            else if (_rightTriggerWasHeld)
                (RightHandWeapon as ZombieIslandVR.Weapons.FirearmWeapon)?.TriggerReleased();

            if (leftTrigger)
                LeftHandWeapon?.TriggerPulled();
            else if (_leftTriggerWasHeld)
                (LeftHandWeapon as ZombieIslandVR.Weapons.FirearmWeapon)?.TriggerReleased();

            _rightTriggerWasHeld = rightTrigger;
            _leftTriggerWasHeld = leftTrigger;
        }

        // ─── Helpers ──────────────────────────────────────────────────────────

        private void RefreshDevices()
        {
            if (!_leftDevice.isValid)
            {
                var list = new System.Collections.Generic.List<InputDevice>();
                InputDevices.GetDevicesWithCharacteristics(
                    InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller, list);
                if (list.Count > 0) _leftDevice = list[0];
            }
            if (!_rightDevice.isValid)
            {
                var list = new System.Collections.Generic.List<InputDevice>();
                InputDevices.GetDevicesWithCharacteristics(
                    InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, list);
                if (list.Count > 0) _rightDevice = list[0];
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (rightHandAnchor)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(rightHandAnchor.position, grabRadius);
            }
            if (leftHandAnchor)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(leftHandAnchor.position, grabRadius);
            }
        }
    }
}
