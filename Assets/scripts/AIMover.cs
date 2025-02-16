using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIMover : MonoBehaviour
{
    public Transform target;
    public float speed = 5f;

    // Variables for the BOIDS algorithm.
    private float maxSpeed = 10f;
    private float maxForce = 10f;
    private float slowingRadius = 1f;
    private float seperationWeight = 1f;

    private Pathfinder pathfinder;
    private List<Vector3> path;
    private int currentWaypoint = 0;

    private Vector3 previousSteering = Vector3.zero;
    private Vector3 steeringVelocity = Vector3.zero;

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
            yield return new WaitForSeconds(10f); // Update path every 10 seconds
        }
    }

    // Update function rewritten to accompany new steering mechanics.
    void Update()
    {
        // Early out if path doesnt exist.
        if (path == null || currentWaypoint >= path.Count) return;

        Vector3 waypointTarget = path[currentWaypoint];

        // We need to check if the NPC is close the next waypoint.
        if (Vector3.Distance(transform.position, waypointTarget) < 0.5f)
        {
            currentWaypoint++;
            if (currentWaypoint >= path.Count)
            {
                isAtDestination = true;
            }
        }

        // Call the compute steering force function.
        Vector3 steering = ComputeSteeringForce();

        // Update the currentVelocity with the steering force previously calculated.
        Vector3 smoothedSteering = Vector3.SmoothDamp(previousSteering, steering, ref steeringVelocity, 0.3f);
        previousSteering = smoothedSteering;

        currentVelocity.y = 0;
        smoothedSteering.y = 0;

        currentVelocity += smoothedSteering * Time.deltaTime;
        currentVelocity = Vector3.ClampMagnitude(currentVelocity, maxSpeed);

        // Update the agents position based on the new velocity calculated.
        transform.position += currentVelocity * Time.deltaTime;

        // Roatation?
    }

    Vector3 ComputeSteeringForce()
    {
        // Default to the target position, if waypoint cant be located.
        Vector3 waypointTarget = target.position;

        // If we have a valid path with waypoints, use the current waypoint
        if (path != null && path.Count > 0 && currentWaypoint < path.Count)
        {
            waypointTarget = path[currentWaypoint];
        }

        // Compute the Arrival behavior.
        Vector3 desired = waypointTarget - transform.position;
        float distance = desired.magnitude;
        desired.Normalize();

        // If we are inside the slowing radius, cut the max speed proportional to the distance.
        if (distance < slowingRadius && currentWaypoint == path.Count - 1 && path != null)
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

    public void SetTargetPosition(Vector3 newPosition)
    {
        target.position = newPosition;
        currentWaypoint = 0;
        isAtDestination = false;
        path = null;
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