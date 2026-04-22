using UnityEngine;

public class SerialInputInterpreter : MonoBehaviour
{
    public SerialController serialController;
    public ThrottleAndBrakeInput throttleAndBrake;
    public SteeringInputReader steeringInput;

    [Range(0, 360)] public float centerSteeringAngle = 102f;
    public float maxSteeringRange = 45f;

    void Update()
    {
        string msg = serialController.ReadSerialMessage();
        if (string.IsNullOrEmpty(msg)) return;

        string[] parts = msg.Split(',');
        if (parts.Length != 3) return;

        if (!int.TryParse(parts[0], out int angleInt) ||
            !int.TryParse(parts[1], out int throttleInt) ||
            !int.TryParse(parts[2], out int brakeInt))
            return;

        float angle = angleInt;
        float throttle = throttleInt / 100f;
        float brake = brakeInt / 100f;

        float delta = Mathf.DeltaAngle(centerSteeringAngle, angle);
        float steerNormalized = Mathf.Clamp(delta / maxSteeringRange, -1f, 1f);

        // Smoothly update
        if (steeringInput != null)
            steeringInput.steering = steerNormalized;

        if (throttleAndBrake != null)
        {
            throttleAndBrake.throttle = Mathf.Clamp01(throttle);
            throttleAndBrake.brake = Mathf.Clamp01(brake);
        }

        // Debug for verification
        // Debug.Log($"Angle={angle:F0}°  Steer={steerNormalized:F2}  Throttle={throttle:F2}  Brake={brake:F2}");
    }
}
