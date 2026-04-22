using UnityEngine;

public class MotoController : MonoBehaviour
{
    [Header("Wheel Colliders")]
    public WheelCollider WColForward;
    public WheelCollider WColBack;

    [Header("Wheel Transforms")]
    public Transform wheelF;
    public Transform wheelB;

    [Header("Center of Mass")]
    public Transform CenterOfMass;

    [Header("Bike Settings")]
    public float maxSteerAngle = 45f;
    public float maxMotorTorque = 200f;
    public float maxBrakeTorque = 600f; // unified brake torque
    public float wheelRadius = 0.3f;
    public float wheelOffset = 0.1f;

    [Header("Control Sensitivity")]
    [Range(0.05f, 1f)] public float steerSensitivity = 0.2f;
    public float lowSpeed = 0.1f;
    public float highSpeed = 10f;

    [Header("External Input")]
    public ThrottleAndBrakeInput hardwareThrottle;
    public SteeringInputReader hardwareSteering;

    [Header("Steering Damping by Speed")]
    [Range(0.1f, 1f)] public float steeringMinFactor = 0.3f;
    [Range(0.5f, 3f)] public float steeringResponsePower = 1.3f;

    // --- Private ---
    private WheelData[] wheels;
    private Transform thisTransform;
    private Rigidbody rb;

    private Vector3 prevPos = Vector3.zero;
    private float speedVal = 0f;

    // ------------------------------
    // Wheel Data Class
    // ------------------------------
    public class WheelData
    {
        public Transform wheelTransform;
        public WheelCollider wheelCollider;
        public Vector3 wheelStartPos;
        public float rotation = 0f;

        public WheelData(Transform transform, WheelCollider collider)
        {
            wheelTransform = transform;
            wheelCollider = collider;
            wheelStartPos = transform.localPosition;
        }
    }

    // ------------------------------
    // Input Struct
    // ------------------------------
    public struct MotoInput
    {
        public float steer;
        public float acceleration;
        public float brake;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = CenterOfMass.localPosition;

        // allow pitch (so the bike can dive) but stay upright (no roll)
        rb.constraints = RigidbodyConstraints.FreezeRotationZ;

        wheels = new WheelData[2];
        wheels[0] = new WheelData(wheelF, WColForward);
        wheels[1] = new WheelData(wheelB, WColBack);

        thisTransform = transform;

        // basic friction tuning
        TuneFriction(WColForward, 1.0f, 1.2f);
        TuneFriction(WColBack, 1.0f, 1.2f);
    }

    void FixedUpdate()
    {
        var input = ReadInput();
        motoMove(input);
        updateWheels();

        // Keep upright (yaw only)
        Quaternion flatRot = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, flatRot, Time.deltaTime * 5f);
    }

    private MotoInput ReadInput()
    {
        MotoInput input = new MotoInput();

        if (hardwareThrottle != null && hardwareSteering != null)
        {
            input.acceleration = hardwareThrottle.throttle;
            input.brake = hardwareThrottle.brake;

            float steerRaw = hardwareSteering.steering;

            // Steering sensitivity decreases with speed
            float normSpeed = Mathf.Clamp01(speedVal / highSpeed);
            float speedFactor = Mathf.Lerp(1f, steeringMinFactor, Mathf.Pow(normSpeed, steeringResponsePower));
            input.steer = steerRaw * steerSensitivity * speedFactor;
        }
        else
        {
            if (Input.GetKey(KeyCode.W)) input.acceleration = 1f;
            if (Input.GetKey(KeyCode.S)) input.brake = 1f;
            if (Input.GetKey(KeyCode.A)) input.steer = steerSensitivity;
            if (Input.GetKey(KeyCode.D)) input.steer = -steerSensitivity;
        }

        return input;
    }

    private void motoMove(MotoInput input)
    {
        // calculate speed
        Vector3 posNow = thisTransform.position;
        Vector3 vel = (posNow - prevPos) / Time.fixedDeltaTime;
        prevPos = posNow;
        speedVal = vel.magnitude;

        // --- Steering ---
        WColForward.steerAngle = Mathf.Clamp(input.steer, -1f, 1f) * maxSteerAngle;

        // --- Braking ---
        float brakeTorque = maxBrakeTorque * input.brake;
        WColBack.brakeTorque = brakeTorque;
        WColForward.brakeTorque = brakeTorque;

        // --- Motor torque ---
        if (input.brake > 0.05f)
        {
            // disable engine torque while braking
            WColBack.motorTorque = 0f;
        }
        else if (input.acceleration > 0.05f && WColBack.GetGroundHit(out WheelHit hit))
        {
            WColBack.motorTorque = maxMotorTorque * input.acceleration;
        }
        else
        {
            WColBack.motorTorque = 0f;
        }

        // --- Simulate pitch under accel/brake ---
        float targetPitch = Mathf.Lerp(-5f, 5f, input.acceleration - input.brake);
        Quaternion targetRot = Quaternion.Euler(targetPitch, transform.eulerAngles.y, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 2f);
    }

    private void TuneFriction(WheelCollider wc, float sideStiffness, float forwardStiffness)
    {
        WheelFrictionCurve fwd = wc.forwardFriction;
        fwd.stiffness = forwardStiffness;
        fwd.extremumSlip = 0.3f;
        fwd.asymptoteSlip = 0.8f;
        wc.forwardFriction = fwd;

        WheelFrictionCurve side = wc.sidewaysFriction;
        side.stiffness = sideStiffness;
        side.extremumSlip = 0.3f;
        side.asymptoteSlip = 0.8f;
        wc.sidewaysFriction = side;
    }

    private void updateWheels()
    {
        float delta = Time.fixedDeltaTime;

        foreach (WheelData w in wheels)
        {
            WheelHit hit;
            Vector3 localPos = w.wheelTransform.localPosition;

            if (w.wheelCollider.GetGroundHit(out hit))
            {
                localPos.y -= Vector3.Dot(w.wheelTransform.position - hit.point, transform.up) - wheelRadius;
            }
            else
            {
                localPos.y = w.wheelStartPos.y - wheelOffset;
            }

            w.wheelTransform.localPosition = localPos;
            w.rotation = Mathf.Repeat(w.rotation + delta * w.wheelCollider.rpm * 360f / 60f, 360f);
            w.wheelTransform.localRotation = Quaternion.Euler(w.rotation, w.wheelCollider.steerAngle, 90f);
        }
    }

    void Awake()
    {
        if (gameObject.scene.name == "DontDestroyOnLoad")
        {
            Debug.LogError("❌ Motorcycle is in DontDestroyOnLoad — THIS IS A BUG");
        }

        Debug.Log($"MotoController Awake — Scene={gameObject.scene.name}");
    }


    void OnGUI()
    {
        GUI.color = Color.black;
        var area = new Rect(0, 0, 150, 60);
        GUI.Label(area, $"{speedVal:F1} m/s");
    }

    // --- Exposed getters for logging ---
    public float Speed => speedVal;
    public float SteerInput => hardwareSteering != null ? hardwareSteering.steering : 0f;
    public float AccelInput => hardwareThrottle != null ? hardwareThrottle.throttle : 0f;
    public float BrakeInput => hardwareThrottle != null ? hardwareThrottle.brake : 0f;
}
