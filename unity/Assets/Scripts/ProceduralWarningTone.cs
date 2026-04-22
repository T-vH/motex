using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class ProceduralWarningTone : MonoBehaviour
{
    private AudioSource src;

    void Awake()
    {
        src = GetComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 1f;
        src.dopplerLevel = 0f;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.maxDistance = 1f;
    }

    /// <summary>
    /// Generates and plays a simple sine-wave tone.
    /// </summary>
    public void PlayTone(float frequency = 800f, float duration = 0.25f, float volume = 1f)
    {
        StartCoroutine(GenerateAndPlay(frequency, duration, volume));
    }

    private IEnumerator GenerateAndPlay(float freq, float dur, float vol)
    {
        int sampleRate = 44100;
        int samples = Mathf.CeilToInt(sampleRate * dur);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
            data[i] = Mathf.Sin(2 * Mathf.PI * freq * i / sampleRate);

        AudioClip clip = AudioClip.Create("WarnTone", samples, 1, sampleRate, false);
        clip.SetData(data, 0);

        src.volume = 1;
        src.clip = clip;
        src.Play();

        yield return new WaitForSeconds(dur);
    }
}
