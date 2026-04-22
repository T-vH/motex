using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NavMeshLaneFollower : MonoBehaviour
{
    [Header("Lane Settings")]
    public float laneOffset = 2f; // +right lane, -left lane
    public float laneCorrectionStrength = 3f;
    public Transform destination;

    private NavMeshAgent agent;
    private Vector3 laneRight;
    private Vector3 lastValidPos;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updatePosition = true;
        agent.updateRotation = true;
        agent.autoBraking = false;
        agent.acceleration = 8f;
        agent.angularSpeed = 120f;
        agent.speed = Random.Range(6f, 10f);

        if (destination != null)
            agent.SetDestination(destination.position);

        lastValidPos = transform.position;
    }

    void Update()
    {
        if (destination == null) return;

        // 1️⃣ Get the agent’s desired direction from its path
        if (agent.hasPath && agent.path.corners.Length > 1)
        {
            Vector3 toNext = (agent.path.corners[1] - agent.transform.position).normalized;

            // 2️⃣ Compute lateral offset direction (perpendicular to path)
            laneRight = Vector3.Cross(Vector3.up, toNext).normalized;

            // 3️⃣ Calculate target lane position
            Vector3 laneTarget = transform.position + laneRight * laneOffset;

            // 4️⃣ Move slightly toward the lane target position
            Vector3 correction = laneTarget - transform.position;
            agent.Move(correction * Time.deltaTime * laneCorrectionStrength);
        }

        // 5️⃣ Smooth rotation
        if (agent.velocity.sqrMagnitude > 0.1f)
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(agent.velocity.normalized, Vector3.up), Time.deltaTime * 5f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + laneRight * laneOffset);
    }

}
