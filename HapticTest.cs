using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class HapticTest : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            TriggerTestHaptic();
        }
    }

    void TriggerTestHaptic()
    {
        // Find any XRBaseInputInteractor in the scene
        XRBaseInputInteractor[] controllers = FindObjectsOfType<XRBaseInputInteractor>();
        
        if (controllers.Length > 0)
        {
            Debug.Log("Found " + controllers.Length + " controllers. Triggering haptic...");
            controllers[0].SendHapticImpulse(0.8f, 0.3f);
        }
        else
        {
            Debug.LogWarning("No XRBaseInputInteractor found in scene!");
        }
    }
}