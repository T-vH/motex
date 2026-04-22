using UnityEngine;
using UnityEngine.AI;

public class BackgroundTrafficManager : MonoBehaviour
{
    public NavMeshAgent[] npcCars;
    public Transform[] destinations;

    void Start()
    {
        foreach (var car in npcCars)
        {
            Transform dest = destinations[Random.Range(0, destinations.Length)];
            car.speed = Random.Range(8f, 14f);
            car.acceleration = Random.Range(10f, 20f);
            car.destination = dest.position;
        }
    }
}
