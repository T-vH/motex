using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SimpleEngineAudio : MonoBehaviour
{
    public MotoController moto;
    public AudioClip engineClip;
    public AudioSource engineSource;

    [Header("Engine Behavior")]
    public float basePitch = 0.8f;
    public float maxPitch = 2.2f;
    public float baseVolume = 0.3f;
    public float maxVolume = 0.9f;
    public float speedForMaxPitch = 25f;

    [Header("Throttle Boost")]
    public float throttlePitchBoost = 0.3f;
    public float throttleFadeSpeed = 3f;

    private float throttleEffect = 0f;

    void Start()
    {
        // --- Audio Source Initialization ---
        if (engineSource == null)
            engineSource = GetComponent<AudioSource>();

        // Auto-assign and play if clip is set
        if (engineClip != null && engineSource != null)
        {
            engineSource.clip = engineClip;
            engineSource.loop = true;

            if (!engineSource.enabled)
                engineSource.enabled = true;

            if (!engineSource.isPlaying)
                engineSource.Play();

            Debug.Log($"🎧 EngineAudio initialized with '{engineClip.name}' on '{engineSource.name}'");
        }
        else
        {
            Debug.LogWarning("⚠️ EngineAudio: Missing AudioSource or AudioClip at Start()");
        }

        // --- Safety check for AudioListener ---
        if (FindObjectOfType<AudioListener>() == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.gameObject.AddComponent<AudioListener>();
                Debug.LogWarning("🔊 Added AudioListener to main camera at runtime.");
            }
            else
            {
                Debug.LogWarning("⚠️ EngineAudio: No camera found to attach AudioListener!");
            }
        }
    }

    void Update()
    {
        if (moto == null || engineSource == null)
            return;

        Rigidbody rb = moto.GetComponent<Rigidbody>();
        float speed = rb.velocity.magnitude;

        // Base pitch from speed
        float normSpeed = Mathf.Clamp01(speed / speedForMaxPitch);
        float targetPitch = Mathf.Lerp(basePitch, maxPitch, normSpeed);

        // Add short throttle punch (rev sound)
        float inputThrottle = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) ? 1f : 0f;
        throttleEffect = Mathf.MoveTowards(throttleEffect, inputThrottle, Time.deltaTime * throttleFadeSpeed);

        engineSource.pitch = targetPitch + throttleEffect * throttlePitchBoost;
        engineSource.volume = Mathf.Lerp(baseVolume, maxVolume, normSpeed);
    }
}
