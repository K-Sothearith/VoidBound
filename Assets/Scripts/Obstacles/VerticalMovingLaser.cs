using UnityEngine;
using Voidbound.Core;
using Voidbound.Player;

namespace Voidbound.Obstacles
{
    /// <summary>
    /// Vertical oscillating laser hazard. Continuously moves up and down between minHeight and maxHeight.
    /// Never shuts off. The player must either jump over it when low or run underneath it when high.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class VerticalMovingLaser : Hazard
    {
        [Header("Vertical Motion Settings")]
        [SerializeField] private float minHeight = 0.4f;
        [SerializeField] private float maxHeight = 2.4f;
        [SerializeField] private float cycleDuration = 3.2f;
        [SerializeField] private float phaseOffsetDegrees = 0.0f;

        [Header("Components")]
        [SerializeField] private Transform movingAssembly;
        [SerializeField] private Renderer beamRenderer;
        [SerializeField] private Collider beamCollider;
        [SerializeField] private Light beamLight;

        private float elapsedTime;
        private bool hasKilled;
        private Vector3 initialAssemblyLocalPos;
        private float initialZ;

        public float MinHeight => minHeight;
        public float MaxHeight => maxHeight;
        public float CycleDuration => cycleDuration;
        public float PhaseOffsetDegrees => phaseOffsetDegrees;
        public float CurrentHeight { get; private set; }
        public bool HasKilled => hasKilled;

        private void Awake()
        {
            if (movingAssembly != null)
            {
                initialAssemblyLocalPos = movingAssembly.localPosition;
            }
            initialZ = transform.position.z;
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnRespawnRequested += OnRespawnRequested;
                GameManager.Instance.OnGameReset += OnGameReset;
            }

            ResetLaser();
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
            elapsedTime += dt;
            float duration = Mathf.Max(0.1f, cycleDuration);
            float phaseRad = phaseOffsetDegrees * Mathf.Deg2Rad;
            float t = (Mathf.Sin((elapsedTime / duration) * Mathf.PI * 2.0f + phaseRad) + 1.0f) * 0.5f;

            CurrentHeight = Mathf.Lerp(minHeight, maxHeight, t);

            if (movingAssembly != null)
            {
                movingAssembly.localPosition = new Vector3(
                    initialAssemblyLocalPos.x,
                    CurrentHeight,
                    initialAssemblyLocalPos.z
                );
            }
            else
            {
                transform.position = new Vector3(transform.position.x, CurrentHeight, transform.position.z);
            }
        }

        public void ResetLaser()
        {
            elapsedTime = 0f;
            hasKilled = false;
            Tick(0f);
        }

        private void OnTriggerEnter(Collider other)
        {
            ProcessHit(other.gameObject);
        }

        public override void ProcessHit(GameObject target)
        {
            if (hasKilled) return;
            hasKilled = true;
            base.ProcessHit(target);
        }

        private void OnRespawnRequested(Vector3 position, Quaternion rotation)
        {
            hasKilled = false;
        }

        private void OnGameReset()
        {
            ResetLaser();
        }
    }
}
