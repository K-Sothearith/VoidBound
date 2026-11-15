using System;
using System.Collections.Generic;
using UnityEngine;
using Voidbound.Core;
using Voidbound.Player;

namespace Voidbound.Interactions
{
    [RequireComponent(typeof(Collider))]
    public class PressurePlate : MonoBehaviour
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
        [SerializeField] private Color activeColor = new Color(0.1f, 0.95f, 1f, 1f);
        [SerializeField] private float inactiveLightIntensity = 0.5f;
        [SerializeField] private float activeLightIntensity = 2.5f;

        [Header("Audio (Optional)")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip pressSound;
        [SerializeField] private AudioClip releaseSound;

        // State
        private bool isPressed;
        private Vector3 unpressedLocalPos;
        private Vector3 pressedLocalPos;
        private readonly HashSet<Collider> occupants = new HashSet<Collider>();
        private Collider plateCollider;

        public bool IsPressed => isPressed;
        public float DepressDistance => depressDistance;
        public float DepressSpeed => depressSpeed;

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
            // Clean up any stale or destroyed colliders
            occupants.RemoveWhere(col => col == null || !col.enabled || !col.gameObject.activeInHierarchy);

            // Double check presence: if pressed, verify that at least one occupant is still over the plate
            if (isPressed && occupants.Count > 0 && plateCollider is BoxCollider boxCol)
            {
                Vector3 worldCenter = transform.TransformPoint(boxCol.center);
                Vector3 halfExtents = Vector3.Scale(boxCol.size, transform.lossyScale) * 0.5f;
                // Extend slightly vertically to detect jumping player directly above plate
                Vector3 checkCenter = worldCenter + Vector3.up * 0.2f;
                Vector3 checkExtents = halfExtents + new Vector3(0.05f, 0.5f, 0.05f);

                Collider[] hits = Physics.OverlapBox(checkCenter, checkExtents, transform.rotation, ~0, QueryTriggerInteraction.Collide);
                bool playerFound = false;
                foreach (var hit in hits)
                {
                    if (IsPlayerCollider(hit))
                    {
                        playerFound = true;
                        break;
                    }
                }

                if (!playerFound)
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
            if (IsPlayerCollider(other))
            {
                occupants.Add(other);
                if (!isPressed)
                {
                    SetPressedState(true);
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (IsPlayerCollider(other))
            {
                occupants.Remove(other);
                if (occupants.Count == 0 && isPressed)
                {
                    SetPressedState(false);
                }
            }
        }

        private bool IsPlayerCollider(Collider col)
        {
            if (col == null) return false;
            return col.CompareTag("Player") ||
                   col.GetComponentInParent<PlayerController>() != null ||
                   col.GetComponentInParent<CharacterController>() != null;
        }

        private void SetPressedState(bool pressed)
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
