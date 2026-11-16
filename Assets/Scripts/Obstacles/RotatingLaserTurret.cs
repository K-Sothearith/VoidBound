using UnityEngine;
using Voidbound.Core;
using Voidbound.Player;

namespace Voidbound.Obstacles
{
    /// <summary>
    /// Rotating central laser turret. Shoots a continuous dual-ended beam in both directions
    /// and rotates at a constant speed in a loop. Never shuts off.
    /// The player must time jumps over the rotating beam while traversing the circular platform.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class RotatingLaserTurret : Hazard
    {
        [Header("Turret Rotation Settings")]
        [Tooltip("Rotation speed in degrees per second. Positive = clockwise, Negative = counter-clockwise.")]
        [SerializeField] private float rotationSpeed = 35.0f;
        [SerializeField] private float beamLength = 7.2f;
        [SerializeField] private float beamHeight = 0.6f;

        [Header("Components")]
        [SerializeField] private Transform rotatingHead;
        [SerializeField] private Renderer beamRenderer;
        [SerializeField] private Collider beamCollider;
        [SerializeField] private Light beamLight;

        private float currentAngle;
        private bool hasKilled;

        public float RotationSpeed => rotationSpeed;
        public float BeamLength => beamLength;
        public float BeamHeight => beamHeight;
        public float CurrentAngle => currentAngle;
        public bool HasKilled => hasKilled;

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnRespawnRequested += OnRespawnRequested;
                GameManager.Instance.OnGameReset += OnGameReset;
            }

            ResetTurret();
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnRespawnRequested -= OnRespawnRequested;
                GameManager.Instance.OnGameReset -= OnGameReset;
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
            currentAngle = (currentAngle + rotationSpeed * dt) % 360f;
            if (currentAngle < 0f) currentAngle += 360f;

            if (rotatingHead != null)
            {
                rotatingHead.localRotation = Quaternion.Euler(0f, currentAngle, 0f);
            }
            else
            {
                transform.rotation = Quaternion.Euler(0f, currentAngle, 0f);
            }
        }

        public void ResetTurret()
        {
            currentAngle = 0f;
            hasKilled = false;
            Tick(0f);
        }

        private void OnTriggerEnter(Collider other)
        {
            ProcessHit(other.gameObject);
        }

        public override void ProcessHit(GameObject target)
        {
            if (TryProcessHit(target)) hasKilled = true;
        }

        private void OnRespawnRequested(Vector3 position, Quaternion rotation)
        {
            hasKilled = false;
        }

        private void OnGameReset()
        {
            ResetTurret();
        }
    }
}
