using UnityEngine;

public class NPCVehicle : MonoBehaviour
{
    public Transform[] waypoints;
    public float speed = 10f;
    private int currentIndex = 0;
    private bool moving = true;

    void Update()
    {
        if (!moving || waypoints.Length == 0) return;

        Transform target = waypoints[currentIndex];
        Vector3 direction = (target.position - transform.position).normalized;

        // Move toward next waypoint
        transform.position += direction * speed * Time.deltaTime;
        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            Quaternion.LookRotation(direction),
            Time.deltaTime * 5f
        );

        // Check distance to target
        if (Vector3.Distance(transform.position, target.position) < 1f)
        {
            currentIndex++;
            if (currentIndex >= waypoints.Length)
            {
                moving = false; // reached end
            }
        }
    }

    public void Stop() => moving = false;
    public void Resume() => moving = true;
}
