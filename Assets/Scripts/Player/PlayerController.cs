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
        [SerializeField] private float jumpHeight = 1.6f;
        [SerializeField] private float gravity = -20.0f;
        [SerializeField] private float groundCheckOffset = 0.1f;

        [Header("Look Settings")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float mouseSensitivity = 0.15f;
        [SerializeField] private float minPitch = -89.0f;
        [SerializeField] private float maxPitch = 89.0f;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip jumpSound;
        [SerializeField] private AudioClip landSound;

        // References & State
        private CharacterController characterController;
        private Vector3 velocity;
        private float pitch;
        private bool isGrounded;
        private bool wasGroundedLastFrame;
        private bool isInputEnabled = true;

        public bool IsGrounded => isGrounded;
        public Vector3 CurrentVelocity => characterController != null ? characterController.velocity : Vector3.zero;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            if (cameraTransform == null)
            {
                Camera cam = GetComponentInChildren<Camera>();
                if (cam != null) cameraTransform = cam.transform;
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
            isGrounded = characterController.isGrounded || Physics.CheckSphere(transform.position + Vector3.up * groundCheckOffset, characterController.radius * 0.85f, ~0, QueryTriggerInteraction.Ignore);
            if (isGrounded && velocity.y < 0)
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
            characterController.Move(moveDirection * targetSpeed * Time.deltaTime);

            // Handle Jump
            if (jumpPressed && isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2.0f * gravity);
                if (jumpSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(jumpSound, 0.6f);
                }
            }

            // Apply gravity
            velocity.y += gravity * Time.deltaTime;
            characterController.Move(velocity * Time.deltaTime);

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
                velocity.y += gravity * Time.deltaTime;
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
            }
        }
    }
}
