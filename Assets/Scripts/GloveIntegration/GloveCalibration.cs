using UnityEngine;

namespace DentalVR.GloveIntegration
{
    /// <summary>
    /// Per-finger calibration offset and scale correction for LucidGloves sensors.
    ///
    /// Raw potentiometer readings from the gloves can vary due to assembly
    /// tolerances.  This component stores a zero-point offset and a gain
    /// multiplier per finger so that the full 0–1 curl range is achieved even
    /// when the hardware isn't perfectly calibrated.
    ///
    /// Typical workflow
    /// ────────────────
    /// 1.  Open the hand fully → press the "Record Open Pose" button (or call
    ///     <see cref="RecordOpenPose"/> from another script / editor button).
    /// 2.  Close the hand into a fist → press "Record Closed Pose" / call
    ///     <see cref="RecordClosedPose"/>.
    /// 3.  The offsets and scales are calculated automatically.
    /// </summary>
    public class GloveCalibration : MonoBehaviour
    {
        // ── Serialised state ──────────────────────────────────────────────────
        [System.Serializable]
        public class FingerCalibration
        {
            [Tooltip("Raw sensor value recorded when the finger is fully open (0).")]
            public float rawOpen   = 0f;

            [Tooltip("Raw sensor value recorded when the finger is fully closed (1).")]
            public float rawClosed = 1f;
        }

        [Header("Per-Finger Calibration (thumb … pinky)")]
        [SerializeField] private FingerCalibration[] fingers = new FingerCalibration[5]
        {
            new FingerCalibration(),
            new FingerCalibration(),
            new FingerCalibration(),
            new FingerCalibration(),
            new FingerCalibration()
        };

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Maps a raw sensor value to a calibrated 0–1 curl value.
        /// </summary>
        /// <param name="fingerIndex">0 = thumb … 4 = pinky.</param>
        /// <param name="rawValue">Uncalibrated curl value from the SteamVR skeleton.</param>
        public float Apply(int fingerIndex, float rawValue)
        {
            if (fingerIndex < 0 || fingerIndex >= fingers.Length) return rawValue;

            FingerCalibration fc = fingers[fingerIndex];
            float range = fc.rawClosed - fc.rawOpen;

            if (Mathf.Approximately(range, 0f)) return rawValue;

            return Mathf.Clamp01((rawValue - fc.rawOpen) / range);
        }

        /// <summary>
        /// Records the current raw curl values as the "open / zero" reference.
        /// Call this while the player is holding their hand fully open.
        /// </summary>
        /// <param name="rawCurls">Live raw curl array from <see cref="FingerTrackingController"/>.</param>
        public void RecordOpenPose(float[] rawCurls)
        {
            if (rawCurls == null || rawCurls.Length < 5)
            {
                Debug.LogWarning("[GloveCalibration] RecordOpenPose: invalid input.");
                return;
            }
            for (int i = 0; i < 5; i++)
                fingers[i].rawOpen = rawCurls[i];

            Debug.Log("[GloveCalibration] Open pose recorded.");
        }

        /// <summary>
        /// Records the current raw curl values as the "closed / full" reference.
        /// Call this while the player is holding a tight fist.
        /// </summary>
        /// <param name="rawCurls">Live raw curl array from <see cref="FingerTrackingController"/>.</param>
        public void RecordClosedPose(float[] rawCurls)
        {
            if (rawCurls == null || rawCurls.Length < 5)
            {
                Debug.LogWarning("[GloveCalibration] RecordClosedPose: invalid input.");
                return;
            }
            for (int i = 0; i < 5; i++)
                fingers[i].rawClosed = rawCurls[i];

            Debug.Log("[GloveCalibration] Closed pose recorded.");
        }

        /// <summary>Resets all calibration values to identity (0 → 1 passthrough).</summary>
        public void ResetCalibration()
        {
            foreach (FingerCalibration fc in fingers)
            {
                fc.rawOpen   = 0f;
                fc.rawClosed = 1f;
            }
            Debug.Log("[GloveCalibration] Calibration reset to defaults.");
        }
    }
}
