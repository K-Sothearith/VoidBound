using UnityEngine;
using Voidbound.Core;
using Voidbound.Player;

namespace Voidbound.Obstacles
{
    /// <summary>
    /// Applies a strong physical knockback impulse to the player upon hammer impact,
    /// pushing them sideways off the narrow suspended bridge and down into the void.
    /// Does not kill the player directly on contact.
    /// </summary>
    public class HammerKnockback : MonoBehaviour
    {
        [Header("Knockback Settings")]
        [Tooltip("Horizontal push speed imparted to the player.")]
        [SerializeField] private float horizontalKnockback = 22.0f;

        [Tooltip("Vertical lift speed to pop player off ground and clear small ledges.")]
        [SerializeField] private float upwardLift = 3.5f;

        [Tooltip("Cooldown in seconds between hits on the same player.")]
        [SerializeField] private float hitCooldown = 0.35f;

        public float HorizontalKnockback => horizontalKnockback;
        public float UpwardLift => upwardLift;

        private Vector3 lastPosition;
        private Vector3 currentVelocity;
        private float lastHitTime = -10f;

        private void Awake()
        {
            lastPosition = transform.position;
        }

        private void FixedUpdate()
        {
            if (Time.fixedDeltaTime > 0f)
            {
                currentVelocity = (transform.position - lastPosition) / Time.fixedDeltaTime;
            }
            lastPosition = transform.position;
        }

        private void OnTriggerEnter(Collider other)
        {
            ApplyPush(other.gameObject);
        }

        private void OnCollisionEnter(Collision collision)
        {
            ApplyPush(collision.gameObject);
        }

        public void ApplyPush(GameObject target)
        {
            if (Time.time - lastHitTime < hitCooldown)
            {
                return;
            }

            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying)
            {
                return;
            }

            PlayerController pc = target.GetComponent<PlayerController>();
            if (pc == null)
            {
                pc = target.GetComponentInParent<PlayerController>();
            }

            if (pc != null)
            {
                lastHitTime = Time.time;

                // Push direction prioritizes the hammer's horizontal swing trajectory
                Vector3 pushDir = new Vector3(currentVelocity.x, 0f, currentVelocity.z);

                if (pushDir.sqrMagnitude < 0.25f)
                {
                    // If moving slowly near apex, push radially away from hammer center
                    pushDir = target.transform.position - transform.position;
                    pushDir.y = 0f;
                }

                if (pushDir.sqrMagnitude > 0.001f)
                {
                    pushDir.Normalize();
                }
                else
                {
                    pushDir = Vector3.right;
                }

                Vector3 knockbackImpulse = pushDir * horizontalKnockback + Vector3.up * upwardLift;
                pc.AddKnockback(knockbackImpulse);
            }
        }
    }
}
