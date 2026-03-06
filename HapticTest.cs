using UnityEngine;
using Meta.XR.ImmersiveDebugger.UserInterface.Generic;

public class HapticTest : MonoBehaviour
{
    void Update()
    {
        OVRInput.SetControllerVibration(0.5f, 0.5f, OVRInput.Controller.RTouch);
        OVRInput.SetControllerVibration(0.5f, 0.5f, OVRInput.Controller.LTouch);
    }
}
