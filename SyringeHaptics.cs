using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem; 
using UnityEngine.XR;

public class SyringeHaptics : MonoBehaviour
{
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable _grabInteractable;

    void Awake()
    {
        _grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
    }

    public void TriggerVibration(float intensity, float duration)
    {
        if (_grabInteractable == null) return;

        if (_grabInteractable.interactorsSelecting.Count > 0)
        {
            var interactor = _grabInteractable.interactorsSelecting[0];

            if (interactor is UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor controllerInteractor)
            {
                controllerInteractor.SendHapticImpulse(intensity, duration);
            }
        }
    }
}