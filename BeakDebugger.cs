using UnityEngine;

public class BeakDebugger : MonoBehaviour
{
    private Tooth _currentTooth = null;
    private float _contactTimer = 0f;
    private float _requiredContactTime = 0.1f;
    private bool _hasStartedPull = false;
    public string _targetToothName = "Object001";

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("BEAK TOUCHED: " + other.gameObject.name);
        
        // Check for extraction tooth first
        Tooth tooth = other.GetComponent<Tooth>();
        if (tooth != null)
        {
            _currentTooth = tooth;
            _contactTimer = 0f;
            _hasStartedPull = false;
            
            // Log contact (for error rate calculation)
            if (MetricsManager.Instance != null)
            {
                MetricsManager.Instance.LogContact();
            }
        }
        else if (other.GetComponent<ToothReference>() != null)
        {
            // Log error for wrong tooth
            if (MetricsManager.Instance != null)
            {
                MetricsManager.Instance.LogContact();
                MetricsManager.Instance.LogError("Wrong tooth contacted: " + other.gameObject.name);
            }
            Debug.LogWarning("[ERROR] Wrong tooth contacted: " + other.gameObject.name);
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (_currentTooth != null && other.GetComponent<Tooth>() == _currentTooth)
        {
            _contactTimer += Time.deltaTime;
            Debug.Log("Contact Timer: " + _contactTimer.ToString("F2") + "s / " + _requiredContactTime + "s");
            
            if (_contactTimer >= _requiredContactTime && !_hasStartedPull)
            {
                Debug.Log(">> CALLING StartForcepsPull() - FIRST TIME ONLY");
                _currentTooth.StartForcepsPull();
                _hasStartedPull = true;
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (_currentTooth != null && other.GetComponent<Tooth>() == _currentTooth)
        {
            Debug.Log("BEAK RELEASED: " + other.gameObject.name);
            Debug.Log(">> CALLING StopForcepsPull()");
            _currentTooth.StopForcepsPull();
            _currentTooth = null;
            _contactTimer = 0f;
            _hasStartedPull = false;
        }
    }
}