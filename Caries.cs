using UnityEngine;

public class Caries : MonoBehaviour
{
    [Header("Settings")]
    public float drillTimeRequired = 5.0f;
    public bool useDeveloperMode = false;
    public float developerDrillTime = 1.5f;

    [Header("Live Data (Read Only)")]
    public float drillProgress = 0f;
    public bool isDrilled = false;

    private float _drillTimer = 0f;
    private bool _isDrilling = false;
    private Collider _collider;
    private Renderer _rendererComponent;

    void Awake()
    {
        _collider = GetComponent<Collider>();
        _rendererComponent = GetComponent<Renderer>();
    }

    void Update()
    {
        if (!_isDrilling || isDrilled) return;

        float currentDrillTime = useDeveloperMode ? developerDrillTime : drillTimeRequired;
        _drillTimer += Time.deltaTime;
        drillProgress = Mathf.Clamp01(_drillTimer / currentDrillTime);

        // Log progress
        if (drillProgress >= 0.5f && drillProgress < 0.51f) Debug.Log("[Caries] 50% drilled");
        if (drillProgress >= 1.0f && drillProgress < 1.01f) Debug.Log("[Caries] 100% drilled");

        if (_drillTimer >= currentDrillTime)
        {
            CompleteDrill();
        }
    }

    public void StartDrill()
    {
        if (isDrilled)
        {
            Debug.LogWarning("[Caries] Already drilled!");
            return;
        }

        if (!_isDrilling)
        {
            Debug.Log("[Caries] Drilling started");
            _isDrilling = true;
            _drillTimer = 0f;
            drillProgress = 0f;
        }
    }

    public void StopDrill()
    {
        if (_isDrilling)
        {
            Debug.Log("[Caries] Drilling stopped");
            _isDrilling = false;
            _drillTimer = 0f;
            drillProgress = 0f;
        }
    }

    private void CompleteDrill()
    {
        isDrilled = true;
        _isDrilling = false;

        Debug.Log("[Caries] Drilling complete!");

        // Disable visuals
        if (_collider != null) _collider.enabled = false;
        if (_rendererComponent != null) _rendererComponent.enabled = false;
    }

    public bool IsDrilled()
    {
        return isDrilled;
    }
}