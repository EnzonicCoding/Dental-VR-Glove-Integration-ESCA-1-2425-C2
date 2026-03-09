using UnityEngine;
using System.Collections.Generic;

public class ToolDebugger : MonoBehaviour
{
    public enum ToolType
    {
        Drill,
        Filler,
        Compactor,
        UVLight
    }

    [SerializeField] private ToolType toolType;
    private HashSet<Caries> _contactingCaries = new HashSet<Caries>();
    private HashSet<Filling> _contactingFillings = new HashSet<Filling>();
    private Dictionary<Caries, float> _cariesTimers = new Dictionary<Caries, float>();
    private Dictionary<Filling, float> _fillingTimers = new Dictionary<Filling, float>();
    private float _requiredContactTime = 0.1f;

    void OnTriggerEnter(Collider other)
    {
        // Check for Caries (only drill interacts with it)
        Caries caries = other.GetComponent<Caries>();
        if (caries != null && toolType == ToolType.Drill && !caries.IsDrilled())
        {
            _contactingCaries.Add(caries);
            _cariesTimers[caries] = 0f;
            
            // Log contact
            if (MetricsManager.Instance != null)
            {
                MetricsManager.Instance.LogContact();
            }
            
            Debug.Log("[ToolDebugger] Drill contacted Caries: " + other.gameObject.name);
        }

        // Check for Filling (filler, compactor, and UV light interact with it)
        Filling filling = other.GetComponent<Filling>();
        if (filling != null && !filling.IsCured())
        {
            _contactingFillings.Add(filling);
            _fillingTimers[filling] = 0f;
            
            // Log contact
            if (MetricsManager.Instance != null)
            {
                MetricsManager.Instance.LogContact();
                
                // Check for wrong tool on wrong stage
                bool isWrongTool = false;
                
                if (toolType == ToolType.Filler && filling.GetCurrentStage() != Filling.FillingStage.NotStarted)
                    isWrongTool = true;
                else if (toolType == ToolType.Compactor && filling.GetCurrentStage() != Filling.FillingStage.Filled)
                    isWrongTool = true;
                else if (toolType == ToolType.UVLight && filling.GetCurrentStage() != Filling.FillingStage.Compacted)
                    isWrongTool = true;
                
                if (isWrongTool)
                {
                    MetricsManager.Instance.LogError("Wrong tool (" + toolType + ") on stage " + filling.GetCurrentStage());
                }
            }
            
            Debug.Log("[ToolDebugger] " + toolType + " contacted Filling: " + other.gameObject.name);
        }
    }

    void OnTriggerStay(Collider other)
    {
        // Handle Caries drilling
        Caries caries = other.GetComponent<Caries>();
        if (caries != null && _contactingCaries.Contains(caries))
        {
            _cariesTimers[caries] += Time.deltaTime;

            if (_cariesTimers[caries] >= _requiredContactTime)
            {
                caries.StartDrill();
            }
        }

        // Handle Filling processing
        Filling filling = other.GetComponent<Filling>();
        if (filling != null && _contactingFillings.Contains(filling))
        {
            _fillingTimers[filling] += Time.deltaTime;

            if (_fillingTimers[filling] >= _requiredContactTime)
            {
                switch (toolType)
                {
                    case ToolType.Filler:
                        if (filling.GetCurrentStage() == Filling.FillingStage.NotStarted)
                            filling.StartFilling();
                        break;
                    case ToolType.Compactor:
                        if (filling.GetCurrentStage() == Filling.FillingStage.Filled)
                            filling.StartCompacting();
                        break;
                    case ToolType.UVLight:
                        if (filling.GetCurrentStage() == Filling.FillingStage.Compacted)
                            filling.StartCuring();
                        break;
                }
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        Caries caries = other.GetComponent<Caries>();
        if (caries != null && _contactingCaries.Contains(caries))
        {
            caries.StopDrill();
            _contactingCaries.Remove(caries);
            _cariesTimers.Remove(caries);
        }

        Filling filling = other.GetComponent<Filling>();
        if (filling != null && _contactingFillings.Contains(filling))
        {
            switch (filling.GetCurrentStage())
            {
                case Filling.FillingStage.NotStarted:
                    filling.StopFilling();
                    break;
                case Filling.FillingStage.Filled:
                    filling.StopCompacting();
                    break;
                case Filling.FillingStage.Compacted:
                    filling.StopCuring();
                    break;
            }

            _contactingFillings.Remove(filling);
            _fillingTimers.Remove(filling);
        }
    }
}