using UnityEngine;
using System.Collections;

public class ScenarioInitializer : MonoBehaviour
{
    private FeedbackManager feedbackManager;

    private void OnEnable()
    {
        StartCoroutine(InitializeAfterSceneReady());
    }

    private IEnumerator InitializeAfterSceneReady()
    {
        yield return null;
        yield return new WaitForSeconds(0.1f);

        if (ScenarioManager.Instance == null)
            yield break;

        var feedbackManager = FeedbackManager.Instance;
        if (feedbackManager == null)
            yield break;

        var trial = ScenarioManager.Instance.GetCurrentTrial();

        feedbackManager.SetMode(trial.modality);

        // 🔴 THIS LINE IS REQUIRED
        DataLogger.Instance.StartTrialLogging();

        Debug.Log(
            $"✅ Scenario initialized | " +
            $"Trial={ScenarioManager.Instance.CurrentTrialNumber} | " +
            $"Scene={trial.scene} | " +
            $"Mode={trial.modality}"
        );
    }

    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.N))
        {
            ScenarioManager.Instance.StartNextTrial();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            ScenarioManager.Instance.RestartCurrentTrial();
        }
    }

}
