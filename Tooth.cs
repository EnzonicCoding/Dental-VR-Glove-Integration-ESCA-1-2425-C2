using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class Tooth : MonoBehaviour
{
    [Header("Setup")]
    public bool isNumb = false;           
    public float extractionTime = 30.0f;  
    public float developerExtractionTime = 3.0f; // Fast extraction for testing
    public bool useDeveloperMode = false; // Check this for 3-second fast extraction
    public Material numbMaterial;

    [Header("Feedback")]
    public AudioSource audioSource;
    public AudioClip crunchClip;
    public AudioClip popClip;

    [Header("Pop Destination")]
    public GameObject metalTray;          
    public float floatHeightAboveTray = 0.02f; 

    // Internal State
    private float _timer = 0f;
    private bool _isExtracted = false;
    private bool _hasStartedMetrics = false;
    private bool _isForcepsPulling = false;
    private Vector3 _initialPos;
    private Quaternion _initialRot;
    private Vector3 _initialScale;
    private Collider _collider;
    private Renderer _rendererComponent;

    // Components
    private XRGrabInteractable _grabInteractable;
    private MeshRenderer _renderer;
    private Material _originalMaterial;

    void Awake()
    {
        _grabInteractable = GetComponent<XRGrabInteractable>();
        _renderer = GetComponent<MeshRenderer>();
        _collider = GetComponent<Collider>();
        _rendererComponent = GetComponent<Renderer>();

        if (_renderer != null) _originalMaterial = _renderer.material;

        // Safety Check
        if (_grabInteractable == null)
        {
            Debug.LogError("CRITICAL: Missing XRGrabInteractable on " + gameObject.name);
            enabled = false;
            return;
        }

        _grabInteractable.selectEntered.AddListener(OnGrab);
        _grabInteractable.selectExited.AddListener(OnRelease);

        // Snapshot initial position for the sliding animation
        _initialPos = transform.localPosition;
        _initialRot = transform.localRotation;
        _initialScale = transform.localScale;

        UpdateNumbState();
    }

    void Update()
    {
        // 1. If not holding or already done, do nothing
        if ((!_grabInteractable.isSelected && !_isForcepsPulling) || _isExtracted) return;

        // 2. We are holding it -> Run logic
        ProcessExtraction();
    }

    // --- MAIN LOGIC ---
    private void ProcessExtraction()
    {
        float currentExtractionTime = useDeveloperMode ? developerExtractionTime : extractionTime;

        _timer += Time.deltaTime;

        // Calculate Progress (0.0 to 1.0)
        float progress = Mathf.Clamp01(_timer / currentExtractionTime);

        // --- 1. VISUALS: Slide & Shake ---
        float toothHeight = _rendererComponent != null ? _rendererComponent.bounds.size.y : 0.1f;
        float popHeight = toothHeight; 
        
        transform.localPosition = _initialPos + (Vector3.up * (progress * popHeight));
        transform.localRotation = _initialRot;
        transform.localScale = _initialScale;

        // --- 2. HAPTICS & AUDIO ---
        TriggerHaptic(0.2f + (0.5f * progress), 0.1f); 

        if (Random.value > 0.97f && audioSource && crunchClip)
            audioSource.PlayOneShot(crunchClip, 0.4f);

        // --- 3. METRICS REPORTING ---
        if (MetricsManager.Instance != null && MetricsManager.Instance.isRecording)
        {
            float simulatedForce = 5.0f + (2.0f * progress);
            MetricsManager.Instance.SetForce(simulatedForce);
        }

        // --- 4. FINISH CHECK ---
        if (_timer >= currentExtractionTime)
        {
            CompleteExtraction();
        }

        if (progress >= 0.25f && progress < 0.26f) Debug.Log("[Tooth] 25% extracted");
        if (progress >= 0.50f && progress < 0.51f) Debug.Log("[Tooth] 50% extracted");
        if (progress >= 0.75f && progress < 0.76f) Debug.Log("[Tooth] 75% extracted");
    }

    private void CompleteExtraction()
    {
        _isExtracted = true;

        if (_collider != null) _collider.enabled = false;

        // Detach from parent so world position is absolute
        transform.SetParent(null);

        // Move to floating position above metal tray (world space)
        if (metalTray != null)
        {
            Vector3 trayPos = metalTray.transform.position;
            transform.position = new Vector3(trayPos.x, -0.44f, trayPos.z);
            transform.rotation = Quaternion.identity;
            
            Debug.Log("Tooth moved above MetalTray at: " + transform.position);
        }
        else
        {
            Debug.LogWarning("MetalTray not assigned! Tooth will stay at extraction point");
        }

        // Audio & Haptics
        if (audioSource && popClip) audioSource.PlayOneShot(popClip);
        TriggerHaptic(1.0f, 0.2f);

        // Stop Metrics Recording
        if (MetricsManager.Instance != null)
        {
            MetricsManager.Instance.EndSession();
        }

        Debug.Log("Tooth Extracted!");
    }

    // --- EVENTS & HELPERS ---

    private void OnGrab(SelectEnterEventArgs args)
    {
        Debug.Log("[Tooth.OnGrab] Tooth grabbed: " + gameObject.name);
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        // If they let go before finishing, reset position
        if (!_isExtracted)
        {
            transform.localPosition = _initialPos;
            transform.localRotation = _initialRot;
            transform.localScale = _initialScale;
            _timer = 0; 
        }
    }

    public void SetNumb(bool state)
    {
        isNumb = state;
        UpdateNumbState();
    }

    private void UpdateNumbState()
    {
        if (_grabInteractable) _grabInteractable.enabled = isNumb;

        if (_renderer)
        {
            _renderer.material = (isNumb && numbMaterial != null) ? numbMaterial : _originalMaterial;
        }
    }

    private void TriggerHaptic(float intensity, float duration)
    {
        if (_grabInteractable.interactorsSelecting.Count > 0)
        {
            if (_grabInteractable.interactorsSelecting[0] is XRBaseInputInteractor controller)
            {
                controller.SendHapticImpulse(intensity, duration);
            }
        }
    }

    // Called by Forceps/Beak when contact is maintained
    public void StartForcepsPull()
    {
        Debug.Log("[Tooth.StartForcepsPull] Called | isExtracted=" + _isExtracted + ", isNumb=" + isNumb + ", isForcepsPulling=" + _isForcepsPulling);
        
        if (_isExtracted || !isNumb)
        {
            Debug.LogWarning("[Tooth.StartForcepsPull] BLOCKED | isExtracted=" + _isExtracted + ", isNumb=" + isNumb);
            return;
        }
        
        // Start forceps-based pulling
        if (!_isForcepsPulling)
        {
            Debug.Log("[Tooth.StartForcepsPull] Starting extraction sequence");
            _isForcepsPulling = true;
            
            if (_collider != null) _collider.enabled = false;
            
            if (!_hasStartedMetrics)
            {
                if (MetricsManager.Instance != null)
                {
                    MetricsManager.Instance.StartSession();
                    _hasStartedMetrics = true;
                }
            }
        }
    }

    // Called by Forceps/Beak when it loses contact
    public void StopForcepsPull()
    {
        Debug.Log("[Tooth.StopForcepsPull] Called | isExtracted=" + _isExtracted);
        
        if (!_isExtracted && _isForcepsPulling)
        {
            Debug.Log("[Tooth.StopForcepsPull] Resetting tooth position");
            _isForcepsPulling = false;
            
            if (_collider != null) _collider.enabled = true;
            
            transform.localPosition = _initialPos;
            transform.localRotation = _initialRot;
            transform.localScale = _initialScale;
            _timer = 0;
            _hasStartedMetrics = false;
        }
    }
}