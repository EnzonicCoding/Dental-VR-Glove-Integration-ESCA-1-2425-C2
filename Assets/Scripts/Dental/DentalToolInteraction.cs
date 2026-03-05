using System;
using UnityEngine;
using Valve.VR;

namespace DentalVR.Dental
{
    /// <summary>
    /// Handles picking up and putting down dental tools (drill, mirror, probe, etc.)
    /// using the LucidGloves grab gesture.  On grab the tool is parented to the hand,
    /// and force feedback is triggered through <see cref="GloveIntegration.GloveInputManager"/>.
    ///
    /// Attach to each interactable dental-tool GameObject.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class DentalToolInteraction : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Tool Settings")]
        [Tooltip("Human-readable tool name, used in debug messages.")]
        [SerializeField] private string toolName = "Dental Tool";

        [Tooltip("Offset applied to the tool's local position when held.")]
        [SerializeField] private Vector3 holdPositionOffset = Vector3.zero;

        [Tooltip("Offset applied to the tool's local rotation (Euler angles) when held.")]
        [SerializeField] private Vector3 holdRotationOffset = Vector3.zero;

        [Header("Grab Detection")]
        [Tooltip("Radius of the sphere used to detect nearby hands.")]
        [SerializeField] private float grabRadius = 0.08f;

        [Tooltip("Grab threshold: minimum finger-curl value to trigger a grab (0–1).")]
        [SerializeField] private float grabThreshold = 0.6f;

        [Tooltip("Release threshold: maximum finger-curl value to release the tool (0–1).")]
        [SerializeField] private float releaseThreshold = 0.3f;

        [Header("Haptic Feedback")]
        [Tooltip("Force-feedback strength when the tool is picked up (0–1).")]
        [SerializeField] private float grabHapticStrength = 0.7f;

        [Tooltip("Duration of the grab haptic pulse (seconds).")]
        [SerializeField] private float grabHapticDuration = 0.15f;

        [Tooltip("Force-feedback strength while the tool is being used (0–1).")]
        [SerializeField] private float useHapticStrength = 0.4f;

        // ── Events ────────────────────────────────────────────────────────────
        /// <summary>Fired when a hand grabs this tool. Param = hand source.</summary>
        public event Action<SteamVR_Input_Sources> OnToolGrabbed;

        /// <summary>Fired when the hand releases this tool.</summary>
        public event Action<SteamVR_Input_Sources> OnToolReleased;

        // ── Internal state ────────────────────────────────────────────────────
        private bool isHeld;
        private SteamVR_Input_Sources holdingHand;
        private Transform holdingHandTransform;
        private Rigidbody rb;

        // Cached hand-pose transforms (populated once in Start to avoid per-frame scene searches).
        private Transform leftHandTransform;
        private Transform rightHandTransform;

        // ── Unity lifecycle ───────────────────────────────────────────────────
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        private void Start()
        {
            CacheHandTransforms();
        }

        private void Update()
        {
            if (!isHeld)
                CheckForGrab();
            else
                CheckForRelease();
        }

        // ── Grab / Release logic ──────────────────────────────────────────────

        private void CheckForGrab()
        {
            // Find the closer hand that is within grabRadius and making a grab gesture
            Transform closest    = null;
            float     closestDist = float.MaxValue;
            SteamVR_Input_Sources closestSource = SteamVR_Input_Sources.Any;

            CheckHand(SteamVR_Input_Sources.LeftHand,  ref closest, ref closestDist, ref closestSource);
            CheckHand(SteamVR_Input_Sources.RightHand, ref closest, ref closestDist, ref closestSource);

            if (closest != null)
                GrabWith(closest, closestSource);
        }

        private void CheckHand(SteamVR_Input_Sources source,
                                ref Transform closest, ref float closestDist,
                                ref SteamVR_Input_Sources closestSource)
        {
            var manager = GloveIntegration.GloveInputManager.Instance;
            if (manager == null) return;
            if (!manager.IsHandTracked(source)) return;

            Transform handTransform = source == SteamVR_Input_Sources.LeftHand
                ? leftHandTransform
                : rightHandTransform;

            if (handTransform == null)
            {
                // Attempt a lazy re-cache in case the hand was spawned after Start.
                CacheHandTransforms();
                handTransform = source == SteamVR_Input_Sources.LeftHand
                    ? leftHandTransform
                    : rightHandTransform;
                if (handTransform == null) return;
            }

            float dist = Vector3.Distance(transform.position, handTransform.position);
            if (dist > grabRadius) return;

            float[] curls = manager.GetFingerCurls(source);
            bool grabbing = IsGrabPose(curls);
            if (!grabbing) return;

            if (dist < closestDist)
            {
                closestDist   = dist;
                closest       = handTransform;
                closestSource = source;
            }
        }

        private void GrabWith(Transform handTransform, SteamVR_Input_Sources source)
        {
            isHeld              = true;
            holdingHand         = source;
            holdingHandTransform = handTransform;

            // Parent the tool to the hand
            transform.SetParent(handTransform);
            transform.localPosition = holdPositionOffset;
            transform.localRotation = Quaternion.Euler(holdRotationOffset);

            // Disable physics while held
            rb.isKinematic = true;

            // Haptic pulse on grab (index + middle fingers)
            var manager = GloveIntegration.GloveInputManager.Instance;
            if (manager != null)
            {
                manager.SendHapticPulse(source, FingerTrackingController.INDEX,
                                        grabHapticStrength, grabHapticDuration);
                manager.SendHapticPulse(source, FingerTrackingController.MIDDLE,
                                        grabHapticStrength, grabHapticDuration);
            }

            Debug.Log($"[DentalToolInteraction] {toolName} grabbed by {source}.");
            OnToolGrabbed?.Invoke(source);
        }

        private void CheckForRelease()
        {
            var manager = GloveIntegration.GloveInputManager.Instance;
            if (manager == null) return;

            float[] curls = manager.GetFingerCurls(holdingHand);

            // Release when the grab pose is broken (fingers open beyond threshold)
            if (!IsGrabPose(curls, releaseThreshold))
                ReleaseFrom(holdingHand);
        }

        private void ReleaseFrom(SteamVR_Input_Sources source)
        {
            isHeld = false;

            transform.SetParent(null);
            rb.isKinematic = false;

            Debug.Log($"[DentalToolInteraction] {toolName} released by {source}.");
            OnToolReleased?.Invoke(source);
        }

        // ── Haptic helpers (called externally, e.g. from DentalDrillHaptics) ─

        /// <summary>
        /// Sends a "use" haptic pulse to the hand currently holding this tool.
        /// </summary>
        public void TriggerUseHaptics(float strengthOverride = -1f)
        {
            if (!isHeld) return;

            var manager = GloveIntegration.GloveInputManager.Instance;
            if (manager == null) return;

            float strength = strengthOverride < 0f ? useHapticStrength : strengthOverride;
            manager.SendHapticPulse(holdingHand, FingerTrackingController.INDEX, strength);
            manager.SendHapticPulse(holdingHand, FingerTrackingController.MIDDLE, strength);
        }

        // ── Utilities ─────────────────────────────────────────────────────────

        private static bool IsGrabPose(float[] curls, float threshold = -1f)
        {
            float t = threshold < 0f ? 0.6f : threshold;
            if (curls == null || curls.Length < 5) return false;
            return curls[FingerTrackingController.INDEX]  >= t &&
                   curls[FingerTrackingController.MIDDLE] >= t &&
                   curls[FingerTrackingController.RING]   >= t;
        }

        /// <summary>
        /// Walks all active SteamVR_Behaviour_Pose components once and caches the
        /// left-hand and right-hand transforms, avoiding repeated per-frame searches.
        /// </summary>
        private void CacheHandTransforms()
        {
            foreach (SteamVR_Behaviour_Pose pose in
                     UnityEngine.Object.FindObjectsOfType<SteamVR_Behaviour_Pose>())
            {
                if (pose.inputSource == SteamVR_Input_Sources.LeftHand)
                    leftHandTransform = pose.transform;
                else if (pose.inputSource == SteamVR_Input_Sources.RightHand)
                    rightHandTransform = pose.transform;
            }
        }

        // ── Gizmos ────────────────────────────────────────────────────────────
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, grabRadius);
        }
    }
}
