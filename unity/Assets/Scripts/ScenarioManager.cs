using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class ScenarioManager : MonoBehaviour
{
    public static ScenarioManager Instance { get; private set; }

    [System.Serializable]
    public struct Trial
    {
        public string scene;
        public FeedbackMode modality;
    }

    [Header("Generated trials (runtime)")]
    public List<Trial> trials = new List<Trial>();

    public int CurrentTrialIndex { get; private set; } = -1;
    public int CurrentTrialNumber => CurrentTrialIndex + 1;
    public bool ExperimentCompleted { get; private set; } = false;


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

    private void Start()
    {
        GenerateRandomTrials();
        StartNextTrial();
    }


    private void GenerateRandomTrials()
    {
        string[] baseScenarios = { "C1", "C2", "L2", "T7" };
        char[] variants = { 'A', 'B', 'C', 'D' };

        FeedbackMode[] modalities =
        {
            FeedbackMode.Control,
            FeedbackMode.Visual,
            FeedbackMode.Audio,
            FeedbackMode.Multimodal
        };

        trials.Clear();

        foreach (var baseScenario in baseScenarios)
        {
            // Create shuffled copies so pairing is random per participant
            List<char> shuffledVariants = new List<char>(variants);
            List<FeedbackMode> shuffledModalities = new List<FeedbackMode>(modalities);

            Shuffle(shuffledVariants);
            Shuffle(shuffledModalities);

            // Pair variant[i] with modality[i]
            for (int i = 0; i < 4; i++)
            {
                trials.Add(new Trial
                {
                    scene = $"{baseScenario}{shuffledVariants[i]}",
                    modality = shuffledModalities[i]
                });
            }
        }

        // Finally shuffle the full trial order
        Shuffle(trials);

        Debug.Log($"🧪 Generated {trials.Count} counterbalanced trials");
    }


    public void StartNextTrial()
    {
        // 🔒 Do nothing if experiment already ended
        if (ExperimentCompleted)
            return;

        CurrentTrialIndex++;

    if (CurrentTrialIndex >= trials.Count)
    {
        Debug.Log("✅ Experiment complete");
        return;
    }


        var trial = trials[CurrentTrialIndex];
        Debug.Log($"▶️ Loading Trial {CurrentTrialNumber}: {trial.scene} | {trial.modality}");

        SceneManager.LoadScene(trial.scene);
    }

    public void RestartCurrentTrial()
    {
        if (CurrentTrialIndex < 0 || CurrentTrialIndex >= trials.Count)
        {
            Debug.LogWarning("⚠️ Cannot restart trial: invalid index");
            return;
        }

        var trial = trials[CurrentTrialIndex];

        Debug.Log($"🔁 Restarting Trial {CurrentTrialNumber}: {trial.scene} | {trial.modality}");

        // Tell DataLogger to restart logging for THIS trial
        DataLogger.Instance?.RestartTrialLogging();

        // Reload same scene
        SceneManager.LoadScene(trial.scene);
    }

    public Trial GetCurrentTrial()
    {
        return trials[CurrentTrialIndex];
    }

    private void Shuffle<T>(IList<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int r = Random.Range(i, list.Count);
            (list[i], list[r]) = (list[r], list[i]);
        }
    }

}
