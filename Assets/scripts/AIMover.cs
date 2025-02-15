using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIMover : MonoBehaviour
{
    public Transform target;
    public float speed = 5f;

    // Variables for the BOIDS algorithm.
    private float maxSpeed = 5f;
    private float maxForce = 10f;
    private float slowingRadius = 3f;
    private float seperationWeight = 1f;

    private Pathfinder pathfinder;
    private List<Vector3> path;
    private int currentWaypoint = 0;

    public bool isAtDestination;

    // Maintain a reference globally about the current speed of the AI.
    private Vector3 currentVelocity;

    void Start()
    {
        pathfinder = FindObjectOfType<Pathfinder>();
        isAtDestination = true;
        //if(pathfinder != null ) { StartCoroutine(UpdatePath()); }


    }

    public IEnumerator UpdatePath()
    {
        while (true)
        {
            if (target != null)
            {
                path = pathfinder.FindPath(transform.position, target.position);
                if (path != null && path.Count > 0)
                {
                    Debug.Log("Path found!");
                    isAtDestination = false;
                    currentWaypoint = 0;
                }
            }
            yield return new WaitForSeconds(10f); // Update path every 0.5 seconds
        }
    }

    // Update function rewritten to accompany new steering mechanics.
    void Update()
    {

    }

    Vector3 ComputeSteeringForce()
    {
        // Compute the Arrival behavior.
        Vector3 desired = target.position - transform.position;
        float distance = desired.magnitude;
        desired.Normalize();

        // If we are inside the slowing radius, cut the max speed proportional to the distance.
        if (distance < slowingRadius)
        {
            desired *= maxSpeed * (distance / slowingRadius);
        }
        else
        {
            desired *= maxSpeed;
        }

        Vector3 steeringForce = desired - currentVelocity;

        // Here we need to attempt to create a seperation force as well as, unit cohesion.

        // We need to clamp the steeringForce, otherwise it could exceed the maxForce causing undesired behavior.
        steeringForce = Vector3.ClampMagnitude(steeringForce, maxForce);
        return steeringForce;
    }

    void OnDrawGizmos()
    {
        if (path != null)
        {
            Gizmos.color = Color.blue;
            foreach (Vector3 point in path)
            {
                Gizmos.DrawSphere(point, 0.2f);
            }
        }
    }
}