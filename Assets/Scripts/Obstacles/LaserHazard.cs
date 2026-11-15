using System;
using UnityEngine;
using Voidbound.Core;
using Voidbound.Player;

namespace Voidbound.Obstacles
{
    public enum LaserState
    {
        Inactive,
        Warning,
        Active
    }

    /// <summary>
    /// Timed pulsing laser hazard. Cycles through Inactive, Warning, and lethal Active states.
    /// Supports phase offsets, alternating pairs, and sequential sweeping groups.
    /// </summary>
    public class LaserHazard : Hazard
    {
        [Header("State Durations (seconds)")]
        [SerializeField] private float inactiveDuration = 2.0f;
        [SerializeField] private float warningDuration = 1.0f;
        [SerializeField] private float activeDuration = 1.5f;
        [SerializeField] private float initialDelay = 0.0f;
        [SerializeField] private LaserState startingState = LaserState.Inactive;

        [Header("Components")]
        [SerializeField] private Renderer beamRenderer;
        [SerializeField] private Collider beamCollider;
        [SerializeField] private Light beamLight;
        [SerializeField] private Renderer[] emitterRenderers;

        [Header("Visual Feedback Colors")]
        [SerializeField] private Color inactiveColor = new Color(0.15f, 0.15f, 0.2f, 0.05f);
        [SerializeField] private Color warningColor = new Color(1.0f, 0.6f, 0.05f, 0.6f);
        [SerializeField] private Color activeColor = new Color(1.0f, 0.1f, 0.1f, 1.0f);

        [SerializeField] private float warningLightIntensity = 0.8f;
        [SerializeField] private float activeLightIntensity = 2.5f;

        private LaserState currentState;
        private float stateTimer;
        private float initialDelayTimer;
        private bool hasKilledThisCycle;

        public LaserState CurrentState => currentState;
        public float InactiveDuration { get => inactiveDuration; set => inactiveDuration = value; }
        public float WarningDuration { get => warningDuration; set => warningDuration = value; }
        public float ActiveDuration { get => activeDuration; set => activeDuration = value; }
        public float InitialDelay { get => initialDelay; set => initialDelay = value; }
        public LaserState StartingState { get => startingState; set => startingState = value; }
        public bool IsLethal => currentState == LaserState.Active;

        public event Action<LaserState> OnLaserStateChanged;

        private void Awake()
        {
            if (beamCollider != null)
            {
                beamCollider.isTrigger = true;
            }
            ResetLaser();
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnRespawnRequested += OnRespawnRequested;
                GameManager.Instance.OnGameReset += ResetLaser;
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnRespawnRequested -= OnRespawnRequested;
                GameManager.Instance.OnGameReset -= ResetLaser;
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

            // Handle initial startup delay if specified (for phase alignment)
            if (initialDelayTimer > 0f)
            {
                float consumed = Mathf.Min(dt, initialDelayTimer);
                initialDelayTimer -= consumed;
                dt -= consumed;

                if (initialDelayTimer <= 0f)
                {
                    SetState(startingState);
                }

                if (dt <= 0f)
                {
                    UpdateVisualsAndCollider();
                    return;
                }
            }

            int safetyIterations = 0;
            while (dt > 0f && safetyIterations < 20)
            {
                safetyIterations++;
                if (stateTimer > dt)
                {
                    stateTimer -= dt;
                    dt = 0f;
                }
                else
                {
                    dt -= stateTimer;
                    AdvanceToNextState();
                }
            }

            UpdateVisualsAndCollider();
        }

        private void AdvanceToNextState()
        {
            switch (currentState)
            {
                case LaserState.Inactive:
                    SetState(warningDuration > 0f ? LaserState.Warning : LaserState.Active);
                    break;

                case LaserState.Warning:
                    SetState(LaserState.Active);
                    break;

                case LaserState.Active:
                    SetState(LaserState.Inactive);
                    break;
            }
        }

        public void SetState(LaserState newState)
        {
            currentState = newState;
            hasKilledThisCycle = false;

            switch (currentState)
            {
                case LaserState.Inactive:
                    stateTimer = inactiveDuration;
                    break;
                case LaserState.Warning:
                    stateTimer = warningDuration;
                    break;
                case LaserState.Active:
                    stateTimer = activeDuration;
                    break;
            }

            UpdateVisualsAndCollider();
            OnLaserStateChanged?.Invoke(currentState);
        }

        private void UpdateVisualsAndCollider()
        {
            // Lethality collider is strictly active ONLY when state is Active
            if (beamCollider != null)
            {
                beamCollider.enabled = (currentState == LaserState.Active);
            }

            // Beam Visuals
            if (beamRenderer != null)
            {
                if (currentState == LaserState.Inactive)
                {
                    beamRenderer.enabled = false;
                }
                else
                {
                    beamRenderer.enabled = true;
                    Material mat = beamRenderer.material;
                    if (mat != null)
                    {
                        Color c = (currentState == LaserState.Active) ? activeColor : warningColor;
                        mat.color = c;
                        if (mat.HasProperty("_EmissionColor"))
                        {
                            mat.SetColor("_EmissionColor", c * (currentState == LaserState.Active ? 2.5f : 1.2f));
                            mat.EnableKeyword("_EMISSION");
                        }
                    }
                }
            }

            // Central Beam Light
            if (beamLight != null)
            {
                if (currentState == LaserState.Inactive)
                {
                    beamLight.enabled = false;
                }
                else
                {
                    beamLight.enabled = true;
                    beamLight.color = (currentState == LaserState.Active) ? activeColor : warningColor;
                    beamLight.intensity = (currentState == LaserState.Active) ? activeLightIntensity : warningLightIntensity;
                }
            }

            // Side Emitters
            if (emitterRenderers != null && emitterRenderers.Length > 0)
            {
                Color emitterColor = currentState == LaserState.Active ? activeColor
                                  : currentState == LaserState.Warning ? warningColor
                                  : new Color(0.2f, 0.2f, 0.25f, 1f);

                for (int i = 0; i < emitterRenderers.Length; i++)
                {
                    if (emitterRenderers[i] != null && emitterRenderers[i].material != null)
                    {
                        emitterRenderers[i].material.color = emitterColor;
                        if (emitterRenderers[i].material.HasProperty("_EmissionColor"))
                        {
                            if (currentState != LaserState.Inactive)
                            {
                                emitterRenderers[i].material.SetColor("_EmissionColor", emitterColor * 1.5f);
                                emitterRenderers[i].material.EnableKeyword("_EMISSION");
                            }
                            else
                            {
                                emitterRenderers[i].material.DisableKeyword("_EMISSION");
                            }
                        }
                    }
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            ProcessHit(other.gameObject);
        }

        private void OnTriggerStay(Collider other)
        {
            ProcessHit(other.gameObject);
        }

        public override void ProcessHit(GameObject target)
        {
            if (currentState != LaserState.Active) return;
            if (hasKilledThisCycle) return;
            hasKilledThisCycle = true;
            base.ProcessHit(target);
        }

        private void OnRespawnRequested(Vector3 position, Quaternion rotation)
        {
            ResetLaser();
        }

        public void ResetLaser()
        {
            hasKilledThisCycle = false;
            initialDelayTimer = initialDelay;

            if (initialDelayTimer > 0f)
            {
                currentState = LaserState.Inactive;
                stateTimer = initialDelayTimer;
            }
            else
            {
                currentState = startingState;
                switch (currentState)
                {
                    case LaserState.Inactive:
                        stateTimer = inactiveDuration;
                        break;
                    case LaserState.Warning:
                        stateTimer = warningDuration;
                        break;
                    case LaserState.Active:
                        stateTimer = activeDuration;
                        break;
                }
            }

            UpdateVisualsAndCollider();
        }
    }
}
