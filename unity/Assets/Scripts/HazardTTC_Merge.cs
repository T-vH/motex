using UnityEngine;

public class HazardTTC_Merge : MonoBehaviour
{
    [Header("References")]
    public Transform moto;
    public Rigidbody motoRb;
    public Rigidbody hazardRb;
    public DataLogger dataLogger;

    [Header("Warning Settings")]
    public float warningLeadTime = 2.0f;
    public float forwardAngleThreshold = 45f;
    public float minDistanceForWarning = 3f;
    public float maxDistanceForWarning = 60f;
    public float minClosingSpeed = 0.5f;
    public float logInterval = 0.1f;

    [Header("Activation")]
    public float activationGraceTime = 0.3f; // ignore first frames after trigger

    private FeedbackManager feedbackManager;
    private float lastLogTime = 0f;
    private bool warningActive = false;
    private bool ttcActive = false;
    private float activationTime = 0f;

    // --- Called by the trigger zone ---
    public void ActivateTTC()
    {
        ttcActive = true;
        activationTime = Time.time;
        Debug.Log("🟢 HazardTTC_Merge: TTC monitoring activated");
    }

    private void OnEnable()
    {
        dataLogger = dataLogger ?? FindObjectOfType<DataLogger>(true);

        if (dataLogger == null)
            Debug.LogError("❌ [HazardTTC_Merge] DataLogger missing");
    }

    void Update()
{
    if (!ttcActive)
        return;

    if (Time.time - activationTime < activationGraceTime)
        return;

    if (feedbackManager == null)
    {
        feedbackManager = FeedbackManager.Instance;
        if (feedbackManager == null)
            return;
    }

    if (moto == null || motoRb == null || hazardRb == null)
        return;

    Vector3 relPos = hazardRb.position - moto.position;
    Vector3 relVel = hazardRb.velocity - motoRb.velocity;

    float distance = relPos.magnitude;
    float closingSpeed = Vector3.Dot(-relVel, relPos.normalized);

    float TTC = float.PositiveInfinity;
    float angle = 0f;
    Vector3 predictedCrashPos = moto.position;

    // --- Validity checks ---
    bool valid =
        distance >= minDistanceForWarning &&
        distance <= maxDistanceForWarning &&
        closingSpeed > minClosingSpeed;

    if (valid)
    {
        TTC = distance / closingSpeed;

        Vector3 dirToHazard = relPos;
        dirToHazard.y = 0f;
        angle = Vector3.SignedAngle(moto.forward, dirToHazard, Vector3.up);

        if (Mathf.Abs(angle) > forwardAngleThreshold)
            valid = false;
    }

    // --- Feedback ---
    if (valid && TTC <= warningLeadTime && !warningActive)
    {
        feedbackManager.StartWarning();
        warningActive = true;
    }
    else if ((!valid || TTC > warningLeadTime) && warningActive)
    {
        StopWarningIfActive();
    }

    feedbackManager.UpdateAngle(angle);

    // --- Predicted crash position ---
    if (valid && motoRb.velocity.sqrMagnitude > 0.01f)
    {
        predictedCrashPos =
            moto.position + motoRb.velocity.normalized * (closingSpeed * TTC);
    }

    // ✅ ALWAYS propagate TTC state
    dataLogger.UpdateHazardData(TTC, predictedCrashPos, angle);
}


    private void StopWarningIfActive()
    {
        if (!warningActive) return;

        feedbackManager.StopWarning();
        warningActive = false;
    }
}
