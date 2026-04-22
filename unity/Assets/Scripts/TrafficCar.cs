using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class TrafficCar : MonoBehaviour
{
    public Transform destination;
    private NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // ✅ Let Unity move AND rotate the car
        agent.updatePosition = true;
        agent.updateRotation = true;

        if (destination != null)
            agent.SetDestination(destination.position);
        else
            Debug.LogWarning($"[{name}] No destination assigned!");
    }
}
