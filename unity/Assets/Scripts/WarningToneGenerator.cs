using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class WarningToneGenerator : MonoBehaviour
{
    private AudioSource src;
    private float frequency = 800f;   // Hz
    private float duration = 0.25f;   // seconds
    private bool playing = false;

    void Awake()
    {
        src = GetComponent<AudioSource>();
        src.spatialBlend = 1f;  // 3D
        src.spread = 20f;
        src.playOnAwake = false;
    }

    public void PlayTone(float freq, float dur)
    {
        if (playing) return;
        StartCoroutine(GenerateTone(freq, dur));
    }

    IEnumerator GenerateTone(float freq, float dur)
    {
        playing = true;

        int sampleRate = 44100;
        int samples = Mathf.CeilToInt(sampleRate * dur);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
            data[i] = Mathf.Sin(2 * Mathf.PI * freq * i / sampleRate);

        var clip = AudioClip.Create("WarnTone", samples, 1, sampleRate, false);
        clip.SetData(data, 0);

        src.clip = clip;
        src.Play();

        yield return new WaitForSeconds(dur);
        playing = false;
    }
}
