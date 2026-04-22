using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class DirectionalWarningAudio : MonoBehaviour
{
    private AudioSource src;
    public ProceduralWarningTone toneGen;   // <— reference to the procedural tone generator
    public float distanceFromHead = 0.25f;
    public float spreadAngle = 0f;          // optional, unused here

    void Awake()
    {
        src = GetComponent<AudioSource>();
        src.spatialBlend = 1f;
        src.dopplerLevel = 0f;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.maxDistance = 1f;
        src.playOnAwake = false;

        // auto-assign tone generator if on same object
        if (toneGen == null)
            toneGen = GetComponent<ProceduralWarningTone>();
    }

    /// <summary>
    /// Projects a short sound at the given hazard angle (degrees around the rider’s head)
    /// and plays a procedurally generated tone from that direction.
    /// </summary>
    public void PlayDirectionalWarning(float angleDeg)
    {
        Debug.Log($"[Audio] PlayDirectionalWarning called at {angleDeg:F1}°");

        Quaternion dir = Quaternion.Euler(0f, angleDeg, 0f);
        Vector3 offset = dir * Vector3.forward * distanceFromHead; // e.g., 0.25 m radius
        transform.localPosition = offset;
        transform.localRotation = dir;

        if (toneGen != null)
            toneGen.PlayTone(800f, 0.3f, 1f);
    }

}
