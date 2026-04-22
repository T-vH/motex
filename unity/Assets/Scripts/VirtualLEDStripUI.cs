using System.Globalization;
using UnityEngine;

public class VirtualLEDStripUI : MonoBehaviour
{
    [Header("Angle Settings")]
    [Tooltip("Total horizontal field of view used for the LEDs (e.g. 180 = -90..+90)")]
    public float arcDegrees = 180f;

    [Tooltip("Number of discrete LED positions (must match Arduino mapping)")]
    public int ledCount = 60;

    [Header("Hardware")]
    public SerialController serialController;   // Ardity SerialController

    // Called by FeedbackManager when warning stops
    public void ResetStrip()
    {
        // Just tell Arduino to turn all LEDs off
        if (serialController != null)
        {
            serialController.SendSerialMessage("LED_OFF\n");
        }
    }

    // Called by FeedbackManager while hazard warning is active
    public void HighlightDirection(float angleDeg)
    {
        if (serialController == null)
            return;

        // Clamp angle to the arc (-arc/2 .. +arc/2)
        float halfArc = arcDegrees * 0.5f;
        float clampedAngle = Mathf.Clamp(angleDeg, -halfArc, halfArc);

        // Map angle → 0..1 → LED index
        float normalized = Mathf.InverseLerp(-halfArc, halfArc, clampedAngle);
        int ledIndex = Mathf.RoundToInt(normalized * (ledCount - 1));
        ledIndex = Mathf.Clamp(ledIndex, 0, ledCount - 1);

        // Convert index back to a quantized angle so Unity and Arduino stay in sync
        float normalizedIndex = (float)ledIndex / (ledCount - 1);
        float quantizedAngle = Mathf.Lerp(-halfArc, halfArc, normalizedIndex);

        string msg = "LED," + quantizedAngle.ToString("F1", CultureInfo.InvariantCulture);
        serialController.SendSerialMessage(msg + "\n");
    }
}
