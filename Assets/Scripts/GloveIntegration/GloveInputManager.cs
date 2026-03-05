using System;
using UnityEngine;
using Valve.VR;

namespace DentalVR.GloveIntegration
{
    /// <summary>
    /// Central manager for LucidGloves (LucasVRTech / OpenGloves) integration.
    /// Relies on the OpenGloves SteamVR driver being installed and active.
    /// Attach this component to a persistent GameObject in the scene.
    /// </summary>
    public class GloveInputManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────
        public static GloveInputManager Instance { get; private set; }

        // ── Inspector references ──────────────────────────────────────────────
        [Header("Glove Components")]
        [Tooltip("FingerTrackingController for the left hand.")]
        [SerializeField] private FingerTrackingController leftHandTracking;

        [Tooltip("FingerTrackingController for the right hand.")]
        [SerializeField] private FingerTrackingController rightHandTracking;

        [Tooltip("HapticFeedbackController for the left glove.")]
        [SerializeField] private HapticFeedbackController leftHaptics;

        [Tooltip("HapticFeedbackController for the right glove.")]
        [SerializeField] private HapticFeedbackController rightHaptics;

        // ── Events ────────────────────────────────────────────────────────────
        /// <summary>Fired when both gloves are detected and ready.</summary>
        public event Action OnGlovesConnected;

        /// <summary>Fired when one or both gloves are disconnected.</summary>
        public event Action OnGlovesDisconnected;

        // ── Internal state ────────────────────────────────────────────────────
        private bool glovesWereConnected;

        // ── Unity lifecycle ───────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (SteamVR.instance == null)
            {
                Debug.LogWarning("[GloveInputManager] SteamVR is not initialised. " +
                                 "Ensure SteamVR is running and the OpenGloves driver is installed.");
            }
        }

        private void Update()
        {
            bool connected = AreBothGlovesConnected();

            if (connected && !glovesWereConnected)
            {
                glovesWereConnected = true;
                Debug.Log("[GloveInputManager] Both gloves connected.");
                OnGlovesConnected?.Invoke();
            }
            else if (!connected && glovesWereConnected)
            {
                glovesWereConnected = false;
                Debug.LogWarning("[GloveInputManager] One or both gloves disconnected.");
                OnGlovesDisconnected?.Invoke();
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the current finger-curl values (0 = open, 1 = closed) for the
        /// requested hand.
        /// </summary>
        /// <param name="hand">Left or Right hand.</param>
        /// <returns>Array of 5 floats: [thumb, index, middle, ring, pinky].</returns>
        public float[] GetFingerCurls(SteamVR_Input_Sources hand)
        {
            FingerTrackingController tracker = GetTracker(hand);
            return tracker != null ? tracker.FingerCurls : new float[5];
        }

        /// <summary>
        /// Sends a force-feedback pulse to a single finger on the given hand.
        /// </summary>
        /// <param name="hand">Left or Right hand.</param>
        /// <param name="fingerIndex">0 = thumb … 4 = pinky.</param>
        /// <param name="strength">Normalised force level (0–1).</param>
        /// <param name="durationSeconds">How long to hold the force.</param>
        public void SendHapticPulse(SteamVR_Input_Sources hand, int fingerIndex,
                                    float strength, float durationSeconds = 0.1f)
        {
            HapticFeedbackController haptics = GetHaptics(hand);
            haptics?.SendForceFeedback(fingerIndex, strength, durationSeconds);
        }

        /// <summary>
        /// Sends force-feedback simultaneously to all five fingers of a hand.
        /// </summary>
        public void SendFullHandHaptics(SteamVR_Input_Sources hand, float strength,
                                        float durationSeconds = 0.1f)
        {
            HapticFeedbackController haptics = GetHaptics(hand);
            if (haptics == null) return;

            for (int i = 0; i < 5; i++)
                haptics.SendForceFeedback(i, strength, durationSeconds);
        }

        /// <summary>True if the named SteamVR device for the given hand is tracked.</summary>
        public bool IsHandTracked(SteamVR_Input_Sources hand)
        {
            FingerTrackingController tracker = GetTracker(hand);
            return tracker != null && tracker.IsTracked;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private bool AreBothGlovesConnected()
        {
            return IsHandTracked(SteamVR_Input_Sources.LeftHand) &&
                   IsHandTracked(SteamVR_Input_Sources.RightHand);
        }

        private FingerTrackingController GetTracker(SteamVR_Input_Sources hand)
        {
            return hand == SteamVR_Input_Sources.LeftHand ? leftHandTracking : rightHandTracking;
        }

        private HapticFeedbackController GetHaptics(SteamVR_Input_Sources hand)
        {
            return hand == SteamVR_Input_Sources.LeftHand ? leftHaptics : rightHaptics;
        }
    }
}
