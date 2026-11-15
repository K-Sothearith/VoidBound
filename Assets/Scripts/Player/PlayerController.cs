using UnityEngine;
using UnityEngine.InputSystem;
using Voidbound.Core;

namespace Voidbound.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float walkSpeed = 6.0f;
        [SerializeField] private float sprintSpeed = 9.5f;
        [Tooltip("Jump apex height in meters. 1.8m gives a comfortable, athletic jump arc.")]
        [SerializeField] private float jumpHeight = 1.8f;
        [Tooltip("Platforming gravity for jumps above platforms.")]
        [SerializeField] private float gravity = -20.0f;
        [Tooltip("Accelerated gravity applied when the player falls into the void below platform level.")]
        [SerializeField] private float voidGravity = -45.0f;
        [SerializeField] private float maxFallSpeed = -50.0f;

        [Header("Look Settings")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float mouseSensitivity = 0.15f;
        [SerializeField] private float minPitch = -89.0f;
        [SerializeField] private float maxPitch = 89.0f;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip jumpSound;
        [SerializeField] private AudioClip landSound;

        [Header("Knockback Settings")]
        [SerializeField] private float knockbackDecay = 7.0f;

        // References & State
        private CharacterController characterController;
        private Vector3 velocity;
        private Vector3 knockbackVelocity;
        private float pitch;
        private bool isGrounded;
        private bool wasGroundedLastFrame;
        private bool isInputEnabled = true;

        // Platform Riding State
        private Voidbound.Interactions.MovingPlatform currentMovingPlatform;
        private Voidbound.Interactions.MovingPlatform activePlatform;
        private float lastPlatformHitTime;
        private Vector3 lastPlatformPos;

        public bool IsGrounded => isGrounded;
        public Vector3 CurrentVelocity => characterController != null ? characterController.velocity : Vector3.zero;
        public Vector3 CurrentKnockback => knockbackVelocity;

        public void AddKnockback(Vector3 force)
        {
            knockbackVelocity = new Vector3(force.x, 0f, force.z);
            if (force.y > 0f)
            {
                velocity.y = force.y;
                isGrounded = false;
            }
        }

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            if (cameraTransform == null)
            {
                Camera cam = GetComponentInChildren<Camera>();
                if (cam != null) cameraTransform = cam.transform;
            }

            // Ensure smooth jump traversal if serialized values in scene were extreme
            if (gravity < -28.0f || gravity > -12.0f)
            {
                gravity = -20.0f;
            }
            if (jumpHeight < 1.7f)
            {
                jumpHeight = 1.8f;
            }
            if (voidGravity > -30.0f)
            {
                voidGravity = -45.0f;
            }
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RegisterSpawnPoint(transform.position, transform.rotation);
                GameManager.Instance.OnRespawnRequested += OnRespawnRequested;
                GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnRespawnRequested -= OnRespawnRequested;
                GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
            }
        }

        private void Update()
        {
            bool canControl = isInputEnabled && GameManager.Instance != null && GameManager.Instance.IsPlaying;

            if (canControl)
            {
                HandleLook();
                HandleMovement();
            }
            else
            {
                // Apply gravity even when control is restricted to keep player grounded
                ApplyGravityOnly();
            }
        }

        private void HandleLook()
        {
            if (Mouse.current == null) return;

            Vector2 mouseDelta = Mouse.current.delta.ReadValue() * mouseSensitivity;

            // Horizontal rotation (yaw) turns the player body
            transform.Rotate(Vector3.up * mouseDelta.x);

            // Vertical rotation (pitch) tilts the camera
            if (cameraTransform != null)
            {
                pitch -= mouseDelta.y;
                pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
                cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
        }

        private void HandleMovement()
        {
            // Reliable native CharacterController ground check (no self-colliding queries)
            isGrounded = characterController.isGrounded;

            // Moving Platform Tracking
            Voidbound.Interactions.MovingPlatform standingPlatform = null;
            if (isGrounded && activePlatform != null && (Time.time - lastPlatformHitTime) < 0.15f)
            {
                standingPlatform = activePlatform;
            }
            else if (!isGrounded)
            {
                activePlatform = null;
            }

            Vector3 platformDelta = Vector3.zero;
            if (standingPlatform != null)
            {
                if (currentMovingPlatform != standingPlatform)
                {
                    currentMovingPlatform = standingPlatform;
                    lastPlatformPos = standingPlatform.transform.position;
                    standingPlatform.SetRider(characterController);
                }
                else
                {
                    platformDelta = standingPlatform.transform.position - lastPlatformPos;
                    lastPlatformPos = standingPlatform.transform.position;
                }
            }
            else
            {
                if (currentMovingPlatform != null)
                {
                    currentMovingPlatform.RemoveRider(characterController);
                    currentMovingPlatform = null;
                }
            }

            if (isGrounded && velocity.y < 0f)
            {
                // Small downward force to stay glued to surfaces
                velocity.y = -2f;
                if (!wasGroundedLastFrame && landSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(landSound, 0.4f);
                }
            }

            Vector2 inputVector = Vector2.zero;
            bool isSprinting = false;
            bool jumpPressed = false;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed) inputVector.y += 1f;
                if (Keyboard.current.sKey.isPressed) inputVector.y -= 1f;
                if (Keyboard.current.aKey.isPressed) inputVector.x -= 1f;
                if (Keyboard.current.dKey.isPressed) inputVector.x += 1f;
                inputVector.Normalize();

                isSprinting = Keyboard.current.leftShiftKey.isPressed;
                jumpPressed = Keyboard.current.spaceKey.wasPressedThisFrame;
            }

            // Calculate movement vector relative to player facing direction
            float targetSpeed = isSprinting ? sprintSpeed : walkSpeed;
            Vector3 moveDirection = (transform.forward * inputVector.y + transform.right * inputVector.x).normalized;
            Vector3 horizontalMovement = (moveDirection * targetSpeed + knockbackVelocity) * Time.deltaTime;

            // Damp knockback over time
            if (knockbackVelocity.sqrMagnitude > 0.01f)
            {
                knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, knockbackDecay * Time.deltaTime);
            }
            else
            {
                knockbackVelocity = Vector3.zero;
            }

            // Handle Jump: strictly requires isGrounded and velocity.y <= 0.1f (prevents any mid-air jump spam)
            if (jumpPressed && isGrounded && velocity.y <= 0.1f)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2.0f * gravity);
                isGrounded = false;
                if (jumpSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(jumpSound, 0.6f);
                }
            }

            // Apply gravity: comfortable platforming arc above platforms, fast plunge below platform level
            float activeGravity = gravity;
            if (!isGrounded && velocity.y < 0f && transform.position.y < -0.3f)
            {
                activeGravity = voidGravity;
            }
            velocity.y += activeGravity * Time.deltaTime;
            if (velocity.y < maxFallSpeed)
            {
                velocity.y = maxFallSpeed;
            }

            // Single unified Move call per frame for rock-solid stability and zero sliding
            Vector3 totalMovement = horizontalMovement + platformDelta + Vector3.up * (velocity.y * Time.deltaTime);
            characterController.Move(totalMovement);

            wasGroundedLastFrame = isGrounded;
        }

        private void ApplyGravityOnly()
        {
            if (characterController.enabled)
            {
                if (characterController.isGrounded && velocity.y < 0)
                {
                    velocity.y = -2f;
                }
                float activeGravity = gravity;
                if (!characterController.isGrounded && velocity.y < 0f && transform.position.y < -0.3f)
                {
                    activeGravity = voidGravity;
                }
                velocity.y += activeGravity * Time.deltaTime;
                if (velocity.y < maxFallSpeed)
                {
                    velocity.y = maxFallSpeed;
                }
                characterController.Move(velocity * Time.deltaTime);
            }
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            if (characterController != null)
            {
                characterController.enabled = false;
            }

            transform.position = position;
            transform.rotation = Quaternion.Euler(0f, rotation.eulerAngles.y, 0f);

            pitch = 0f;
            if (cameraTransform != null)
            {
                cameraTransform.localRotation = Quaternion.identity;
            }

            velocity = Vector3.zero;
            knockbackVelocity = Vector3.zero;
            isGrounded = false;
            activePlatform = null;
            currentMovingPlatform = null;

            if (characterController != null)
            {
                characterController.enabled = true;
            }
        }

        public void SetInputEnabled(bool enabled)
        {
            isInputEnabled = enabled;
            if (!enabled)
            {
                velocity = Vector3.zero;
                knockbackVelocity = Vector3.zero;
            }
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            // Detect moving platform beneath feet (upward collision normal)
            if (hit.normal.y > 0.5f)
            {
                var mp = hit.collider.GetComponentInParent<Voidbound.Interactions.MovingPlatform>();
                if (mp != null)
                {
                    activePlatform = mp;
                    lastPlatformHitTime = Time.time;
                }
            }

            var hammerKnockback = hit.collider.GetComponent<Voidbound.Obstacles.HammerKnockback>();
            if (hammerKnockback == null)
            {
                hammerKnockback = hit.collider.GetComponentInParent<Voidbound.Obstacles.HammerKnockback>();
            }

            if (hammerKnockback != null)
            {
                hammerKnockback.ApplyPush(gameObject);
            }
        }

        private void OnRespawnRequested(Vector3 targetPosition, Quaternion targetRotation)
        {
            Teleport(targetPosition, targetRotation);
        }

        private void OnGameStateChanged(GameState state)
        {
            if (state == GameState.Playing)
            {
                isInputEnabled = true;
            }
            else
            {
                velocity = Vector3.zero;
                knockbackVelocity = Vector3.zero;
            }
        }
    }
}
