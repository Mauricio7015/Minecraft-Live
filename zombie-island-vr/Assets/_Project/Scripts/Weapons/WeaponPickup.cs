using UnityEngine;

namespace ZombieIslandVR.Weapons
{
    /// <summary>
    /// Floats and rotates in the world. Shows a prompt when player hand is near.
    /// Auto-snaps into hand when grabbed (handled by WeaponBase.OnGrab).
    /// </summary>
    public class WeaponPickup : MonoBehaviour
    {
        [Header("Float Animation")]
        public float floatAmplitude = 0.08f;
        public float floatFrequency = 1.2f;
        public float rotationSpeed = 45f;

        [Header("Prompt")]
        public GameObject promptCanvas;
        public float promptDistance = 0.8f;

        private Vector3 _startPos;
        private Transform _playerCam;

        private void Start()
        {
            _startPos = transform.position;
            _playerCam = Camera.main?.transform;
        }

        private void Update()
        {
            // Float
            float y = Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
            transform.position = _startPos + Vector3.up * y;
            transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f);

            // Prompt
            if (promptCanvas != null && _playerCam != null)
            {
                float dist = Vector3.Distance(transform.position, _playerCam.position);
                promptCanvas.SetActive(dist < promptDistance * 3f);
                if (promptCanvas.activeSelf)
                    promptCanvas.transform.LookAt(_playerCam);
            }
        }
    }
}
