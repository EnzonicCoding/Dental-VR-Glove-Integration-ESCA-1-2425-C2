using System;
using UnityEngine;

public class Clock : MonoBehaviour
{
    // References to the hand transforms (assign these in the Inspector)
    public Transform hourHand;
    public Transform minuteHand;
    public Transform secondHand;

    void Update()
    {
        // 1. Get the current system time
        DateTime time = DateTime.Now;

        // 2. Calculate the rotation degrees
        // 360 degrees / 12 hours = 30 degrees per hour
        // We add (time.Minute / 2f) to make the hour hand move smoothly between hours
        float hourAngle = (time.Hour % 12) * 30f + (time.Minute / 2f);

        // 360 degrees / 60 minutes = 6 degrees per minute
        float minuteAngle = time.Minute * 6f;

        // 360 degrees / 60 seconds = 6 degrees per second
        float secondAngle = time.Second * 6f;

        // 3. Apply the rotation
        // NOTE: Depending on how your prefab is built, you might need to change
        // which axis (x, y, or z) gets the angle.
        // The standard Course Library clock usually rotates around the X or Z axis.
        // This code assumes Z-axis rotation (common for 2D/Wall clocks).

        if (hourHand != null)
            hourHand.localRotation = Quaternion.Euler(hourAngle, 0, 0);

        if (minuteHand != null)
            minuteHand.localRotation = Quaternion.Euler(minuteAngle, 0, 0);

        if (secondHand != null)
            secondHand.localRotation = Quaternion.Euler(secondAngle, 0, 0);
    }
}