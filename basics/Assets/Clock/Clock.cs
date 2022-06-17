using UnityEngine;
using System;

public class Clock : MonoBehaviour
{
    const float hoursToDegrees = -30f;
    const float minutesToDegrees = -6f;
    const float secondsToDegrees = -6f;

    [SerializeField]
    Transform hoursPivot, minutesPivot, secondsPivot;

    void Update()
    {
        var time = DateTime.Now.TimeOfDay;
        hoursPivot.localRotation = Quaternion.Euler(
            0, 0, (float)time.TotalHours * hoursToDegrees);
        minutesPivot.localRotation = Quaternion.Euler(
            0, 0, (float)time.TotalMinutes * minutesToDegrees);
        secondsPivot.localRotation = Quaternion.Euler(
            0, 0, (float)time.TotalSeconds * secondsToDegrees);
    }
}
