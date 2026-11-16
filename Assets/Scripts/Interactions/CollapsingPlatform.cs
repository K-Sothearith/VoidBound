using UnityEngine;
using Voidbound.Core;
using Voidbound.Player;

namespace Voidbound.Interactions
{
    public enum CollapsingPlatformState
    {
        Idle,
        Warning,
        Collapsed
    }

    /// <summary>
    /// Section 7 Collapsing Platform:
    /// Warns briefly with a warning material flash and shake upon player contact,
    /// then collapses by disabling its solid collider and plunging downward.
    /// Resets automatically on player respawn or game restart.
    /// </summary>
    public class CollapsingPlatform : MonoBehaviour
    {
        [Header("Timing & Motion")]
        [Tooltip("Seconds before the platform collapses after player contact")]
        [SerializeField] private float warnDuration = 0.8f;
        [Tooltip("Downward fall speed when collapsed (m/s)")]
        [SerializeField] private float fallSpeed = 16.0f;
        [Tooltip("Lateral shake amplitude during warning")]
        [SerializeField] private float shakeIntensity = 0.05f;

        [Header("Components & Materials")]
        [SerializeField] private Collider solidCollider;
        [SerializeField] private Renderer platformRenderer;
        [SerializeField] private Material normalMaterial;
        [SerializeField] private Material warningMaterial;

        // Runtime state
        private CollapsingPlatformState currentState = CollapsingPlatformState.Idle;
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private float warningTimer;

        public CollapsingPlatformState CurrentState => currentState;
        public Vector3 InitialPosition => initialPosition;
        public float WarnDuration => warnDuration;
        public float FallSpeed => fallSpeed;
        public bool IsSolidColliderEnabled => solidCollider != null && solidCollider.enabled;

        private void Awake()
        {
            initialPosition = transform.position;
            initialRotation = transform.rotation;

            if (solidCollider == null)
            {
                // Find non-trigger collider
                Collider[] cols = GetComponents<Collider>();
                foreach (var c in cols)
                {
                    if (!c.isTrigger)
                    {
                        solidCollider = c;
                        break;
                    }
                }
            }

            if (platformRenderer == null)
            {
                platformRenderer = GetComponent<Renderer>();
            }

            if (normalMaterial == null && platformRenderer != null)
            {
                normalMaterial = platformRenderer.sharedMaterial;
            }
        }

        private void Start()
        {
            if (initialPosition == Vector3.zero)
            {
                initialPosition = transform.position;
                initialRotation = transform.rotation;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnRespawnRequested += HandleRespawn;
                GameManager.Instance.OnGameReset += ResetPlatform;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnRespawnRequested -= HandleRespawn;
                GameManager.Instance.OnGameReset -= ResetPlatform;
            }
        }

        private void Update()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
            {
                return;
            }

            Tick(Time.deltaTime);
        }

        public void Tick(float dt)
        {
            if (dt <= 0f) return;

            if (currentState == CollapsingPlatformState.Warning)
            {
                warningTimer -= dt;

                // Visual shake feedback during warning phase
                float shakeX = Mathf.Sin(warningTimer * 50f) * shakeIntensity;
                float shakeZ = Mathf.Cos(warningTimer * 45f) * shakeIntensity;
                transform.position = initialPosition + new Vector3(shakeX, 0f, shakeZ);

                if (warningTimer <= 0f)
                {
                    Collapse();
                }
            }
            else if (currentState == CollapsingPlatformState.Collapsed)
            {
                transform.position += Vector3.down * (fallSpeed * dt);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (currentState != CollapsingPlatformState.Idle) return;

            if (IsPlayer(other))
            {
                TriggerCollapse();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (currentState != CollapsingPlatformState.Idle) return;

            if (IsPlayer(collision.collider))
            {
                TriggerCollapse();
            }
        }

        private bool IsPlayer(Collider col)
        {
            return col.CompareTag("Player") ||
                   col.GetComponent<CharacterController>() != null ||
                   col.GetComponentInParent<PlayerController>() != null;
        }

        public void TriggerCollapse()
        {
            if (currentState != CollapsingPlatformState.Idle) return;
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;

            currentState = CollapsingPlatformState.Warning;
            warningTimer = warnDuration;

            if (platformRenderer != null && warningMaterial != null)
            {
                platformRenderer.sharedMaterial = warningMaterial;
            }
        }

        private void Collapse()
        {
            currentState = CollapsingPlatformState.Collapsed;

            if (solidCollider != null)
            {
                solidCollider.enabled = false;
            }
        }

        private void HandleRespawn(Vector3 pos, Quaternion rot)
        {
            ResetPlatform();
        }

        public void ResetPlatform()
        {
            currentState = CollapsingPlatformState.Idle;
            warningTimer = 0f;

            transform.position = initialPosition;
            transform.rotation = initialRotation;

            if (solidCollider != null)
            {
                solidCollider.enabled = true;
            }

            if (platformRenderer != null && normalMaterial != null)
            {
                platformRenderer.sharedMaterial = normalMaterial;
            }
        }

        public void Configure(Material normalMat, Material warnMat, float duration = 0.8f, float speed = 16.0f, float shake = 0.05f)
        {
            normalMaterial = normalMat;
            warningMaterial = warnMat;
            warnDuration = duration;
            fallSpeed = speed;
            shakeIntensity = shake;

            initialPosition = transform.position;
            initialRotation = transform.rotation;

            if (platformRenderer != null && normalMaterial != null)
            {
                platformRenderer.sharedMaterial = normalMaterial;
            }
        }
    }
}
