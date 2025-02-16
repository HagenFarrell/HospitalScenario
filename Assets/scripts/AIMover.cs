using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIMover : MonoBehaviour
{
    // Variables for the BOIDS algorithm.
    [SerializeField] private float speed = 5f;
    [SerializeField] private float maxSpeed = 10f;
    [SerializeField] private float maxForce = 5f;
    [SerializeField] private float slowingRadius = 1f;
    [SerializeField] private float seperationRadius = 2f;
    [SerializeField] private float pathUpdateInterval = 0.5f;


    private Pathfinder pathfinder;
    private List<Vector3> path;
    private int currentWaypoint = 0;

    private Vector3 previousSteering = Vector3.zero;

    /* For anyone reading this that doesnt understand.
     * get: means other classes can read the value
     * Private set: means only the class containing this property can modify the value. 
     * (So this class we are currently in. :D )
     */
    public bool isAtDestination { get; private set; }

    // Maintain a reference globally about the current speed of the AI.
    private Vector3 currentVelocity;

    private Transform _target;
    public Transform target
    {
        get
        {
            if (_target == null)
            {
                // Create a new GameObject for the target if one does not exist already.
                GameObject targetObj = new GameObject($"{gameObject.name}_Target");
                _target = targetObj.transform;
            }

            return _target;
        }
    }


    void Start()
    {
        pathfinder = FindObjectOfType<Pathfinder>();
        isAtDestination = true;
    }

    public IEnumerator UpdatePath()
    {
        while (!isAtDestination)
        {
            if (target != null)
            {
                path = pathfinder.FindPath(transform.position, target.position);
                if (path != null && path.Count > 0)
                {
                    currentWaypoint = 0;
                }
            }

            // New pathing will be updated every 1/2 seconds.
            yield return new WaitForSeconds(pathUpdateInterval);
        }
    }

    // Update function rewritten to accompany new steering mechanics.
    private void Update()
    {
        // Early out if path doesnt exist.
        if (path == null || currentWaypoint >= path.Count) return;

        Vector3 waypointTarget = path[currentWaypoint];

        // Call the compute steering force function.
        Vector3 steering = ComputeSteeringForce(waypointTarget);
        applyMovement(steering);

        // We need to check if the NPC is close the next waypoint.
        if (Vector3.Distance(transform.position, waypointTarget) < 0.5f)
        {
            currentWaypoint++;
            if (currentWaypoint >= path.Count)
            {
                isAtDestination = true;
            }
        }
    }

    private Vector3 ComputeSteeringForce(Vector3 waypointTarget)
    {
        // Compute the Arrival behavior.
        Vector3 desired = waypointTarget - transform.position;
        float distance = desired.magnitude;
        desired.Normalize();

        // Apply a slowing effect when approaching the final waypoint.
        float targetSpeed = maxSpeed;
        if (currentWaypoint == path.Count - 1 && distance < slowingRadius)
        {
            targetSpeed = maxSpeed * (distance / slowingRadius);
        }

        desired *= targetSpeed;

        // Base steering force, will be adjusted by a seperation force.
        Vector3 steeringForce = desired - currentVelocity;


        steeringForce += calculateSeperation() * 1.5f;


        // We need to clamp the steeringForce, otherwise it could exceed the maxForce causing undesired behavior.
        steeringForce = Vector3.ClampMagnitude(steeringForce, maxForce);

        return steeringForce;
    }

    private Vector3 calculateSeperation()
    {
        Vector3 seperation = Vector3.zero;
        int neighborCount = 0;

        Collider[] neighbors = Physics.OverlapSphere(transform.position, seperationRadius);

        foreach (Collider neighbor in neighbors)
        {
            if (neighbor.gameObject != gameObject)
            {
                Vector3 direction = transform.position - neighbor.transform.position;
                float distance = direction.magnitude;

                if (distance < seperationRadius)
                {
                    seperation += direction.normalized / distance;
                    neighborCount++;
                }
            }
        }

        if (neighborCount > 0)
        {
            seperation /= neighborCount;
            seperation.Normalize();
            seperation *= maxSpeed;
        }

        return seperation;
    }

    private void applyMovement(Vector3 steering)
    {
        // Update the currentVelocity with the steering force previously calculated.
        Vector3 smoothedSteering = Vector3.Lerp(previousSteering, steering, 0.5f);
        previousSteering = smoothedSteering;

        currentVelocity.y = 0;
        smoothedSteering.y = 0;

        currentVelocity += smoothedSteering * Time.deltaTime;
        currentVelocity = Vector3.ClampMagnitude(currentVelocity, maxSpeed);

        // Update the agents position based on the new velocity calculated.
        transform.position += currentVelocity * Time.deltaTime;
    }


    // TODO: Update the rotation of the group based on the commander of the group.
    private void updateRotation()
    {

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