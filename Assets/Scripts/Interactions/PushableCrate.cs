using UnityEngine;
using Voidbound.Core;

namespace Voidbound.Interactions
{
    /// <summary>
    /// Pushable Rigidbody crate for Section 6 physics chamber puzzle.
    /// Handles player pushing forces, void fall recovery, and level resets.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(BoxCollider))]
    public class PushableCrate : MonoBehaviour
    {
        [Header("Physics Tuning")]
        [Tooltip("Multiplier applied to player speed when pushing.")]
        [SerializeField] private float pushSpeedMultiplier = 0.5f;
        [Tooltip("Minimum horizontal velocity when pushed.")]
        [SerializeField] private float minPushSpeed = 1.8f;
        [Tooltip("Maximum horizontal velocity when pushed.")]
        [SerializeField] private float maxPushSpeed = 3.2f;
        [Tooltip("Y-position below which crate is considered lost to the void and reset.")]
        [SerializeField] private float fallResetThreshold = -5.0f;

        [Header("Audio (Optional)")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip pushSound;

        private Rigidbody rb;
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private float lastPushTime;

        public Vector3 InitialPosition => initialPosition;
        public Quaternion InitialRotation => initialRotation;
        public Rigidbody Rigidbody => rb;
        public float FallResetThreshold => fallResetThreshold;

        private void Awake()
        {
            InitializeCrate();
        }

        public void InitializeCrate()
        {
            if (rb == null) rb = GetComponent<Rigidbody>();
            initialPosition = transform.position;
            initialRotation = transform.rotation;

            // Ensure stable collision detection and interpolation for smooth physics
            if (rb != null)
            {
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
            }
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameReset += ResetPosition;
                GameManager.Instance.OnRespawnRequested += HandlePlayerRespawn;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameReset -= ResetPosition;
                GameManager.Instance.OnRespawnRequested -= HandlePlayerRespawn;
            }
        }

        private void FixedUpdate()
        {
            // Void fall recovery: automatically resets crate if it falls into the void
            if (transform.position.y < fallResetThreshold)
            {
                ResetPosition();
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.ShowNotification("Lost crate recovered to origin.", 2.0f);
                }
            }
        }

        /// <summary>
        /// Called when the player's CharacterController collides with the crate.
        /// </summary>
        public void HandlePush(ControllerColliderHit hit, float playerSpeed)
        {
            if (rb == null) InitializeCrate();
            if (rb == null || rb.isKinematic) return;

            Vector3 pushDir = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z);
            if (pushDir.sqrMagnitude < 0.01f) return;
            pushDir.Normalize();

            float targetSpeed = Mathf.Clamp(playerSpeed * pushSpeedMultiplier, minPushSpeed, maxPushSpeed);
            rb.linearVelocity = new Vector3(pushDir.x * targetSpeed, rb.linearVelocity.y, pushDir.z * targetSpeed);
            lastPushTime = Time.time;

            if (audioSource != null && pushSound != null && !audioSource.isPlaying)
            {
                audioSource.PlayOneShot(pushSound, 0.5f);
            }
        }

        /// <summary>
        /// Directly applies horizontal push velocity (used by tests and programmatic interactions).
        /// </summary>
        public void ApplyPushForce(Vector3 direction, float speed)
        {
            if (rb == null) InitializeCrate();
            if (rb == null || rb.isKinematic) return;
            Vector3 pushDir = new Vector3(direction.x, 0f, direction.z).normalized;
            rb.linearVelocity = new Vector3(pushDir.x * speed, rb.linearVelocity.y, pushDir.z * speed);
        }

        /// <summary>
        /// Resets the crate to its initial position and rotation, zeroing all velocities.
        /// </summary>
        public void ResetPosition()
        {
            if (rb == null) InitializeCrate();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                // Reset the physics pose too: interpolation can overwrite a Transform-only teleport.
                rb.position = initialPosition;
                rb.rotation = initialRotation;
            }

            transform.SetPositionAndRotation(initialPosition, initialRotation);
        }

        private void HandlePlayerRespawn(Vector3 pos, Quaternion rot)
        {
            ResetPosition();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<KillVolume>() != null)
            {
                ResetPosition();
            }
        }
    }
}
