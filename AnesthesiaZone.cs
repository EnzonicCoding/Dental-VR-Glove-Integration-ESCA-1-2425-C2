using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class AnesthesiaZone : MonoBehaviour
{
    [Header("Settings")]
    public float injectionTimeRequired = 3.0f; // Seconds to hold steady
    public string syringeTag = "Syringe";

    [Header("Events")]
    public UnityEvent onAnesthesiaComplete;
    public UnityEvent onInjectionInterrupted;

    [Header("Affected Teeth")]
    public List<Tooth> affectedTeeth = new List<Tooth>();

    [Header("Feedback Effects")]
    public ParticleSystem completionParticles; 
    public AudioSource completionSound;        
    public AudioClip popClip;                  

    private float _timer = 0f;
    private bool _isInjecting = false;
    private bool _isComplete = false;
    public float hapticIntensity = 0.2f;
    public bool isNumb = false;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(syringeTag) && !_isComplete)
        {
            _isInjecting = true;
            Debug.Log("Injection started...");
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag(syringeTag) && !_isComplete)
        {
            _timer += Time.deltaTime;

            // Find the haptics script on the syringe
            SyringeHaptics haptics = other.GetComponentInParent<SyringeHaptics>();
            if (haptics != null)
            {
                // Buzz every frame (0.2 intensity is a good "fluid" feel)
                haptics.TriggerVibration(0.2f, 0.1f);
            }

            if (_timer >= injectionTimeRequired)
            {
                CompleteAnesthesia(other);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(syringeTag))
        {
            ResetTimer();
        }
    }

    void CompleteAnesthesia(Collider syringeCollider)
    {
        _isComplete = true;
        _isInjecting = false;

        Debug.Log("--- COMPLETION TRIGGERED ---");

        // 1. Play Visuals
        if (completionParticles != null)
        {
            completionParticles.Stop();
            completionParticles.Play();
            Debug.Log("Particles Fired!");
        }

        // 2. Play Audio
        if (completionSound != null && popClip != null)
        {
            completionSound.clip = popClip;
            completionSound.Play();
            Debug.Log("Audio Fired on: " + completionSound.gameObject.name);
        }

        // 3. Completion Haptic Feedback
        SyringeHaptics haptics = syringeCollider.GetComponentInParent<SyringeHaptics>();
        if (haptics != null)
        {
            haptics.TriggerVibration(0.8f, 0.3f);
        }

        // 4. Update the Teeth
        isNumb = true;

        foreach (Tooth t in affectedTeeth)
        {
            if (t != null) t.SetNumb(true);
        }

        // 5. START METRICS SESSION
        if (MetricsManager.Instance != null)
        {
            MetricsManager.Instance.StartSession();
        }

        Debug.Log("Anesthesia Complete!");
        onAnesthesiaComplete.Invoke();
    }

    void ResetTimer()
    {
        if (_isInjecting)
        {
            Debug.Log("Injection Interrupted! Steady hands, doc.");
            _timer = 0f;
            _isInjecting = false;
            onInjectionInterrupted.Invoke();
        }
    }

    public float GetProgress() => Mathf.Clamp01(_timer / injectionTimeRequired);
}