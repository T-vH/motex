using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class HazardCar_CrossingT7 : MonoBehaviour
{
    [Header("Navigation Points")]
    public Transform firstDestination;   // stop line position
    public Transform secondDestination;  // after trigger, crosses into lane

    [Header("Timings")]
    public float waitAfterFirst = 1.5f;  // seconds waiting before trigger (visual pause)
    public float startDelayAfterTrigger = 0.3f; // reaction delay after trigger

    private NavMeshAgent agent;
    private bool reachedFirst = false;
    private bool triggered = false;
    private bool movingToSecond = false;
    private float waitTimer = 0f;
    private float triggerDelayTimer = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = true;
        agent.isStopped = false;

        if (firstDestination != null)
            agent.SetDestination(firstDestination.position);
    }

    void Update()
    {
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            if (!reachedFirst)
            {
                // --- Reached first point ---
                reachedFirst = true;
                agent.isStopped = true;
                waitTimer = 0f;
            }
            else if (movingToSecond && !agent.pathPending && agent.remainingDistance < 0.5f)
            {
                // --- Reached second point ---
                agent.isStopped = true;
                movingToSecond = false;
            }
        }

        // --- Waiting at first point ---
        if (reachedFirst && !movingToSecond && !triggered)
        {
            waitTimer += Time.deltaTime;
            // just idle visually before trigger
        }

        // --- After trigger ---
        if (triggered && !movingToSecond)
        {
            triggerDelayTimer += Time.deltaTime;
            if (triggerDelayTimer >= startDelayAfterTrigger)
            {
                StartSecondMovement();
            }
        }
    }

    // Triggered by HazardTrigger when the motorcycle enters
    public void StartMoving()
    {
        if (!reachedFirst)
        {
            // if car hasn't reached first yet, just keep going there
            return;
        }

        if (!triggered)
        {
            triggered = true;
            triggerDelayTimer = 0f;
        }
    }

    private void StartSecondMovement()
    {
        if (secondDestination == null)
        {
            return;
        }

        agent.isStopped = false;
        agent.SetDestination(secondDestination.position);
        movingToSecond = true;
    }
}
