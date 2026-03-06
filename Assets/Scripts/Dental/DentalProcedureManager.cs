using System.Collections;
using UnityEngine;
using Valve.VR;

namespace DentalVR.Dental
{
    /// <summary>
    /// High-level controller for the dental simulation session.
    /// Coordinates the state of the VR gloves and procedural haptics, and
    /// provides simple helpers for triggering procedure-specific feedback
    /// (probe tap, tooth extraction resistance, impression tray seating, etc.).
    ///
    /// Place one instance in the scene (typically on the XR Rig / CameraRig root).
    /// </summary>
    public class DentalProcedureManager : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Glove Manager")]
        [SerializeField] private GloveIntegration.GloveInputManager gloveInputManager;

        [Header("Feedback Presets")]
        [Tooltip("Haptic strength when the probe touches a tooth surface (0–1).")]
        [SerializeField] private float probeTapStrength = 0.4f;

        [Tooltip("Duration of a probe-tap haptic pulse (seconds).")]
        [SerializeField] private float probeTapDuration = 0.08f;

        [Tooltip("Continuous force strength simulating tooth extraction resistance (0–1).")]
        [SerializeField] private float extractionResistanceStrength = 0.85f;

        [Tooltip("Haptic strength for seating an impression tray (0–1).")]
        [SerializeField] private float impressionSeatStrength = 0.55f;

        [Tooltip("Duration of the impression-seat haptic burst (seconds).")]
        [SerializeField] private float impressionSeatDuration = 0.3f;

        // ── Unity lifecycle ───────────────────────────────────────────────────
        private void OnEnable()
        {
            if (gloveInputManager != null)
            {
                gloveInputManager.OnGlovesConnected    += HandleGlovesConnected;
                gloveInputManager.OnGlovesDisconnected += HandleGlovesDisconnected;
            }
        }

        private void OnDisable()
        {
            if (gloveInputManager != null)
            {
                gloveInputManager.OnGlovesConnected    -= HandleGlovesConnected;
                gloveInputManager.OnGlovesDisconnected -= HandleGlovesDisconnected;
            }
        }

        // ── Procedure feedback API ────────────────────────────────────────────

        /// <summary>
        /// Triggers a short haptic tap on the specified hand to simulate a
        /// dental probe touching a tooth surface.
        /// </summary>
        public void TriggerProbeTap(SteamVR_Input_Sources hand)
        {
            gloveInputManager?.SendHapticPulse(hand,
                GloveIntegration.FingerTrackingController.INDEX,
                probeTapStrength, probeTapDuration);
        }

        /// <summary>
        /// Starts a continuous resistance feedback pattern simulating the force
        /// required to extract a tooth.  Call <see cref="StopExtractionFeedback"/>
        /// when the extraction is complete.
        /// </summary>
        public void StartExtractionFeedback(SteamVR_Input_Sources hand)
        {
            StartCoroutine(ExtractionResistanceLoop(hand));
        }

        /// <summary>Stops extraction resistance haptics (set the flag to exit the coroutine).</summary>
        public void StopExtractionFeedback(SteamVR_Input_Sources hand)
        {
            extractionActive = false;
        }

        /// <summary>
        /// Fires a haptic burst on both hands simulating seating an impression tray.
        /// </summary>
        public void TriggerImpressionSeat()
        {
            gloveInputManager?.SendFullHandHaptics(SteamVR_Input_Sources.LeftHand,
                impressionSeatStrength, impressionSeatDuration);
            gloveInputManager?.SendFullHandHaptics(SteamVR_Input_Sources.RightHand,
                impressionSeatStrength, impressionSeatDuration);
        }

        // ── Internal helpers ──────────────────────────────────────────────────
        private bool extractionActive;

        private IEnumerator ExtractionResistanceLoop(SteamVR_Input_Sources hand)
        {
            extractionActive = true;
            while (extractionActive)
            {
                gloveInputManager?.SendFullHandHaptics(hand,
                    extractionResistanceStrength, 0.06f);
                yield return new WaitForSeconds(0.06f);
            }
        }

        private void HandleGlovesConnected()
        {
            Debug.Log("[DentalProcedureManager] Gloves connected — simulation ready.");
        }

        private void HandleGlovesDisconnected()
        {
            Debug.LogWarning("[DentalProcedureManager] Gloves disconnected — check hardware.");
        }
    }
}
