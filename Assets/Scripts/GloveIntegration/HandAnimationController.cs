using UnityEngine;

namespace DentalVR.GloveIntegration
{
    /// <summary>
    /// Drives a hand-rig's finger bone rotations from live glove finger-curl values.
    /// Each finger is represented by a chain of three bone Transforms (proximal →
    /// intermediate → distal phalanx).  The curl value (0–1) is mapped linearly to
    /// a configurable rotation range around the finger's local X axis.
    ///
    /// Attach one instance to each hand root GameObject and assign the bone chains
    /// in the Inspector.
    /// </summary>
    public class HandAnimationController : MonoBehaviour
    {
        // ── Data structures ───────────────────────────────────────────────────
        [System.Serializable]
        public class FingerBones
        {
            [Tooltip("Name shown in the Inspector only.")]
            public string name = "Finger";

            [Tooltip("Proximal phalanx (knuckle) bone Transform.")]
            public Transform proximal;

            [Tooltip("Intermediate phalanx (middle segment) bone Transform.")]
            public Transform intermediate;

            [Tooltip("Distal phalanx (tip segment) bone Transform.")]
            public Transform distal;

            [Header("Rotation Limits")]
            [Tooltip("Rotation (degrees) around the bone's local X-axis when curl = 0 (open).")]
            public float openAngle = 0f;

            [Tooltip("Rotation (degrees) around the bone's local X-axis when curl = 1 (closed).")]
            public float closedAngle = 90f;

            [Tooltip("Fraction of total curl applied to each phalanx. Must sum to 1.")]
            public float proximalWeight    = 0.5f;
            public float intermediateWeight = 0.35f;
            public float distalWeight       = 0.15f;
        }

        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Finger Bone Chains (thumb … pinky)")]
        [SerializeField] private FingerBones thumb;
        [SerializeField] private FingerBones index;
        [SerializeField] private FingerBones middle;
        [SerializeField] private FingerBones ring;
        [SerializeField] private FingerBones pinky;

        [Header("Data Source")]
        [Tooltip("FingerTrackingController on the same hand.")]
        [SerializeField] private FingerTrackingController fingerTracking;

        [Header("Smoothing")]
        [Tooltip("Lerp speed for animation smoothing (higher = more responsive).")]
        [SerializeField] private float smoothingSpeed = 15f;

        // ── Internal ──────────────────────────────────────────────────────────
        private FingerBones[] fingers;
        private float[] smoothedCurls = new float[5];

        // ── Unity lifecycle ───────────────────────────────────────────────────
        private void Awake()
        {
            fingers = new[] { thumb, index, middle, ring, pinky };
        }

        private void Update()
        {
            if (fingerTracking == null) return;

            float[] curls = fingerTracking.FingerCurls;

            for (int i = 0; i < fingers.Length; i++)
            {
                // Smooth the incoming value to avoid jittery bone motion
                smoothedCurls[i] = Mathf.Lerp(smoothedCurls[i], curls[i],
                                               Time.deltaTime * smoothingSpeed);
                ApplyCurl(fingers[i], smoothedCurls[i]);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void ApplyCurl(FingerBones finger, float curl)
        {
            if (finger == null) return;

            float totalDelta = Mathf.Lerp(finger.openAngle, finger.closedAngle, curl);

            SetLocalXRotation(finger.proximal,     totalDelta * finger.proximalWeight);
            SetLocalXRotation(finger.intermediate, totalDelta * finger.intermediateWeight);
            SetLocalXRotation(finger.distal,       totalDelta * finger.distalWeight);
        }

        private static void SetLocalXRotation(Transform bone, float xAngle)
        {
            if (bone == null) return;
            Vector3 angles = bone.localEulerAngles;
            angles.x = xAngle;
            bone.localEulerAngles = angles;
        }
    }
}
