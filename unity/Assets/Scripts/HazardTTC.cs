using UnityEngine;

public class HazardTTC : MonoBehaviour
{
    [Header("Auto-bound references (do NOT wire manually unless needed)")]
    public Transform moto;
    public Rigidbody motoRb;
    public Rigidbody hazardRb;
    public DataLogger dataLogger;

    [Header("Settings")]
    public float warningLeadTime = 3f;

    private FeedbackManager feedbackManager;
    private bool warningActive = false;

    private bool initialized = false;

    // --------------------------------------------------
    // Initialization
    // --------------------------------------------------

    private void OnEnable()
    {
        InitializeReferences();
    }

    private void InitializeReferences()
    {
        if (initialized) return;

        // --- Hazard Rigidbody: always this object ---
        if (hazardRb == null)
            hazardRb = GetComponent<Rigidbody>();

        if (hazardRb == null)
        {
            Debug.LogError($"❌ [HazardTTC] No Rigidbody on hazard car ({name})");
            return;
        }

        // --- Motorcycle ---
        if (moto == null)
        {
            GameObject motoObj = GameObject.FindGameObjectWithTag("Player");
            if (motoObj != null)
                moto = motoObj.transform;
        }

        if (moto != null && motoRb == null)
            motoRb = moto.GetComponent<Rigidbody>();

        if (moto == null || motoRb == null)
        {
            Debug.LogError("❌ [HazardTTC] Motorcycle or Rigidbody not found (check Player tag)");
            return;
        }

        // --- Managers ---
        if (dataLogger == null)
            dataLogger = DataLogger.Instance;

        if (dataLogger == null)
        {
            Debug.LogError("❌ [HazardTTC] DataLogger missing");
            return;
        }

        feedbackManager = FeedbackManager.Instance;
        if (feedbackManager == null)
        {
            Debug.LogError("❌ [HazardTTC] FeedbackManager missing");
            return;
        }

        initialized = true;
        Debug.Log($"✅ [HazardTTC] Initialized on {name}");
    }

    // --------------------------------------------------
    // TTC logic
    // --------------------------------------------------

    private void Update()
    {
        if (!initialized)
            return;

        Vector3 relPos = hazardRb.position - moto.position;
        Vector3 relVel = hazardRb.velocity - motoRb.velocity;

        float closingSpeed = Vector3.Dot(-relVel, relPos.normalized);
        if (closingSpeed <= 0f)
        {
            StopWarningIfActive();
            return;
        }

        float distance = relPos.magnitude;
        float TTC = distance / closingSpeed;

        Vector3 dirToHazard = relPos;
        dirToHazard.y = 0f;
        float angle = Vector3.SignedAngle(moto.forward, dirToHazard, Vector3.up);

        feedbackManager.UpdateAngle(angle);

        if (TTC <= warningLeadTime && !warningActive)
        {
            feedbackManager.StartWarning();
            warningActive = true;
        }
        else if (TTC > warningLeadTime && warningActive)
        {
            StopWarningIfActive();
        }

        Vector3 predictedCrashPos =
            motoRb.velocity.sqrMagnitude > 0.01f
                ? moto.position + motoRb.velocity.normalized * (closingSpeed * TTC)
                : moto.position;

        dataLogger.UpdateHazardData(TTC, predictedCrashPos, angle);
    }

    private void StopWarningIfActive()
    {
        if (!warningActive) return;

        feedbackManager.StopWarning();
        warningActive = false;
    }
}
