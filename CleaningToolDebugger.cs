using UnityEngine;
using System.Collections.Generic;

public class CleaningToolDebugger : MonoBehaviour
{
    private HashSet<CleanableTooth> _contactingTeeth = new HashSet<CleanableTooth>();
    private Dictionary<CleanableTooth, float> _contactTimers = new Dictionary<CleanableTooth, float>();
    private Dictionary<CleanableTooth, bool> _hasStartedCleaning = new Dictionary<CleanableTooth, bool>();
    private float _requiredContactTime = 0.1f;
    public string toolType = "Scaler";

    void OnTriggerEnter(Collider other)
    {
        CleanableTooth tooth = other.GetComponent<CleanableTooth>();
        if (tooth != null && !_contactingTeeth.Contains(tooth))
        {
            // Skip Stage 3 teeth (already fully cleaned)
            int stage = tooth.GetCurrentStage();
            if (stage == 3)
            {
                return;
            }
            
            _contactingTeeth.Add(tooth);
            _contactTimers[tooth] = 0f;
            _hasStartedCleaning[tooth] = false;
            
            // Log contact for metrics
            if (MetricsManager.Instance != null)
            {
                MetricsManager.Instance.LogContact();
                
                // Check for wrong tool on wrong stage (skip Stage 3 teeth - already clean)
                if ((toolType == "Scaler" && stage != 1) ||
                    (toolType == "Spade" && stage != 2))
                {
                    MetricsManager.Instance.LogError("Wrong tool used on stage " + stage + ": " + toolType);
                    Debug.LogWarning("[ERROR] Wrong tool on stage " + stage);
                }
            }
            
            Debug.Log("[OK] CleanableTooth found on: " + other.gameObject.name + " (Stage " + stage + ")");
        }
    }

    void OnTriggerStay(Collider other)
    {
        CleanableTooth tooth = other.GetComponent<CleanableTooth>();
        if (tooth != null && _contactingTeeth.Contains(tooth))
        {
            int stage = tooth.GetCurrentStage();
            
            // Skip Stage 3 teeth (already fully cleaned)
            if (stage == 3)
            {
                return;
            }
            
            _contactTimers[tooth] += Time.deltaTime;
            
            // Start cleaning once contact time threshold is reached
            if (_contactTimers[tooth] >= _requiredContactTime && !_hasStartedCleaning[tooth])
            {
                // Validate tool for stage
                if ((toolType == "Scaler" && stage == 1) || (toolType == "Spade" && stage == 2))
                {
                    Debug.Log(">> StartCleaning: " + tooth.gameObject.name + " (Stage " + stage + ")");
                    tooth.StartCleaning(toolType);
                    _hasStartedCleaning[tooth] = true;
                }
                else
                {
                    Debug.LogWarning(">> Cannot clean: Wrong tool for stage " + stage);
                    _hasStartedCleaning[tooth] = true;
                }
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        CleanableTooth tooth = other.GetComponent<CleanableTooth>();
        if (tooth != null && _contactingTeeth.Contains(tooth))
        {
            Debug.Log("TOOL RELEASED: " + other.gameObject.name);
            tooth.StopCleaning();
            
            _contactingTeeth.Remove(tooth);
            _contactTimers.Remove(tooth);
            _hasStartedCleaning.Remove(tooth);
        }
    }
}