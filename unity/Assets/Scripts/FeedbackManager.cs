using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public enum FeedbackMode
{
    Control,
    Visual,
    Audio,
    Multimodal
}

public class FeedbackManager : MonoBehaviour
{
    public static FeedbackManager Instance { get; private set; }

    [Header("Outputs (auto-assigned per scene)")]
    public VirtualLEDStripUI ledStrip;
    public DirectionalWarningAudio directionalAudio;

    [Header("Timing")]
    public float cueInterval = 0.3f;

    private FeedbackMode currentMode = FeedbackMode.Control;
    private float currentAngle;
    private bool isActive;
    private Coroutine routine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(RebindSceneOutputs());
    }

    private IEnumerator RebindSceneOutputs()
    {
        yield return null;
        yield return new WaitForSeconds(0.1f);

        ledStrip = FindObjectOfType<VirtualLEDStripUI>(true);
        directionalAudio = FindObjectOfType<DirectionalWarningAudio>(true);

        Debug.Log(
            $"🔌 FeedbackManager rebound | " +
            $"LED={(ledStrip != null)} | Audio={(directionalAudio != null)}"
        );
    }

    public void SetMode(FeedbackMode mode)
    {
        currentMode = mode;
        StopWarning();
        ledStrip?.ResetStrip();

        Debug.Log($"🎛️ Feedback mode set to {currentMode}");
    }

    public void UpdateAngle(float angle)
    {
        currentAngle = angle;
    }

    public void StartWarning()
    {
        if (isActive || currentMode == FeedbackMode.Control)
            return;

        isActive = true;
        routine = StartCoroutine(FeedbackLoop());

        Debug.Log($"⚠️ Feedback START ({currentMode})");
    }

    public void StopWarning()
    {
        if (!isActive) return;

        isActive = false;

        if (routine != null)
            StopCoroutine(routine);

        ledStrip?.ResetStrip();

        Debug.Log($"✅ Feedback STOP ({currentMode})");
    }

    private IEnumerator FeedbackLoop()
    {
        while (isActive)
        {
            if ((currentMode == FeedbackMode.Visual || currentMode == FeedbackMode.Multimodal)
                && ledStrip != null)
            {
                ledStrip.HighlightDirection(currentAngle);
            }

            if ((currentMode == FeedbackMode.Audio || currentMode == FeedbackMode.Multimodal)
                && directionalAudio != null)
            {
                directionalAudio.PlayDirectionalWarning(currentAngle);
            }

            yield return new WaitForSeconds(cueInterval);
        }
    }
}
