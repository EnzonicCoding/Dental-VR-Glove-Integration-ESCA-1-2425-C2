using System.Collections;
using UnityEngine;

namespace DentalVR.Dental
{
    /// <summary>
    /// Generates realistic force-feedback patterns for the dental drill tool.
    /// When the drill is active (spinning / in contact with a tooth surface) the
    /// holding hand receives a continuous vibration-like haptic pattern that
    /// varies with drill speed and applied pressure.
    ///
    /// Attach to the same GameObject as <see cref="DentalToolInteraction"/>.
    /// </summary>
    [RequireComponent(typeof(DentalToolInteraction))]
    public class DentalDrillHaptics : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Drill State")]
        [Tooltip("Is the drill currently spinning?")]
        [SerializeField] private bool isDrillActive;

        [Tooltip("Normalised drill speed (0–1). Can be driven by an animation or input event.")]
        [Range(0f, 1f)]
        [SerializeField] private float drillSpeed = 0.5f;

        [Header("Haptic Pattern")]
        [Tooltip("Base pulse strength when the drill is running at full speed (0–1).")]
        [SerializeField] private float maxPulseStrength = 0.6f;

        [Tooltip("Interval (seconds) between haptic pulses at maximum speed.")]
        [SerializeField] private float minPulseInterval = 0.04f;

        [Tooltip("Interval (seconds) between haptic pulses at minimum speed.")]
        [SerializeField] private float maxPulseInterval = 0.15f;

        [Header("Contact Feedback")]
        [Tooltip("Additional strength added when the drill contacts a tooth surface.")]
        [SerializeField] private float contactStrengthBonus = 0.3f;

        [Tooltip("Duration of the contact haptic burst (seconds).")]
        [SerializeField] private float contactBurstDuration = 0.2f;

        // ── Internal state ────────────────────────────────────────────────────
        private DentalToolInteraction toolInteraction;
        private bool isContactActive;
        private Coroutine pulseCoroutine;

        // ── Unity lifecycle ───────────────────────────────────────────────────
        private void Awake()
        {
            toolInteraction = GetComponent<DentalToolInteraction>();
        }

        private void OnEnable()
        {
            toolInteraction.OnToolGrabbed  += OnGrabbed;
            toolInteraction.OnToolReleased += OnReleased;
        }

        private void OnDisable()
        {
            toolInteraction.OnToolGrabbed  -= OnGrabbed;
            toolInteraction.OnToolReleased -= OnReleased;
            StopDrillPulse();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Start or stop the drill motor.</summary>
        public void SetDrillActive(bool active)
        {
            isDrillActive = active;

            if (active)
                StartDrillPulse();
            else
                StopDrillPulse();
        }

        /// <summary>Set the normalised drill speed (0–1).</summary>
        public void SetDrillSpeed(float speed)
        {
            drillSpeed = Mathf.Clamp01(speed);
        }

        /// <summary>
        /// Call when the drill bit contacts a tooth surface to trigger an
        /// additional contact-feedback burst.
        /// </summary>
        public void OnContactStart()
        {
            if (isContactActive) return;
            isContactActive = true;
            StartCoroutine(ContactBurst());
        }

        /// <summary>Call when the drill loses contact with the tooth surface.</summary>
        public void OnContactEnd()
        {
            isContactActive = false;
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void OnGrabbed(Valve.VR.SteamVR_Input_Sources _)
        {
            if (isDrillActive) StartDrillPulse();
        }

        private void OnReleased(Valve.VR.SteamVR_Input_Sources _)
        {
            StopDrillPulse();
        }

        private void StartDrillPulse()
        {
            if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
            pulseCoroutine = StartCoroutine(DrillPulseLoop());
        }

        private void StopDrillPulse()
        {
            if (pulseCoroutine != null)
            {
                StopCoroutine(pulseCoroutine);
                pulseCoroutine = null;
            }
        }

        private IEnumerator DrillPulseLoop()
        {
            while (isDrillActive)
            {
                float strength = maxPulseStrength * drillSpeed;
                if (isContactActive) strength += contactStrengthBonus;
                strength = Mathf.Clamp01(strength);

                toolInteraction.TriggerUseHaptics(strength);

                float interval = Mathf.Lerp(maxPulseInterval, minPulseInterval, drillSpeed);
                yield return new WaitForSeconds(interval);
            }
        }

        private IEnumerator ContactBurst()
        {
            float elapsed = 0f;
            while (elapsed < contactBurstDuration && isContactActive)
            {
                toolInteraction.TriggerUseHaptics(maxPulseStrength + contactStrengthBonus);
                yield return new WaitForSeconds(minPulseInterval);
                elapsed += minPulseInterval;
            }
        }
    }
}
