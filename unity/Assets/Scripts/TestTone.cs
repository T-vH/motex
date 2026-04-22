using UnityEngine;

public class TestTone : MonoBehaviour
{
    public ProceduralWarningTone tone;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            tone.PlayTone(800f, 0.5f, 1f);
            Debug.Log("Tone test triggered");
        }
    }
}
