using UnityEngine;
using UnityEngine.SceneManagement;

public class CleanableTooth : MonoBehaviour
{
    [Header("Setup")]
    public int startingStage = 1;
    public Material worsetooth;
    public Material badtooth;
    public Material white;

    [Header("Cleaning Settings")]
    public float cleaningTimePerStage = 3.0f;
    public bool useDeveloperMode = false;
    public float developerCleaningTime = 1.5f;

    [Header("Live Data (Read Only)")]
    public int currentStage = 1;
    public float stageProgress = 0f;
    [SerializeField] private bool isFullyCleaned = false;

    public bool IsFullyCleanedProperty
    {
        get => isFullyCleaned;
        set => isFullyCleaned = value;
    }

    private float _stageTimer = 0f;
    private bool _isBeingCleaned = false;
    private Collider _collider;
    private MeshRenderer _renderer;
    private Material _originalMaterial;
    private bool _hasStartedMetrics = false;

    void Awake()
    {
        _renderer = GetComponent<MeshRenderer>();
        _collider = GetComponent<Collider>();

        if (_renderer != null) _originalMaterial = _renderer.material;

        currentStage = startingStage;
        
        // If tooth starts at Stage 3, mark it as fully cleaned
        if (currentStage >= 3)
        {
            isFullyCleaned = true;
        }
        
        UpdateVisuals();
    }

    void Update()
    {
        if (!_isBeingCleaned || isFullyCleaned) return;

        float currentCleaningTime = useDeveloperMode ? developerCleaningTime : cleaningTimePerStage;
        _stageTimer += Time.deltaTime;
        stageProgress = Mathf.Clamp01(_stageTimer / currentCleaningTime);

        // Track force during cleaning
        if (MetricsManager.Instance != null && MetricsManager.Instance.isRecording)
        {
            float simulatedForce = stageProgress * 10f;
            MetricsManager.Instance.SetForce(simulatedForce);
        }

        if (_stageTimer >= currentCleaningTime)
        {
            ProgressToNextStage();
        }
    }

    public void StartCleaning(string toolType)
    {
        if (isFullyCleaned)
        {
            Debug.LogWarning("[CleanableTooth] Cannot clean - already finished");
            return;
        }

        if (!_isBeingCleaned)
        {
            Debug.Log("[CleanableTooth] Cleaning started on " + gameObject.name + " (Stage " + currentStage + ") with " + toolType);
            _isBeingCleaned = true;
            _stageTimer = 0f;
            stageProgress = 0f;

            // Start metrics session on first tooth contact (both scenes)
            if (!_hasStartedMetrics && MetricsManager.Instance != null)
            {
                MetricsManager.Instance.StartSession();
                _hasStartedMetrics = true;
            }
        }
    }

    public void StopCleaning()
    {
        if (_isBeingCleaned)
        {
            Debug.Log("[CleanableTooth] Cleaning stopped on " + gameObject.name);
            _isBeingCleaned = false;
            _stageTimer = 0f;
            stageProgress = 0f;
        }
    }

    private void ProgressToNextStage()
    {
        currentStage++;
        _stageTimer = 0f;
        stageProgress = 0f;
        _isBeingCleaned = false;
        
        Debug.Log("[CleanableTooth] Progressed to stage " + currentStage + ": " + gameObject.name);
        UpdateVisuals();

        if (currentStage >= 3)
        {
            isFullyCleaned = true;
            Debug.Log("[CleanableTooth] Tooth fully cleaned: " + gameObject.name);

            // Log completion to metrics
            if (MetricsManager.Instance != null && MetricsManager.Instance.isRecording)
            {
                MetricsManager.Instance.LogContact();
            }

            // Check if all teeth are cleaned (end session for cleaning scene)
            string currentScene = SceneManager.GetActiveScene().name;
            if (currentScene == "Procedure - Oral Prophylaxis")
            {
                if (AllTeethCleaned() && MetricsManager.Instance != null)
                {
                    Debug.Log("[CleanableTooth] All teeth cleaned! Ending session.");
                    MetricsManager.Instance.EndSession();
                }
            }
            // End metrics for extraction scene
            else if (currentScene == "ToothExtraction" && MetricsManager.Instance != null)
            {
                MetricsManager.Instance.EndSession();
            }
        }
    }

    private bool AllTeethCleaned()
    {
        CleanableTooth[] allTeeth = FindObjectsOfType<CleanableTooth>();
        Debug.Log("[AllTeethCleaned] Checking " + allTeeth.Length + " teeth");
        
        foreach (CleanableTooth tooth in allTeeth)
        {
            Debug.Log("[AllTeethCleaned] " + tooth.gameObject.name + " - Stage: " + tooth.GetCurrentStage() + ", Cleaned: " + tooth.IsFullyCleaned());
            if (!tooth.IsFullyCleaned())
            {
                return false;
            }
        }
        
        Debug.Log("[AllTeethCleaned] All teeth are fully cleaned!");
        return true;
    }

    private void UpdateVisuals()
    {
        if (_renderer == null) return;

        if (currentStage == 1 && worsetooth != null)
            _renderer.material = worsetooth;
        else if (currentStage == 2 && badtooth != null)
            _renderer.material = badtooth;
        else if (currentStage == 3 && white != null)
            _renderer.material = white;
        else
            _renderer.material = _originalMaterial;
    }

    public int GetCurrentStage()
    {
        return currentStage;
    }

    public bool IsFullyCleaned()
    {
        return isFullyCleaned;
    }
}