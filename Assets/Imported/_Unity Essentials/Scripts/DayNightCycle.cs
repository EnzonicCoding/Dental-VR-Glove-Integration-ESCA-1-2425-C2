using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Header("Cycle Settings")]
    [Tooltip("How many real-world seconds it takes for a full 24-hour cycle to pass.")]
    public float secondsInFullDay = 120f;

    [Header("Starting Time")]
    [Range(0, 1)]
    [Tooltip("0 is sunrise, 0.5 is sunset, 0.25 is midday.")]
    public float timeProgress = 0f;

    void Update()
    {
        // Calculate how many degrees to rotate per second
        // 360 degrees / secondsInFullDay = degrees per second
        float rotationSpeed = 360f / secondsInFullDay;

        // Rotate the light around its local X-axis
        transform.Rotate(Vector3.right * rotationSpeed * Time.deltaTime);

        // Optional: Keep track of the progress (0 to 1) for other systems
        timeProgress += (Time.deltaTime / secondsInFullDay);
        if (timeProgress > 1f) timeProgress = 0f;
    }
}