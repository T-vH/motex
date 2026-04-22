using UnityEngine;
using TMPro;

public class SpeedometerDisplay3D : MonoBehaviour
{
    [Header("References")]
    public MotoController moto;         // drag your motorcycle object here
    public TextMeshPro speedText;       // assign the TextMeshPro text

    [Header("Display Settings")]
    public bool useKmh = true;
    public string unit = "km/h";
    public float smoothSpeed = 8f;
    public float angleTilt = 0f;        // small tilt if needed

    private float displayedSpeed = 0f;

    void Update()
    {
        if (moto == null || speedText == null) return;

        float targetSpeed = moto.Speed * (useKmh ? 3.6f : 1f); // convert to km/h
        displayedSpeed = Mathf.Lerp(displayedSpeed, targetSpeed, Time.deltaTime * smoothSpeed);

        speedText.text = $"{displayedSpeed:0}";

        // Optional: small tilt animation to follow curvature
        transform.localRotation = Quaternion.Euler(angleTilt, 0f, 0f);
    }
}
