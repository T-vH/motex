using UnityEngine;

public class HazardTrigger : MonoBehaviour
{
    [Header("Optional (auto-filled if empty)")]
    public GameObject hazardCar;
    public DataLogger dataLogger;

    private HazardTTC_Merge mergeTTC;
    private bool triggered = false;

    private void Awake()
    {
        // Auto-bind DataLogger
        if (dataLogger == null)
            dataLogger = DataLogger.Instance;

        // Auto-find hazard car (first parent with StartMoving or TTC)
        if (hazardCar == null)
            hazardCar = GetComponentInParent<Rigidbody>()?.gameObject;

        if (hazardCar != null)
        {
            // Auto-detect merge TTC (only exists in L2 scenarios)
            mergeTTC = hazardCar.GetComponent<HazardTTC_Merge>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered)
            return;

        if (!other.CompareTag("Player"))
            return;

        triggered = true;

        // --- Activate merge TTC if present ---
        if (mergeTTC != null)
        {
            mergeTTC.ActivateTTC();
            Debug.Log("🟢 HazardTrigger: Merge TTC activated");
        }

        // --- Start hazard movement ---
        if (hazardCar != null)
        {
            hazardCar.SendMessage(
                "StartMoving",
                SendMessageOptions.DontRequireReceiver
            );
            Debug.Log($"🏍️ Motorcycle triggered {hazardCar.name}");
        }
        else
        {
            Debug.LogWarning("⚠️ HazardTrigger: No hazard car found");
        }

        // --- Logging (DataLogger is always-on now, so this is informational) ---
        if (dataLogger == null)
        {
            Debug.LogWarning("⚠️ HazardTrigger: DataLogger missing");
        }
    }
}
