using System;
using UnityEngine;
using UnityEngine.XR;
using Valve.VR;

namespace DentalVR.GloveIntegration
{
    /// <summary>
    /// Central manager for LucidGloves (LucasVRTech / OpenGloves) integration.
    /// Relies on the OpenGloves SteamVR driver being installed and active.
    ///
    /// This component is designed to be *optional* in a scene.  When SteamVR is
    /// not the active XR runtime (e.g. the project is running on a standalone
    /// Meta Quest build or via the Oculus XR Plugin), the manager disables itself
    /// gracefully so that the rest of the dental simulation continues to function
    /// normally without the gloves.
    ///
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

        // ── Public state ──────────────────────────────────────────────────────
        /// <summary>
        /// True when SteamVR is the active XR runtime and the glove subsystem is
        /// available.  False when running on Meta Quest / Oculus runtime — in that
        /// case all glove API calls are no-ops so the simulation still works.
        /// </summary>
        public bool IsGlovesAvailable { get; private set; }

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
            IsGlovesAvailable = IsSteamVRActiveRuntime();

            if (!IsGlovesAvailable)
            {
                Debug.Log("[GloveInputManager] SteamVR is not the active XR runtime. " +
                          "Running without LucidGloves — headset and controller tracking " +
                          "via the active XR plugin (e.g. Meta Quest / OpenXR) are unaffected. " +
                          "To enable gloves, run the project through SteamVR on a Windows PC " +
                          "with the OpenGloves driver installed.");
            }
        }

        private void Update()
        {
            if (!IsGlovesAvailable) return;

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
        /// requested hand.  Returns zeros when gloves are unavailable.
        /// </summary>
        /// <param name="hand">Left or Right hand.</param>
        /// <returns>Array of 5 floats: [thumb, index, middle, ring, pinky].</returns>
        public float[] GetFingerCurls(SteamVR_Input_Sources hand)
        {
            if (!IsGlovesAvailable) return new float[5];
            FingerTrackingController tracker = GetTracker(hand);
            return tracker != null ? tracker.FingerCurls : new float[5];
        }

        /// <summary>
        /// Sends a force-feedback pulse to a single finger on the given hand.
        /// No-op when gloves are unavailable.
        /// </summary>
        /// <param name="hand">Left or Right hand.</param>
        /// <param name="fingerIndex">0 = thumb … 4 = pinky.</param>
        /// <param name="strength">Normalised force level (0–1).</param>
        /// <param name="durationSeconds">How long to hold the force.</param>
        public void SendHapticPulse(SteamVR_Input_Sources hand, int fingerIndex,
                                    float strength, float durationSeconds = 0.1f)
        {
            if (!IsGlovesAvailable) return;
            HapticFeedbackController haptics = GetHaptics(hand);
            haptics?.SendForceFeedback(fingerIndex, strength, durationSeconds);
        }

        /// <summary>
        /// Sends force-feedback simultaneously to all five fingers of a hand.
        /// No-op when gloves are unavailable.
        /// </summary>
        public void SendFullHandHaptics(SteamVR_Input_Sources hand, float strength,
                                        float durationSeconds = 0.1f)
        {
            if (!IsGlovesAvailable) return;
            HapticFeedbackController haptics = GetHaptics(hand);
            if (haptics == null) return;

            for (int i = 0; i < 5; i++)
                haptics.SendForceFeedback(i, strength, durationSeconds);
        }

        /// <summary>True if the named SteamVR device for the given hand is tracked.</summary>
        public bool IsHandTracked(SteamVR_Input_Sources hand)
        {
            if (!IsGlovesAvailable) return false;
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

        /// <summary>
        /// Returns true when SteamVR is the currently loaded XR subsystem.
        /// Uses the runtime device name to detect this without calling
        /// SteamVR.instance (which would attempt to initialise SteamVR and
        /// could conflict with the Meta/Oculus runtime).
        /// </summary>
        private static bool IsSteamVRActiveRuntime()
        {
            try
            {
                // XRSettings.loadedDeviceName is "OpenVR" when SteamVR is active,
                // "Oculus" when the Oculus XR plugin is active, or "OpenXR" when the
                // OpenXR plugin is active.
                string device = XRSettings.loadedDeviceName;
                bool isSteamVR = string.Equals(device, "OpenVR",
                                               StringComparison.OrdinalIgnoreCase);
                if (!isSteamVR)
                    Debug.Log($"[GloveInputManager] Active XR device: '{device}'. " +
                               "LucidGloves require OpenVR (SteamVR) as the XR runtime on a PC.");
                return isSteamVR;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[GloveInputManager] Could not determine XR runtime: " + ex.Message);
                return false;
            }
        }
    }
}
