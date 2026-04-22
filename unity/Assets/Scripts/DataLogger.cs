using UnityEngine;
using System.IO;
using System.Globalization;

public class DataLogger : MonoBehaviour
{
    public static DataLogger Instance { get; private set; }

    [Header("Participant")]
    [Tooltip("Set this ONCE before pressing Play, e.g. P01, P02, ...")]
    public string participantID = "P01";

    [Header("Logging Settings")]
    [Tooltip("Seconds between log samples (0.1 = 10 Hz)")]
    public float logInterval = 0.05f;

    // --- Runtime ---
    private StreamWriter writer;
    private float lastLogTime;
    private bool logging;
    private MotoController moto;

    // --- Hazard data (updated externally) ---
    private float currentTTC = float.PositiveInfinity;
    private Vector3 predictedCrashPos = Vector3.zero;
    private float crashAngle = 0f;

    // --- Session ---
    private string sessionTimestamp;

    // --------------------------------------------------
    // Lifecycle
    // --------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        sessionTimestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");

        Debug.Log($"📝 DataLogger ready | Participant={participantID} | Session={sessionTimestamp}");
    }

    private void OnApplicationQuit()
    {
        StopTrialLogging();
    }

    // --------------------------------------------------
    // Trial-level control
    // --------------------------------------------------

    /// <summary>
    /// Called AFTER a new scene is loaded and modality is set
    /// </summary>
    public void StartTrialLogging()
    {
        StopTrialLogging(); // safety

        moto = FindObjectOfType<MotoController>(true);
        if (moto == null)
        {
            Debug.LogError("❌ DataLogger: MotoController not found — cannot start logging");
            return;
        }

        if (ScenarioManager.Instance == null)
        {
            Debug.LogError("❌ DataLogger: ScenarioManager missing");
            return;
        }

        var trial = ScenarioManager.Instance.GetCurrentTrial();
        int trialNr = ScenarioManager.Instance.CurrentTrialNumber;

        string folder =
            Application.dataPath + $"/Logs/{participantID}/";
        Directory.CreateDirectory(folder);

        string filePath =
            $"{folder}MOTEX_{participantID}_{sessionTimestamp}_" +
            $"Trial{trialNr:D2}_{trial.scene}_{trial.modality}.csv";

        writer = new StreamWriter(filePath, false);
        writer.WriteLine(
            "Time;Trial;Scene;Modality;" +
            "PosX;PosZ;Speed;" +
            "Steer;Throttle;Brake;" +
            "TTC;PredCrashX;PredCrashZ;CrashAngle"
        );


        writer.Flush();

        lastLogTime = 0f;
        logging = true;

        Debug.Log($"🟢 Trial {trialNr} logging STARTED → {Path.GetFileName(filePath)}");
    }
   
    public void RestartTrialLogging()
    {
        // Close current writer safely
        try
        {
            writer?.Flush();
            writer?.Close();
        }
        catch { }

        logging = false;

        // Re-open the same trial file
        StartTrialLogging();

        Debug.Log($"🔁 Trial {ScenarioManager.Instance.CurrentTrialNumber} logging RESTARTED");
    }

    /// <summary>
    /// Called when pressing N (before loading next trial)
    /// </summary>
    public void StopTrialLogging()
    {
        if (!logging)
            return;

        logging = false;

        writer?.Flush();
        writer?.Close();
        writer = null;

        Debug.Log("🔴 Trial logging STOPPED");
    }

    // --------------------------------------------------
    // Continuous logging
    // --------------------------------------------------

    private void FixedUpdate()
    {
        if (!logging || writer == null || moto == null)
            return;

        if (Time.time - lastLogTime < logInterval)
            return;

        lastLogTime = Time.time;

        Vector3 p = moto.transform.position;

        writer.WriteLine(string.Format(
            CultureInfo.InvariantCulture,
            "{0:F3};{1};{2};{3};" +
            "{4:F3};{5:F3};{6:F2};" +
            "{7:F3};{8:F3};{9:F3};" +
            "{10:F2};{11:F3};{12:F3};{13:F1}",
            Time.time,
            ScenarioManager.Instance.CurrentTrialNumber,
            ScenarioManager.Instance.GetCurrentTrial().scene,
            ScenarioManager.Instance.GetCurrentTrial().modality,
            p.x,
            p.z,
            moto.Speed,
            moto.SteerInput,
            moto.AccelInput,
            moto.BrakeInput,
            currentTTC,
            predictedCrashPos.x,
            predictedCrashPos.z,
            crashAngle
        ));

    }

    // --------------------------------------------------
    // Hazard / TTC input
    // --------------------------------------------------

    /// <summary>
    /// Called continuously by HazardTTC / HazardTTC_Merge
    /// </summary>
    public void UpdateHazardData(float ttc, Vector3 predPos, float angle)
    {
        currentTTC = ttc;
        predictedCrashPos = predPos;
        crashAngle = angle;
    }
}
