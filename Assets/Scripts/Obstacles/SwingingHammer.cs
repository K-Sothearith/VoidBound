using UnityEngine;
using Voidbound.Core;

namespace Voidbound.Obstacles
{
    [RequireComponent(typeof(Rigidbody))]
    public class SwingingHammer : MonoBehaviour
    {
        [Header("Swing Dynamics")]
        [Tooltip("Maximum swing angle from center in degrees.")]
        [SerializeField] private float maxSwingAngle = 55.0f;

        [Tooltip("Time in seconds for one full back-and-forth oscillation.")]
        [SerializeField] private float swingPeriod = 2.8f;

        [Tooltip("Phase offset in degrees to stagger timing between multiple hammers.")]
        [SerializeField] private float phaseOffsetDegrees = 0.0f;

        [Tooltip("Local axis around which the hammer swings (typically Z to swing across the X-axis path).")]
        [SerializeField] private Vector3 swingAxis = Vector3.forward;

        [Header("Audio (Optional)")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip whooshSound;
        [SerializeField] private float whooshVolume = 0.5f;

        private Rigidbody rb;
        private Quaternion initialLocalRotation;
        private float accumulatedTime;
        private bool wasPositiveLastStep;

        public float MaxSwingAngle => maxSwingAngle;
        public float SwingPeriod => swingPeriod;
        public float PhaseOffsetDegrees => phaseOffsetDegrees;
        public float CurrentAngle { get; private set; }

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            initialLocalRotation = transform.localRotation;
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnRespawnRequested += OnCourseReset;
            }

            // Initialize to starting phase angle
            UpdateHammerRotation(0f);
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnRespawnRequested -= OnCourseReset;
            }
        }

        private void FixedUpdate()
        {
            // Only advance physics animation when game is actively playing and timescale > 0
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
            {
                return;
            }

            if (Time.timeScale <= 0f || swingPeriod <= 0f)
            {
                return;
            }

            accumulatedTime += Time.fixedDeltaTime;
            UpdateHammerRotation(accumulatedTime);
        }

        private void UpdateHammerRotation(float time)
        {
            float frequency = (2f * Mathf.PI) / swingPeriod;
            float phaseRad = phaseOffsetDegrees * Mathf.Deg2Rad;
            float sinVal = Mathf.Sin(frequency * time + phaseRad);

            CurrentAngle = sinVal * maxSwingAngle;
            Quaternion targetRotation = initialLocalRotation * Quaternion.AngleAxis(CurrentAngle, swingAxis);

            if (rb != null)
            {
                rb.MoveRotation(targetRotation);
            }
            else
            {
                transform.localRotation = targetRotation;
            }

            // Audio cue when crossing near center with high speed
            bool isPositive = sinVal >= 0f;
            if (isPositive != wasPositiveLastStep)
            {
                if (audioSource != null && whooshSound != null && audioSource.isActiveAndEnabled)
                {
                    audioSource.PlayOneShot(whooshSound, whooshVolume);
                }
                wasPositiveLastStep = isPositive;
            }
        }

        public void ResetHammer()
        {
            accumulatedTime = 0f;
            UpdateHammerRotation(0f);
        }

        private void OnCourseReset(Vector3 respawnPos, Quaternion respawnRot)
        {
            ResetHammer();
        }
    }
}
