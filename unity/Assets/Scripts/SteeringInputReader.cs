using UnityEngine;

public class SteeringInputReader : MonoBehaviour
{
    [Header("Analog steering mapping (degrees)")]
    public float centerSteeringAngle = 100f;
    public float minAngle = 10f;
    public float maxAngle = 180f;
    [HideInInspector] public float responseCurve = 15f;
    [HideInInspector] public float deadZone = 0.03f;

    [HideInInspector] public float steering;   // -1..1 output

    private float previousSteer = 0f;

    // Called by your SerialInputInterpreter once per update
    public void SetRawAngle(float angle)
    {
        // Normalize to -1..1 around center
        float norm = (angle - centerSteeringAngle) /
                     (angle > centerSteeringAngle ? (maxAngle - centerSteeringAngle)
                                                  : (centerSteeringAngle - minAngle));
        norm = Mathf.Clamp(norm, -1f, 1f);

        // Apply response curve (gentle near center)
        float curved = Mathf.Sign(norm) * Mathf.Pow(Mathf.Abs(norm), responseCurve);

        // Apply small deadzone
        if (Mathf.Abs(curved) < deadZone)
            curved = 0f;

        // Optional light smoothing
        steering = Mathf.Lerp(previousSteer, curved, Time.deltaTime * 1f);
        previousSteer = steering;
    }
}
