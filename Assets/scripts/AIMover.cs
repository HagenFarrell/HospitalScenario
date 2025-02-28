using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIMover : MonoBehaviour
{
    private Animator animator;

    // ---- Variables for the BOIDS algorithm. ----
    [SerializeField] private float speed = 13f;
    [SerializeField] private float maxSpeed = 15f;
    [SerializeField] private float maxForce = 15f;
    [SerializeField] private float slowingRadius = 1f;
    [SerializeField] private float seperationRadius = 1f;
    [SerializeField] private float lookAheadDistance = 1f;


    // ---- Movement based variables ----
    [SerializeField] private float movementThreshhold = 0.1f;
    [SerializeField] private float animationDampTime = 0.1f;

    // Animation parameter hashes for efficiency
    private readonly int walkingHash = Animator.StringToHash("IsWalking");
    private readonly int speedHash = Animator.StringToHash("Speed");


    // ---- Pathingfinding variables ----
    [SerializeField] private float pathWeight = 0.4f;
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

    // Lazy loading for target transforms. Only to be used when needed. Otherwise is not loaded.
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
        if (pathfinder == null)
        {
            Debug.LogError($"No Pathfinder found for {gameObject.name}!");
        }

        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError($"No Animator found for {gameObject.name}!");
        }

        isAtDestination = true;
    }

    public IEnumerator UpdatePath()
    {
        while (!isAtDestination)
        {
            if (target != null)
            {
                //Debug.Log($"Finding path from {transform.position} to {target.position}");
                path = pathfinder.FindPath(transform.position, target.position);
                if (path != null && path.Count > 0)
                {
                    //Debug.Log($"Path found with {path.Count} waypoints");
                    currentWaypoint = 0;
                }
            }
            yield return new WaitForSeconds(pathUpdateInterval);
        }
    }

    // Update function rewritten to accompany new steering mechanics.
    private void Update()
    {
        // Ensuring idling if path is out.
        if (path == null || currentWaypoint >= path.Count)
        {
            currentVelocity = Vector3.zero;
            previousSteering = Vector3.zero;
            UpdateAnimation();
            return;
        }

        Vector3 waypointTarget = path[currentWaypoint];

        // Call the compute steering force function.
        Vector3 steering = ComputeSteeringForce(waypointTarget);
        Debug.Log($"{gameObject.name} steering force: {steering.magnitude}");
        applyMovement(steering);

        float distToWaypoint = Vector3.Distance(transform.position, waypointTarget);
        Debug.Log($"Distance to waypoint: {distToWaypoint}");


        // We need to check if the NPC is close the next waypoint.
        if (distToWaypoint < 0.5f)
        {
            Debug.Log($"Reached waypoint {currentWaypoint}, moving to next");
            currentWaypoint++;
            if (currentWaypoint >= path.Count)
            {
                Debug.Log("Reached final waypoint");
                isAtDestination = true;
            }
        }
    }

    private Vector3 ComputeSteeringForce(Vector3 waypointTarget)
    {
        // Primary movement vector this drives our main movement
        Vector3 targetDirection = (waypointTarget - transform.position);
        float distanceToTarget = targetDirection.magnitude;


        // Use full speed for the desired velocity to ensure snappy movement
        Vector3 desiredVelocity = targetDirection.normalized * maxSpeed;

        // Predict position for path following, but keep it very short range
        Vector3 futurePosition = transform.position + (currentVelocity * 0.5f); // Reduced look ahead
        Vector3 nearestPathPoint = FindNearestPointOnPath(futurePosition);
        Vector3 pathOffset = nearestPathPoint - transform.position;

        // Calculate path influence - but keep it minimal
        float pathInfluence = Mathf.Clamp01(pathOffset.magnitude / 5f) * 0.2f;
        Vector3 pathCorrection = pathOffset.normalized * maxSpeed;

        // Heavily favor direct movement
        Vector3 blendedDesiredVelocity = Vector3.Lerp(
            desiredVelocity,
            pathCorrection,
            pathInfluence
        );

        // Only slow down when very close to target
        if (distanceToTarget < slowingRadius)
        {
            float speedMultiplier = Mathf.Clamp01(distanceToTarget / slowingRadius);
            blendedDesiredVelocity *= speedMultiplier;
        }

        // More aggressive steering calculation
        Vector3 steeringForce = (blendedDesiredVelocity - currentVelocity) * 2f; // Multiplier for stronger steering

        // Minimal separation only when absolutely necessary
        steeringForce += calculateSeperation() * 0.2f;

        // Allow stronger forces for quicker acceleration
        return Vector3.ClampMagnitude(steeringForce, maxForce * 2f);
    }

    private Vector3 FindNearestPointOnPath(Vector3 position)
    {
        if (path == null || path.Count == 0) return position;

        float minDistance = float.MaxValue;
        Vector3 nearestPoint = position;

        for (int i = currentWaypoint; i < path.Count - 1; ++i)
        {
            Vector3 start = path[i];
            Vector3 end = path[i + 1];
            Vector3 point = GetNearestPointOnSegment(position, start, end);

            float distance = Vector3.Distance(position, point);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestPoint = point;
            }
        }
        return nearestPoint;
    }


    private Vector3 GetNearestPointOnSegment(Vector3 point, Vector3 start, Vector3 end)
    {
        Vector3 segment = end - start;
        Vector3 VectorToPoint = point - start;

        float segmentLength = segment.magnitude;
        Vector3 segmentDirection = segment / segmentLength;

        float projection = Vector3.Dot(VectorToPoint, segmentDirection);
        projection = Mathf.Clamp(projection, 0f, segmentLength);

        return start + (segmentDirection * projection);
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

        // Animate on move.
        UpdateAnimation();

        // Add smooth rotations.
        updateRotation();
    }

    // Updates the animations of units depending on movement speed.
    private void UpdateAnimation()
    {
        if (animator != null)
        {
            float currentSpeed = currentVelocity.magnitude;
            animator.SetBool(walkingHash, currentSpeed > movementThreshhold);


            // We need to consider multiple conditions for a unit to be "stopped":
            bool shouldBeIdle =
                // Check if we've reached our destination
                isAtDestination ||
                // Check if velocity is effectively zero
                currentSpeed < 0.01f ||
                // Check if we don't have a valid path
                path == null ||
                // Check if we've reached the end of our path
                currentWaypoint >= path.Count;

            // Set the walking animation state based on our idle check
            animator.SetBool(walkingHash, !shouldBeIdle);

            // If we are going to idle, make sure to zero out the NPC velocity.
            if (shouldBeIdle)
            {
                currentVelocity = Vector3.zero;
                previousSteering = Vector3.zero;
            }
        }
    }


    // Uses quaternions (to avoid gimble lock) for smooth rotations based on hit point clicked. (target location)
    private void updateRotation()
    {
        if (currentVelocity.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(currentVelocity.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 0.5f);
        }
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