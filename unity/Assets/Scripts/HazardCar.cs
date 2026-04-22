using UnityEngine;
using UnityEngine.AI;

public class HazardCar : MonoBehaviour
{
    private NavMeshAgent agent;
    public Transform endPoint; // assigned in Inspector

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = true;
        agent.isStopped = true;
    }

    public void ActivateHazard()
    {
        if (endPoint == null)
        {
            Debug.LogWarning($"[{name}] No endPoint assigned!");
            return;
        }

        agent.isStopped = false;
        agent.SetDestination(endPoint.position);
    }
}
