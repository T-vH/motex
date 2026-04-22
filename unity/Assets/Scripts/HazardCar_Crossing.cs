using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class HazardCar_Crossing: MonoBehaviour
{
    [Header("Navigation Settings")]
    public Transform destination;
    public float startDelay = 0f;   // optional small delay after trigger

    private NavMeshAgent agent;
    private bool active = false;
    private float delayTimer = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.isStopped = true;       // idle until triggered
        agent.updateRotation = true;  // let NavMesh control facing direction
    }

    void Update()
    {
        if (!active)
            return;

        // Optional delay before moving
        if (delayTimer < startDelay)
        {
            delayTimer += Time.deltaTime;
            return;
        }

        // Start moving once delay has passed
        if (agent.isStopped)
        {
            agent.isStopped = false;
            if (destination != null)
                agent.SetDestination(destination.position);
        }

        // Stop when close to destination
        if (destination != null && !agent.pathPending && agent.remainingDistance < 0.5f)
        {
            agent.isStopped = true;
            active = false;
        }
    }

    // ✅ Called by the trigger zone
    public void StartMoving()
    {
        if (active) return;

        active = true;
        delayTimer = 0f;
    }
}
