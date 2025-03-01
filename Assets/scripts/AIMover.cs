using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIMover : MonoBehaviour
{
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


    // ---- Pathingfinding variables ----
    [SerializeField] private float pathWeight = 0.4f;
    [SerializeField] private float pathUpdateInterval = 0.5f;
    private readonly int speedHash = Animator.StringToHash("Speed");

    // Animation parameter hashes for efficiency
    private readonly int walkingHash = Animator.StringToHash("IsWalking");

    // Lazy loading for target transforms. Only to be used when needed. Otherwise is not loaded.
    private Transform _target;
    private Animator animator;

    // Maintain a reference globally about the current speed of the AI.
    private Vector3 currentVelocity;
    private int currentWaypoint;
    private List<Vector3> path;
    private Pathfinder pathfinder;


    private Vector3 previousSteering = Vector3.zero;

    /* For anyone reading this that doesnt understand.
     * get: means other classes can read the value
     * Private set: means only the class containing this property can modify the value.
     * (So this class we are currently in. :D )
     */
    public bool isAtDestination { get; private set; }

    public Transform target
    {
        get
        {
            if (_target == null)
            {
                // Create a new GameObject for the target if one does not exist already.
                var targetObj = new GameObject($"{gameObject.name}_Target");
                _target = targetObj.transform;
            }

            return _target;
        }
    }


    private void Start()
    {
        pathfinder = FindObjectOfType<Pathfinder>();
        if (pathfinder == null) Debug.LogError($"No Pathfinder found for {gameObject.name}!");

        animator = GetComponent<Animator>();
        if (animator == null) Debug.LogError($"No Animator found for {gameObject.name}!");

        isAtDestination = true;
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

        var waypointTarget = path[currentWaypoint];

        // Call the compute steering force function.
        var steering = ComputeSteeringForce(waypointTarget);
        //Debug.Log($"{gameObject.name} steering force: {steering.magnitude}");
        applyMovement(steering);

        var distToWaypoint = Vector3.Distance(transform.position, waypointTarget);
        //Debug.Log($"Distance to waypoint: {distToWaypoint}");


        // We need to check if the NPC is close the next waypoint.
        if (distToWaypoint < 0.5f)
        {
            //Debug.Log($"Reached waypoint {currentWaypoint}, moving to next");
            currentWaypoint++;
            if (currentWaypoint >= path.Count)
                //Debug.Log("Reached final waypoint");
                isAtDestination = true;
        }
    }

    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
        if (path != null)
        {
            Gizmos.color = Color.blue;
            foreach (var point in path) Gizmos.DrawSphere(point, 0.2f);
        }
    }
#endif

    public IEnumerator UpdatePath()
    {
        while (!isAtDestination)
            if (target != null)
            {
                // Add random offset to update times to prevent synchronized updates
                var jitter = Random.Range(0f, 0.5f);

                // Calculate distance-based interval
                var distance = Vector3.Distance(transform.position, target.position);
                var updateInterval = pathUpdateInterval;

                // Less frequent updates for longer distances
                if (distance > 100f) updateInterval *= 4f;
                else if (distance > 50f) updateInterval *= 2f;

                // Queue path request instead of calculating directly
                PathRequestManager.RequestPath(transform.position, target.position, OnPathFound);

                // Wait longer before requesting another path
                yield return new WaitForSeconds(updateInterval + jitter);
            }
            else
            {
                yield return new WaitForSeconds(pathUpdateInterval);
            }
    }

    // Callback when path is ready
    private void OnPathFound(List<Vector3> newPath)
    {
        if (newPath != null && newPath.Count > 0)
        {
            path = newPath;
            currentWaypoint = 0;
        }
    }

    private Vector3 ComputeSteeringForce(Vector3 waypointTarget)
    {
        // Primary movement vector this drives our main movement
        var targetDirection = waypointTarget - transform.position;
        var distanceToTarget = targetDirection.magnitude;


        // Use full speed for the desired velocity to ensure snappy movement
        var desiredVelocity = targetDirection.normalized * maxSpeed;

        // Predict position for path following, but keep it very short range
        var futurePosition = transform.position + currentVelocity * 0.5f; // Reduced look ahead
        var nearestPathPoint = FindNearestPointOnPath(futurePosition);
        var pathOffset = nearestPathPoint - transform.position;

        // Calculate path influence - but keep it minimal
        var pathInfluence = Mathf.Clamp01(pathOffset.magnitude / 5f) * 0.2f;
        var pathCorrection = pathOffset.normalized * maxSpeed;

        // Heavily favor direct movement
        var blendedDesiredVelocity = Vector3.Lerp(
            desiredVelocity,
            pathCorrection,
            pathInfluence
        );

        // Only slow down when very close to target
        if (distanceToTarget < slowingRadius)
        {
            var speedMultiplier = Mathf.Clamp01(distanceToTarget / slowingRadius);
            blendedDesiredVelocity *= speedMultiplier;
        }

        // More aggressive steering calculation
        var steeringForce = (blendedDesiredVelocity - currentVelocity) * 2f; // Multiplier for stronger steering

        // Minimal separation only when absolutely necessary
        steeringForce += calculateSeperation() * 0.2f;

        // Allow stronger forces for quicker acceleration
        return Vector3.ClampMagnitude(steeringForce, maxForce * 2f);
    }

    private Vector3 FindNearestPointOnPath(Vector3 position)
    {
        if (path == null || path.Count == 0) return position;

        var minDistance = float.MaxValue;
        var nearestPoint = position;

        for (var i = currentWaypoint; i < path.Count - 1; ++i)
        {
            var start = path[i];
            var end = path[i + 1];
            var point = GetNearestPointOnSegment(position, start, end);

            var distance = Vector3.Distance(position, point);
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
        var segment = end - start;
        var VectorToPoint = point - start;

        var segmentLength = segment.magnitude;
        var segmentDirection = segment / segmentLength;

        var projection = Vector3.Dot(VectorToPoint, segmentDirection);
        projection = Mathf.Clamp(projection, 0f, segmentLength);

        return start + segmentDirection * projection;
    }


    private Vector3 calculateSeperation()
    {
        var seperation = Vector3.zero;
        var neighborCount = 0;

        var neighbors = Physics.OverlapSphere(transform.position, seperationRadius);

        foreach (var neighbor in neighbors)
            if (neighbor.gameObject != gameObject)
            {
                var direction = transform.position - neighbor.transform.position;
                var distance = direction.magnitude;

                if (distance < seperationRadius)
                {
                    seperation += direction.normalized / distance;
                    neighborCount++;
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
        var smoothedSteering = Vector3.Lerp(previousSteering, steering, 0.5f);
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
            var currentSpeed = currentVelocity.magnitude;
            animator.SetBool(walkingHash, currentSpeed > movementThreshhold);


            // We need to consider multiple conditions for a unit to be "stopped":
            var shouldBeIdle =
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
            var targetRotation = Quaternion.LookRotation(currentVelocity.normalized);
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
}