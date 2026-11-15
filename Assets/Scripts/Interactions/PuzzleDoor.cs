using UnityEngine;
using Voidbound.Core;

namespace Voidbound.Interactions
{
    /// <summary>
    /// Locked exit door for Section 6 Physics Chamber.
    /// Operates dual sliding blast doors that unlock ONLY when both crate pressure plates are active simultaneously.
    /// </summary>
    public class PuzzleDoor : MonoBehaviour
    {
        [Header("Pressure Plates")]
        [SerializeField] private CratePressurePlate plateLeft;
        [SerializeField] private CratePressurePlate plateRight;

        [Header("Door Leaves (Dual Sliding Blast Doors)")]
        [SerializeField] private Transform doorLeafLeft;
        [SerializeField] private Transform doorLeafRight;
        [SerializeField] private Collider doorColliderLeft;
        [SerializeField] private Collider doorColliderRight;

        [Header("Door Status Indicator")]
        [SerializeField] private Renderer statusRenderer;
        [SerializeField] private Light statusLight;
        [SerializeField] private Color lockedColor = new Color(1.0f, 0.15f, 0.15f, 1f); // Red
        [SerializeField] private Color unlockedColor = new Color(0.1f, 0.95f, 0.4f, 1f); // Neon Green

        [Header("Motion Settings")]
        [SerializeField] private float slideDistance = 3.0f;
        [SerializeField] private float slideSpeed = 3.5f;
        [SerializeField] private bool stayOpenOnceUnlocked = true;

        [Header("Audio (Optional)")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip unlockSound;

        // State
        private bool isUnlocked;
        private bool isOpen;
        private Vector3 leftClosedPos;
        private Vector3 leftOpenPos;
        private Vector3 rightClosedPos;
        private Vector3 rightOpenPos;

        public bool IsUnlocked => isUnlocked;
        public bool IsOpen => isOpen;
        public CratePressurePlate PlateLeft => plateLeft;
        public CratePressurePlate PlateRight => plateRight;
        public bool StayOpenOnceUnlocked { get => stayOpenOnceUnlocked; set => stayOpenOnceUnlocked = value; }

        private void Awake()
        {
            InitializeDoorPositions();
            UpdateVisuals(false);
        }

        public void InitializeDoorPositions()
        {
            if (doorLeafLeft != null)
            {
                leftClosedPos = doorLeafLeft.localPosition;
                leftOpenPos = leftClosedPos + Vector3.left * slideDistance;
            }
            if (doorLeafRight != null)
            {
                rightClosedPos = doorLeafRight.localPosition;
                rightOpenPos = rightClosedPos + Vector3.right * slideDistance;
            }
        }

        private void Start()
        {
            if (plateLeft != null)
            {
                plateLeft.OnStateChanged += OnPlateStateChanged;
            }
            if (plateRight != null)
            {
                plateRight.OnStateChanged += OnPlateStateChanged;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameReset += ResetDoor;
                GameManager.Instance.OnRespawnRequested += HandlePlayerRespawn;
            }

            CheckPlateConditions();
        }

        private void OnDestroy()
        {
            if (plateLeft != null)
            {
                plateLeft.OnStateChanged -= OnPlateStateChanged;
            }
            if (plateRight != null)
            {
                plateRight.OnStateChanged -= OnPlateStateChanged;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameReset -= ResetDoor;
                GameManager.Instance.OnRespawnRequested -= HandlePlayerRespawn;
            }
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Tick(float dt)
        {
            if (leftOpenPos == leftClosedPos)
            {
                InitializeDoorPositions();
            }

            Vector3 targetLeft = isOpen ? leftOpenPos : leftClosedPos;
            Vector3 targetRight = isOpen ? rightOpenPos : rightClosedPos;

            if (doorLeafLeft != null && dt > 0f)
            {
                doorLeafLeft.localPosition = Vector3.MoveTowards(doorLeafLeft.localPosition, targetLeft, slideSpeed * dt);
            }
            if (doorLeafRight != null && dt > 0f)
            {
                doorLeafRight.localPosition = Vector3.MoveTowards(doorLeafRight.localPosition, targetRight, slideSpeed * dt);
            }

            // Disable door colliders when fully open so player passes through smoothly
            bool fullyOpen = doorLeafLeft != null && Vector3.Distance(doorLeafLeft.localPosition, leftOpenPos) < 0.05f;
            if (doorColliderLeft != null) doorColliderLeft.enabled = !fullyOpen;
            if (doorColliderRight != null) doorColliderRight.enabled = !fullyOpen;
        }

        private void OnPlateStateChanged(bool _)
        {
            CheckPlateConditions();
        }

        public void CheckPlateConditions()
        {
            bool bothActive = plateLeft != null && plateLeft.IsPressed &&
                              plateRight != null && plateRight.IsPressed;

            if (bothActive)
            {
                if (!isUnlocked)
                {
                    UnlockDoor();
                }
            }
            else
            {
                if (!stayOpenOnceUnlocked && isUnlocked)
                {
                    LockDoor();
                }
            }
        }

        public void UnlockDoor()
        {
            isUnlocked = true;
            isOpen = true;
            UpdateVisuals(true);

            if (audioSource != null && unlockSound != null)
            {
                audioSource.PlayOneShot(unlockSound, 0.8f);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.ShowNotification("Chamber Unlocked — Exit Access Granted", 3.0f);
            }
        }

        public void LockDoor()
        {
            isUnlocked = false;
            isOpen = false;
            UpdateVisuals(false);

            if (doorColliderLeft != null) doorColliderLeft.enabled = true;
            if (doorColliderRight != null) doorColliderRight.enabled = true;
        }

        public void ResetDoor()
        {
            LockDoor();
            if (doorLeafLeft != null) doorLeafLeft.localPosition = leftClosedPos;
            if (doorLeafRight != null) doorLeafRight.localPosition = rightClosedPos;
        }

        private void HandlePlayerRespawn(Vector3 pos, Quaternion rot)
        {
            ResetDoor();
        }

        private void UpdateVisuals(bool unlocked)
        {
            Color targetColor = unlocked ? unlockedColor : lockedColor;

            if (statusRenderer != null && statusRenderer.material != null)
            {
                statusRenderer.material.color = targetColor;
                if (statusRenderer.material.HasProperty("_EmissionColor"))
                {
                    statusRenderer.material.SetColor("_EmissionColor", targetColor * 2.5f);
                    statusRenderer.material.EnableKeyword("_EMISSION");
                }
            }

            if (statusLight != null)
            {
                statusLight.color = targetColor;
                statusLight.intensity = unlocked ? 2.5f : 1.2f;
            }
        }
    }
}
