using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;

public class MetricsManager : MonoBehaviour
{
    public static MetricsManager Instance;
    private static bool _sessionAlreadyStarted = false;

    [Header("Settings")]
    public string subjectName = "Student_01";

    [Header("Live Data (Read Only)")]
    public bool isRecording = false;
    public float currentTimer = 0f;
    public int errorCount = 0;
    public int totalContacts = 0;
    public float currentSimulatedForce = 0f;

    private List<float> _forceSamples = new List<float>();
    private StringBuilder _eventLog = new StringBuilder();
    private System.DateTime _sessionStartTime;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        if (!isRecording) return;

        currentTimer += Time.deltaTime;
        _forceSamples.Add(currentSimulatedForce);
    }

    public void StartSession()
    {
        // Only start if not already started
        if (_sessionAlreadyStarted) return;
        
        _sessionAlreadyStarted = true;
        isRecording = true;
        currentTimer = 0;
        errorCount = 0;
        totalContacts = 0;
        _forceSamples.Clear();
        _eventLog.Clear();
        _eventLog.AppendLine("Time,Event");
        _sessionStartTime = System.DateTime.Now;
        Debug.Log("METRICS: Session Started at " + _sessionStartTime.ToString("HH:mm:ss"));
    }

    public void EndSession()
    {
        isRecording = false;
        _sessionAlreadyStarted = false;
        SaveReport();
        Debug.Log("METRICS: Session Saved");
    }

    public void LogError(string errorType)
    {
        if (!isRecording) return;
        errorCount++;
        totalContacts++;
        _eventLog.AppendLine(currentTimer.ToString("F2") + ",ERROR: " + errorType);
        Debug.LogWarning("METRICS ERROR: " + errorType);
    }

    public void LogContact()
    {
        if (!isRecording) return;
        totalContacts++;
    }

    public void SetForce(float forceInNewtons)
    {
        currentSimulatedForce = forceInNewtons;
    }

    private void SaveReport()
    {
        string filename = subjectName + "_Session_" + System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".csv";
        string filePath = Path.Combine(Application.persistentDataPath, filename);

        StringBuilder csv = new StringBuilder();

        System.DateTime sessionEndTime = System.DateTime.Now;
        float errorRate = totalContacts > 0 ? (errorCount / (float)totalContacts) * 100f : 0f;

        // Determine report title based on active scene - dynamically formatted
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string reportTitle = FormatSceneTitle(currentScene);

        csv.AppendLine(reportTitle);
        csv.AppendLine("Participant," + subjectName);
        csv.AppendLine("Session Start," + _sessionStartTime.ToString("yyyy-MM-dd HH:mm:ss"));
        csv.AppendLine("Session End," + sessionEndTime.ToString("yyyy-MM-dd HH:mm:ss"));
        csv.AppendLine("Date," + System.DateTime.Now);
        csv.AppendLine("------------------------------------------------");

        float peakForce = _forceSamples.Count > 0 ? _forceSamples.Max() : 0;
        float avgForce = _forceSamples.Count > 0 ? _forceSamples.Average() : 0;

        float sumSquares = _forceSamples.Sum(x => (x - avgForce) * (x - avgForce));
        float forceVariability = _forceSamples.Count > 0 ? Mathf.Sqrt(sumSquares / _forceSamples.Count) : 0;

        csv.AppendLine("PERFORMANCE METRICS");
        csv.AppendLine("Session Duration (s)," + currentTimer.ToString("F2"));
        csv.AppendLine("Total Contacts," + totalContacts);
        csv.AppendLine("Error Count," + errorCount);
        csv.AppendLine("Error Rate (%)," + errorRate.ToString("F2"));
        csv.AppendLine("Peak Force (N)," + peakForce.ToString("F2"));
        csv.AppendLine("Avg Force (N)," + avgForce.ToString("F2"));
        csv.AppendLine("Force Variability (StdDev)," + forceVariability.ToString("F4"));
        csv.AppendLine("Total Force Samples," + _forceSamples.Count);

        csv.AppendLine("------------------------------------------------");
        csv.AppendLine("EVENT LOG");
        csv.Append(_eventLog.ToString());

        File.WriteAllText(filePath, csv.ToString());
        Debug.Log("REPORT SAVED TO: " + filePath);
    }

    private string FormatSceneTitle(string sceneName)
    {
        // Remove "Procedure - " prefix if it exists
        string cleanName = sceneName.StartsWith("Procedure - ") 
            ? sceneName.Substring("Procedure - ".Length) 
            : sceneName;
        
        // Convert to uppercase and add "SESSION METRICS REPORT"
        return cleanName.ToUpper() + " SESSION METRICS REPORT";
    }
}