using UnityEngine;
using UnityEngine.XR;

namespace ZombieIslandVR.Player
{
    /// <summary>
    /// VR Player locomotion: smooth movement, snap turn, sprint, teleport option.
    /// Requires Meta XR SDK or XR Interaction Toolkit with OpenXR.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerStats))]
    public class PlayerController : MonoBehaviour
    {
        // ─── References ───────────────────────────────────────────────────────
        [Header("References")]
        public Transform cameraTransform;
        public CharacterController characterController;
        private PlayerStats _stats;

        // ─── Locomotion ───────────────────────────────────────────────────────
        [Header("Movement")]
        public float walkSpeed = 2.5f;
        public float sprintSpeed = 5f;
        public float turnSpeed = 90f;          // degrees per second (smooth turn)
        public bool useSnapTurn = true;
        public float snapAngle = 30f;
        public float snapCooldown = 0.3f;

        [Header("Comfort")]
        public bool enableVignette = true;
        [Range(0f, 1f)] public float vignetteIntensity = 0.5f;

        // ─── Input Device Cache ───────────────────────────────────────────────
        private InputDevice _leftController;
        private InputDevice _rightController;
        private float _snapTimer;
        private bool _isTeleportMode;

        // ─── Gravity ──────────────────────────────────────────────────────────
        private float _verticalVelocity;
        private const float Gravity = -9.81f;

        private void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            characterController = GetComponent<CharacterController>();
        }

        private void Start()
        {
            RefreshInputDevices();
        }

        private void Update()
        {
            RefreshInputDevices();
            HandleMovement();
            HandleTurn();
        }

        // ─── Movement ─────────────────────────────────────────────────────────

        private void HandleMovement()
        {
            if (_stats.IsDead) return;

            // Left joystick → move
            Vector2 primary2DAxis = Vector2.zero;
            if (_leftController.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 leftAxis))
                primary2DAxis = leftAxis;

            // Sprint: right controller grip held
            bool sprintHeld = false;
            if (_rightController.TryGetFeatureValue(CommonUsages.gripButton, out bool grip))
                sprintHeld = grip && _stats.currentStamina > 5f;

            _stats.IsSprinting = sprintHeld;
            float speed = sprintHeld ? sprintSpeed : walkSpeed;

            // Direction relative to camera (XZ only)
            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;
            forward.y = 0f; forward.Normalize();
            right.y = 0f; right.Normalize();

            Vector3 move = (forward * primary2DAxis.y + right * primary2DAxis.x) * speed;

            // Gravity
            if (characterController.isGrounded)
                _verticalVelocity = -0.5f;
            else
                _verticalVelocity += Gravity * Time.deltaTime;

            move.y = _verticalVelocity;
            characterController.Move(move * Time.deltaTime);
        }

        private void HandleTurn()
        {
            if (_stats.IsDead) return;

            _rightController.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 rightAxis);

            if (useSnapTurn)
            {
                _snapTimer -= Time.deltaTime;
                if (Mathf.Abs(rightAxis.x) > 0.7f && _snapTimer <= 0f)
                {
                    transform.Rotate(0f, Mathf.Sign(rightAxis.x) * snapAngle, 0f);
                    _snapTimer = snapCooldown;
                }
            }
            else
            {
                transform.Rotate(0f, rightAxis.x * turnSpeed * Time.deltaTime, 0f);
            }
        }

        // ─── Helpers ──────────────────────────────────────────────────────────

        private void RefreshInputDevices()
        {
            if (!_leftController.isValid)
            {
                var devices = new System.Collections.Generic.List<InputDevice>();
                InputDevices.GetDevicesWithCharacteristics(
                    InputDeviceCharacteristics.Left | InputDeviceCharacteristics.Controller, devices);
                if (devices.Count > 0) _leftController = devices[0];
            }

            if (!_rightController.isValid)
            {
                var devices = new System.Collections.Generic.List<InputDevice>();
                InputDevices.GetDevicesWithCharacteristics(
                    InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, devices);
                if (devices.Count > 0) _rightController = devices[0];
            }
        }
    }
}
