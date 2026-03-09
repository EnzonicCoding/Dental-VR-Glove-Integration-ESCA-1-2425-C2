using UnityEngine;
using TMPro;

public class FillingProgressDisplay : MonoBehaviour
{
    [SerializeField] private Filling _filling;
    [SerializeField] private TextMeshProUGUI _progressText;
    [SerializeField] private TextMeshProUGUI _stageText;
    [SerializeField] private TextMeshProUGUI _statusText;

    void Update()
    {
        if (_filling == null || _progressText == null)
            return;

        // Update progress bar (0-100%)
        int progressPercent = Mathf.RoundToInt(_filling.stageProgress * 100f);
        _progressText.text = $"Progress: {progressPercent}%\n[{new string(' ', progressPercent / 10)}{new string(' ', 10 - progressPercent / 10)}]";

        // Update current stage
        _stageText.text = $"Stage: {_filling.GetCurrentStage()}\n{_filling.currentStage}";

        // Update status
        string status = _filling.IsCured() ? "COMPLETE ?" : "IN PROGRESS";
        _statusText.text = $"Status: {status}";
    }
}