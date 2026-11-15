using System;
using System.Collections.Generic;
using UnityEngine;
using Voidbound.Core;

namespace Voidbound.Interactions
{
    /// <summary>
    /// Pressure plate specifically calibrated to detect PushableCrates in Section 6.
    /// Drives depression animation, emissive glow, and state change events for puzzle logic.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class CratePressurePlate : MonoBehaviour
    {
        [Header("Plate References")]
        [SerializeField] private Transform platePad;
        [SerializeField] private Renderer plateRenderer;
        [SerializeField] private Light plateLight;

        [Header("Depression Animation")]
        [SerializeField] private float depressDistance = 0.08f;
        [SerializeField] private float depressSpeed = 8.0f;

        [Header("Visual Colors")]
        [SerializeField] private Color inactiveColor = new Color(0.15f, 0.45f, 0.6f, 1f);
        [SerializeField] private Color activeColor = new Color(0.1f, 0.95f, 0.4f, 1f); // Vibrant emerald-cyan
        [SerializeField] private float inactiveLightIntensity = 0.5f;
        [SerializeField] private float activeLightIntensity = 2.5f;

        [Header("Audio (Optional)")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip pressSound;
        [SerializeField] private AudioClip releaseSound;

        [Header("Crate Filter")]
        [Tooltip("Optional designated crate. If null, any PushableCrate activates the plate.")]
        [SerializeField] private PushableCrate designatedCrate;

        // State
        private bool isPressed;
        private Vector3 unpressedLocalPos;
        private Vector3 pressedLocalPos;
        private readonly HashSet<PushableCrate> occupants = new HashSet<PushableCrate>();
        private Collider plateCollider;

        public bool IsPressed => isPressed;
        public float DepressDistance => depressDistance;
        public float DepressSpeed => depressSpeed;
        public PushableCrate DesignatedCrate => designatedCrate;

        public event Action<bool> OnStateChanged;
        public event Action OnPressed;
        public event Action OnReleased;

        private void Awake()
        {
            plateCollider = GetComponent<Collider>();
            if (plateCollider != null)
            {
                plateCollider.isTrigger = true;
            }

            if (platePad != null)
            {
                unpressedLocalPos = platePad.localPosition;
                pressedLocalPos = unpressedLocalPos - Vector3.up * depressDistance;
            }

            UpdateVisuals(false, true);
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameReset += ResetPlate;
                GameManager.Instance.OnRespawnRequested += HandlePlayerRespawn;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameReset -= ResetPlate;
                GameManager.Instance.OnRespawnRequested -= HandlePlayerRespawn;
            }
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Tick(float dt)
        {
            // Remove any null or inactive occupants
            occupants.RemoveWhere(crate => crate == null || !crate.gameObject.activeInHierarchy);

            // Double check physical presence over the plate pad
            if (isPressed && occupants.Count > 0 && plateCollider is BoxCollider boxCol)
            {
                Vector3 worldCenter = transform.TransformPoint(boxCol.center);
                Vector3 halfExtents = Vector3.Scale(boxCol.size, transform.lossyScale) * 0.5f;
                Vector3 checkCenter = worldCenter + Vector3.up * 0.3f;
                Vector3 checkExtents = halfExtents + new Vector3(0.05f, 0.6f, 0.05f);

                Collider[] hits = Physics.OverlapBox(checkCenter, checkExtents, transform.rotation, ~0, QueryTriggerInteraction.Collide);
                bool validFound = false;
                foreach (var hit in hits)
                {
                    var crate = hit.GetComponentInParent<PushableCrate>();
                    if (crate != null && (designatedCrate == null || crate == designatedCrate))
                    {
                        validFound = true;
                        break;
                    }
                }

                if (!validFound)
                {
                    occupants.Clear();
                }
            }

            bool shouldBePressed = occupants.Count > 0;
            if (shouldBePressed != isPressed)
            {
                SetPressedState(shouldBePressed);
            }

            // Animate physical depression of the pad
            if (platePad != null && dt > 0f)
            {
                Vector3 targetLocalPos = isPressed ? pressedLocalPos : unpressedLocalPos;
                platePad.localPosition = Vector3.MoveTowards(platePad.localPosition, targetLocalPos, depressSpeed * dt);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            var crate = other.GetComponentInParent<PushableCrate>();
            if (crate != null)
            {
                if (designatedCrate == null || crate == designatedCrate)
                {
                    occupants.Add(crate);
                    if (!isPressed)
                    {
                        SetPressedState(true);
                    }
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var crate = other.GetComponentInParent<PushableCrate>();
            if (crate != null)
            {
                occupants.Remove(crate);
                if (occupants.Count == 0 && isPressed)
                {
                    SetPressedState(false);
                }
            }
        }

        public void SetPressedState(bool pressed)
        {
            isPressed = pressed;
            UpdateVisuals(pressed, false);

            if (audioSource != null)
            {
                AudioClip clip = pressed ? pressSound : releaseSound;
                if (clip != null) audioSource.PlayOneShot(clip, 0.7f);
            }

            OnStateChanged?.Invoke(pressed);
            if (pressed) OnPressed?.Invoke();
            else OnReleased?.Invoke();
        }

        private void HandlePlayerRespawn(Vector3 pos, Quaternion rot)
        {
            ResetPlate();
        }

        public void ResetPlate()
        {
            occupants.Clear();
            isPressed = false;
            if (platePad != null)
            {
                platePad.localPosition = unpressedLocalPos;
            }
            UpdateVisuals(false, true);
            OnStateChanged?.Invoke(false);
        }

        private void UpdateVisuals(bool active, bool immediate)
        {
            Color targetColor = active ? activeColor : inactiveColor;

            if (plateRenderer != null && plateRenderer.material != null)
            {
                plateRenderer.material.color = targetColor;
                if (plateRenderer.material.HasProperty("_EmissionColor"))
                {
                    plateRenderer.material.SetColor("_EmissionColor", active ? activeColor * 2.5f : activeColor * 0.2f);
                    plateRenderer.material.EnableKeyword("_EMISSION");
                }
            }

            if (plateLight != null)
            {
                plateLight.color = targetColor;
                plateLight.intensity = active ? activeLightIntensity : inactiveLightIntensity;
            }
        }
    }
}
