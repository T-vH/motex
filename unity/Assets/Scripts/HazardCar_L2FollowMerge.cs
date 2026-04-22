using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class HazardCar_L2FollowMerge : MonoBehaviour
{
    [Header("References")]
    public Transform motorcycle;
    public Rigidbody motorcycleRb;

    [Header("Behavior Settings")]
    [Tooltip("Meters ahead of the motorcycle to maintain while following")]
    public float followOffset = 5f;

    [Tooltip("How much faster than the motorcycle (m/s)")]
    public float speedLead = 1.5f;

    [Tooltip("Lateral speed during merge (m/s)")]
    public float lateralSpeed = 2.0f;

    [Tooltip("How far sideways the car moves to merge (lane width)")]
    public float mergeDistance = 2.2f;

    private Rigidbody rb;

    // State
    private bool mergeStarted = false;

    // Forward speed locked at moment of trigger
    private float lockedForwardSpeed = 0f;

    private Vector3 startPos;
    private Vector3 targetLateralPos;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (motorcycle != null && motorcycleRb == null)
            motorcycleRb = motorcycle.GetComponent<Rigidbody>();

        startPos = transform.position;
        targetLateralPos = startPos; // initial lateral lane
    }

    void FixedUpdate()
    {
        if (motorcycle == null || motorcycleRb == null)
            return;

        float dt = Time.fixedDeltaTime;

        if (!mergeStarted)
        {
            // -------- FOLLOW MODE (before trigger) --------
            float motoSpeed = motorcycleRb.velocity.magnitude;
            float targetSpeed = motoSpeed + speedLead;

            // Move toward a point ahead of the motorcycle (world-relative)
            Vector3 targetPos = motorcycle.position - Vector3.right * followOffset;
            targetPos.z = targetLateralPos.z; // stay in current lane

            Vector3 nextPos = Vector3.MoveTowards(rb.position, targetPos, targetSpeed * dt);
            rb.MovePosition(nextPos);
        }
        else
        {
            // -------- MERGE / POST-TRIGGER MODE --------

            // Move forward relative to car’s facing direction (transform.forward)
            Vector3 forwardStep = transform.forward * lockedForwardSpeed * dt;

            // Move sideways relative to car (transform.right)
            Vector3 currentPos = rb.position;
            Vector3 lateralTarget = new Vector3(
                currentPos.x,
                currentPos.y,
                targetLateralPos.z
            );

            // Move toward lateral target (left or right depending on setup)
            float step = lateralSpeed * dt;
            Vector3 newPos = Vector3.MoveTowards(currentPos, lateralTarget, step);

            // Add forward motion (using transform.forward)
            newPos += forwardStep;

            rb.MovePosition(newPos);
        }
    }

    // ✅ Called by the trigger when the motorcycle enters the zone
    public void StartMoving()
    {
        if (mergeStarted) return;

        mergeStarted = true;

        // Lock the forward speed to whatever the motorcycle is doing at trigger time
        lockedForwardSpeed = (motorcycleRb != null) ? motorcycleRb.velocity.magnitude : 0f;

        // Compute new lateral offset based on car's local direction
        startPos = transform.position;
        targetLateralPos = startPos - transform.right * mergeDistance; // merge left (relative to car)
        Debug.Log($"🚗 HazardCar_L2: Merge started. Locked speed = {lockedForwardSpeed:F2} m/s");
    }
}
