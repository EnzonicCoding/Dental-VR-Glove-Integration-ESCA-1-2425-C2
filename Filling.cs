using UnityEngine;

public class Filling : MonoBehaviour
{
    public enum FillingStage
    {
        NotStarted,      // initialFilling (invisible)
        Filled,          // transparentFilling (visible)
        Compacted,       // metallicFilling
        Cured            // opaqueFilling (then destroyed)
    }

    [Header("Settings")]
    public float fillerTimeRequired = 5.0f;
    public float compactorTimeRequired = 5.0f;
    public float uvLightTimeRequired = 5.0f;
    public bool useDeveloperMode = false;
    public float developerTime = 1.5f;

    [Header("Materials")]
    public Material initialFilling;
    public Material transparentFilling;
    public Material metallicFilling;
    public Material opaqueFilling;

    [Header("Live Data (Read Only)")]
    public FillingStage currentStage = FillingStage.NotStarted;
    public float stageProgress = 0f;

    private float _stageTimer = 0f;
    private bool _isProcessing = false;
    private Renderer _rendererComponent;

    void Awake()
    {
        _rendererComponent = GetComponent<Renderer>();
        
        // Start with invisible material
        if (_rendererComponent != null && initialFilling != null)
        {
            _rendererComponent.material = initialFilling;
        }
    }

    void Update()
    {
        if (!_isProcessing) return;

        float currentTime = useDeveloperMode ? developerTime : GetTimeForStage();
        _stageTimer += Time.deltaTime;
        stageProgress = Mathf.Clamp01(_stageTimer / currentTime);

        if (_stageTimer >= currentTime)
        {
            ProgressToNextStage();
        }
    }

    public void StartFilling()
    {
        if (currentStage != FillingStage.NotStarted)
        {
            Debug.LogWarning("[Filling] Already started or completed!");
            return;
        }

        if (!_isProcessing)
        {
            Debug.Log("[Filling] Filling started");
            _isProcessing = true;
            _stageTimer = 0f;
            stageProgress = 0f;
        }
    }

    public void StopFilling()
    {
        if (_isProcessing)
        {
            Debug.Log("[Filling] Filling stopped");
            _isProcessing = false;
            _stageTimer = 0f;
            stageProgress = 0f;
        }
    }

    public void StartCompacting()
    {
        if (currentStage != FillingStage.Filled)
        {
            Debug.LogWarning("[Filling] Must apply filler first!");
            return;
        }

        if (!_isProcessing)
        {
            Debug.Log("[Filling] Compacting started");
            _isProcessing = true;
            _stageTimer = 0f;
            stageProgress = 0f;
        }
    }

    public void StopCompacting()
    {
        if (_isProcessing)
        {
            Debug.Log("[Filling] Compacting stopped");
            _isProcessing = false;
            _stageTimer = 0f;
            stageProgress = 0f;
        }
    }

    public void StartCuring()
    {
        if (currentStage != FillingStage.Compacted)
        {
            Debug.LogWarning("[Filling] Must compact first!");
            return;
        }

        if (!_isProcessing)
        {
            Debug.Log("[Filling] Curing started");
            _isProcessing = true;
            _stageTimer = 0f;
            stageProgress = 0f;
        }
    }

    public void StopCuring()
    {
        if (_isProcessing)
        {
            Debug.Log("[Filling] Curing stopped");
            _isProcessing = false;
            _stageTimer = 0f;
            stageProgress = 0f;
        }
    }

    private void ProgressToNextStage()
    {
        _isProcessing = false;

        switch (currentStage)
        {
            case FillingStage.NotStarted:
                // Start metrics on first fill
                if (MetricsManager.Instance != null && !MetricsManager.Instance.isRecording)
                {
                    MetricsManager.Instance.StartSession();
                }
                currentStage = FillingStage.Filled;
                ApplyTransparentFilling();
                Debug.Log("[Filling] Stage 1 complete: Filler applied");
                break;

            case FillingStage.Filled:
                currentStage = FillingStage.Compacted;
                ApplyMetallicFilling();
                Debug.Log("[Filling] Stage 2 complete: Filling compacted");
                break;

            case FillingStage.Compacted:
                currentStage = FillingStage.Cured;
                ApplyOpaqueFilling();
                Debug.Log("[Filling] Stage 3 complete: Filling cured");
                
                // End metrics when procedure complete
                if (MetricsManager.Instance != null)
                {
                    MetricsManager.Instance.EndSession();
                }
                
                Destroy(gameObject);
                break;
        }
    }

    private void ApplyTransparentFilling()
    {
        if (_rendererComponent != null && transparentFilling != null)
        {
            _rendererComponent.material = transparentFilling;
        }
    }

    private void ApplyMetallicFilling()
    {
        if (_rendererComponent != null && metallicFilling != null)
        {
            _rendererComponent.material = metallicFilling;
        }
    }

    private void ApplyOpaqueFilling()
    {
        if (_rendererComponent != null && opaqueFilling != null)
        {
            _rendererComponent.material = opaqueFilling;
        }
    }

    private float GetTimeForStage()
    {
        return currentStage switch
        {
            FillingStage.NotStarted => fillerTimeRequired,
            FillingStage.Filled => compactorTimeRequired,
            FillingStage.Compacted => uvLightTimeRequired,
            _ => 1.0f
        };
    }

    public FillingStage GetCurrentStage()
    {
        return currentStage;
    }

    public bool IsCured()
    {
        return currentStage == FillingStage.Cured;
    }
}