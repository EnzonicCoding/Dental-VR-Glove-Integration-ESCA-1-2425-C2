using System;
using System.Collections;
using System.IO.Ports;
using UnityEngine;

namespace DentalVR.GloveIntegration
{
    /// <summary>
    /// Manages force-feedback (haptic) communication with a single LucidGlove
    /// over a serial port.  The LucidGloves firmware accepts a command string of
    /// the form:
    ///
    ///     FFB&lt;thumb&gt;&amp;&lt;index&gt;&amp;&lt;middle&gt;&amp;&lt;ring&gt;&amp;&lt;pinky&gt;\n
    ///
    /// where each value is an integer 0–255 (0 = no force, 255 = maximum force).
    ///
    /// Attach one component per physical glove and configure the COM port in
    /// the Inspector (or let <see cref="GloveInputManager"/> assign it at runtime).
    /// </summary>
    public class HapticFeedbackController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────
        [Header("Serial Connection")]
        [Tooltip("Serial port name, e.g. 'COM3' on Windows or '/dev/ttyUSB0' on Linux.")]
        [SerializeField] private string portName = "COM3";

        [Tooltip("Baud rate matching the firmware setting (default: 115200).")]
        [SerializeField] private int baudRate = 115200;

        [Tooltip("Automatically open the serial port on Start.")]
        [SerializeField] private bool autoConnect = true;

        [Header("Feedback Defaults")]
        [Tooltip("Default hold duration (seconds) when not specified per-call.")]
        [SerializeField] private float defaultDuration = 0.1f;

        [Tooltip("Minimum interval (seconds) between consecutive commands to avoid " +
                 "overloading the serial buffer.")]
        [SerializeField] private float commandCooldown = 0.02f;

        // ── Internal state ────────────────────────────────────────────────────
        private SerialPort serialPort;
        private float[] pendingForces = new float[5];  // thumb..pinky, 0–1
        private float lastCommandTime;

        // ── Unity lifecycle ───────────────────────────────────────────────────
        private void Start()
        {
            if (autoConnect) OpenPort();
        }

        private void OnDestroy()
        {
            ClosePort();
        }

        private void OnApplicationQuit()
        {
            ClosePort();
        }

        // ── Connection management ─────────────────────────────────────────────

        /// <summary>Opens the serial port for communication with the glove firmware.</summary>
        public void OpenPort()
        {
            if (serialPort != null && serialPort.IsOpen) return;

            try
            {
                serialPort = new SerialPort(portName, baudRate)
                {
                    ReadTimeout  = 100,
                    WriteTimeout = 100
                };
                serialPort.Open();
                Debug.Log($"[HapticFeedbackController] Opened serial port {portName} @ {baudRate} baud.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[HapticFeedbackController] Could not open {portName}: {ex.Message}. " +
                                 "Haptic feedback will be disabled.");
                serialPort = null;
            }
        }

        /// <summary>Closes the serial port if it is open.</summary>
        public void ClosePort()
        {
            if (serialPort != null && serialPort.IsOpen)
            {
                // Release all fingers before closing
                WriteCommand(BuildCommand(new float[5]));
                serialPort.Close();
            }
            serialPort = null;
        }

        /// <summary>True when the serial port is open and ready.</summary>
        public bool IsConnected => serialPort != null && serialPort.IsOpen;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Applies a normalised force (0–1) to a single finger for
        /// <paramref name="durationSeconds"/> seconds, then releases.
        /// </summary>
        /// <param name="fingerIndex">0 = thumb … 4 = pinky.</param>
        /// <param name="strength">Force level, 0 (none) to 1 (maximum).</param>
        /// <param name="durationSeconds">Hold duration in seconds.</param>
        public void SendForceFeedback(int fingerIndex, float strength,
                                      float durationSeconds = -1f)
        {
            if (!IsConnected) return;
            if (fingerIndex < 0 || fingerIndex > 4) return;

            float duration = durationSeconds < 0 ? defaultDuration : durationSeconds;
            pendingForces[fingerIndex] = Mathf.Clamp01(strength);
            StartCoroutine(HoldAndRelease(fingerIndex, strength, duration));
        }

        /// <summary>
        /// Applies per-finger forces simultaneously then releases after
        /// <paramref name="durationSeconds"/> seconds.
        /// </summary>
        /// <param name="forces">Array of 5 normalised force values [thumb..pinky].</param>
        public void SendAllFingersFeedback(float[] forces, float durationSeconds = -1f)
        {
            if (!IsConnected || forces == null || forces.Length < 5) return;

            float duration = durationSeconds < 0 ? defaultDuration : durationSeconds;
            StartCoroutine(HoldAllAndRelease(forces, duration));
        }

        /// <summary>
        /// Immediately releases all finger forces (sends zeros).
        /// </summary>
        public void ReleaseAll()
        {
            if (!IsConnected) return;
            WriteCommand(BuildCommand(new float[5]));
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private IEnumerator HoldAndRelease(int fingerIndex, float strength, float duration)
        {
            float[] forces = new float[5];
            forces[fingerIndex] = Mathf.Clamp01(strength);

            ThrottledWrite(BuildCommand(forces));
            yield return new WaitForSeconds(duration);

            forces[fingerIndex] = 0f;
            ThrottledWrite(BuildCommand(forces));
        }

        private IEnumerator HoldAllAndRelease(float[] forces, float duration)
        {
            ThrottledWrite(BuildCommand(forces));
            yield return new WaitForSeconds(duration);
            ThrottledWrite(BuildCommand(new float[5]));
        }

        private void ThrottledWrite(string command)
        {
            if (Time.time - lastCommandTime < commandCooldown) return;
            lastCommandTime = Time.time;
            WriteCommand(command);
        }

        private void WriteCommand(string command)
        {
            try
            {
                serialPort?.WriteLine(command);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[HapticFeedbackController] Serial write failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Builds a LucidGloves-compatible FFB command string.
        /// Format: "FFB{thumb}&{index}&{middle}&{ring}&{pinky}"
        /// Each value is 0–255.
        /// </summary>
        private static string BuildCommand(float[] forces)
        {
            int t = NormTo255(forces[0]);
            int i = NormTo255(forces[1]);
            int m = NormTo255(forces[2]);
            int r = NormTo255(forces[3]);
            int p = NormTo255(forces[4]);
            return $"FFB{t}&{i}&{m}&{r}&{p}";
        }

        private static int NormTo255(float normalised) =>
            Mathf.RoundToInt(Mathf.Clamp01(normalised) * 255f);
    }
}
