using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Haptic test script — platform-agnostic.
/// Uses the Unity XR InputDevice API so it works with any XR backend
/// (Meta Quest via Oculus XR Plugin or OpenXR, SteamVR, etc.).
///
/// Previously this used OVRInput.SetControllerVibration which is
/// Oculus-SDK-specific and stops working when SteamVR is the active
/// XR runtime.  The UnityEngine.XR.InputDevice haptics API works
/// identically across all supported runtimes.
/// </summary>
public class HapticTest : MonoBehaviour
{
    [Range(0f, 1f)]
    [Tooltip("Haptic amplitude (0 = silent, 1 = maximum).")]
    public float amplitude = 0.5f;

    [Range(0f, 1f)]
    [Tooltip("Haptic frequency (0 = low, 1 = high).")]
    public float frequency = 0.5f;

    private readonly List<InputDevice> _devices = new List<InputDevice>();

    private void Update()
    {
        // Send to all tracked hand/controller devices discovered this frame.
        SendHaptics(InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller);
        SendHaptics(InputDeviceCharacteristics.Left  | InputDeviceCharacteristics.Controller);
    }

    private void SendHaptics(InputDeviceCharacteristics characteristics)
    {
        _devices.Clear();
        InputDevices.GetDevicesWithCharacteristics(characteristics, _devices);

        foreach (InputDevice device in _devices)
        {
            if (device.TryGetHapticCapabilities(out HapticCapabilities caps) && caps.supportsImpulse)
                device.SendHapticImpulse(0, amplitude, Time.deltaTime);
        }
    }
}
