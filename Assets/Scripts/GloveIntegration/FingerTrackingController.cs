using UnityEngine;
using Valve.VR;

namespace DentalVR.GloveIntegration
{
    /// <summary>
    /// Reads per-finger curl values from SteamVR's skeletal input system.
    /// The OpenGloves driver exposes the glove sensor data through the same
    /// Valve Index "hand skeleton" actions, so no special driver API is required.
    ///
    /// Attach one instance to each hand GameObject (left and right).
    /// </summary>
    public class FingerTrackingController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("SteamVR Skeleton")]
        [Tooltip("SteamVR_Behaviour_Skeleton component that drives this hand's rig.")]
        [SerializeField] private SteamVR_Behaviour_Skeleton handSkeleton;

        [Header("Hand Side")]
        [Tooltip("Which hand this tracker belongs to.")]
        [SerializeField] private SteamVR_Input_Sources inputSource = SteamVR_Input_Sources.RightHand;

        [Header("Calibration")]
        [Tooltip("Optional GloveCalibration component for per-sensor offset correction.")]
        [SerializeField] private GloveCalibration calibration;

        // ── Public state ──────────────────────────────────────────────────────
        /// <summary>Current finger-curl values: [thumb, index, middle, ring, pinky]. Range 0–1.</summary>
        public float[] FingerCurls { get; private set; } = new float[5];

        /// <summary>True when SteamVR reports the device as tracked.</summary>
        public bool IsTracked { get; private set; }

        // ── Finger index constants ────────────────────────────────────────────
        public const int THUMB  = 0;
        public const int INDEX  = 1;
        public const int MIDDLE = 2;
        public const int RING   = 3;
        public const int PINKY  = 4;

        // ── Unity lifecycle ───────────────────────────────────────────────────
        private void Update()
        {
            if (handSkeleton == null)
            {
                Debug.LogWarning($"[FingerTrackingController] No SteamVR_Behaviour_Skeleton " +
                                 $"assigned on {gameObject.name}.");
                return;
            }

            // SteamVR_Behaviour_Skeleton exposes an array of per-finger curl values.
            // Index 0 = thumb, 1 = index, 2 = middle, 3 = ring, 4 = pinky.
            for (int i = 0; i < 5; i++)
            {
                float raw = handSkeleton.fingerCurls[i];
                FingerCurls[i] = calibration != null ? calibration.Apply(i, raw) : raw;
            }

            // Determine tracking state via pose action
            IsTracked = SteamVR_Action_Pose.GetLocalIsActive(inputSource);
        }

        // ── Public helpers ────────────────────────────────────────────────────

        /// <summary>Returns the curl value for a single finger (0–1).</summary>
        public float GetFingerCurl(int fingerIndex)
        {
            if (fingerIndex < 0 || fingerIndex >= FingerCurls.Length) return 0f;
            return FingerCurls[fingerIndex];
        }

        /// <summary>
        /// Returns true if the given finger is considered "bent" (curl > threshold).
        /// </summary>
        public bool IsFingerBent(int fingerIndex, float threshold = 0.5f)
        {
            return GetFingerCurl(fingerIndex) >= threshold;
        }

        /// <summary>
        /// Returns true when the hand is in a "grab" pose —
        /// index, middle, ring and pinky are all bent beyond the threshold.
        /// </summary>
        public bool IsGrabbing(float threshold = 0.6f)
        {
            return IsFingerBent(INDEX,  threshold) &&
                   IsFingerBent(MIDDLE, threshold) &&
                   IsFingerBent(RING,   threshold) &&
                   IsFingerBent(PINKY,  threshold);
        }

        /// <summary>
        /// Returns true when the hand is making a "pinch" gesture —
        /// thumb and index are bent while middle/ring/pinky are open.
        /// </summary>
        public bool IsPinching(float bentThreshold = 0.6f, float openThreshold = 0.3f)
        {
            return IsFingerBent(THUMB, bentThreshold) &&
                   IsFingerBent(INDEX, bentThreshold) &&
                   !IsFingerBent(MIDDLE, openThreshold) &&
                   !IsFingerBent(RING,   openThreshold);
        }
    }
}
