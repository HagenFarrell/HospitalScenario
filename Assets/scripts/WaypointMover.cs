using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public class WaypointMover : MonoBehaviour
{
    // Stores a referece to the waypoint system this object will use
    [SerializeField] public Waypoints waypoints;

    [Range(1f, 10f)]
    [SerializeField] private float moveSpeed = 2f;

    [SerializeField] private float distanceThreshold = 0.1f;


    [Range(1f, 20f)]
    [SerializeField] private float rotateSpeed = 10f;

    // The current waypoint target that the object is moving towards
    public Transform currentWaypoint;

    // The roation target for the current frame
    private Quaternion rotationGoal;
    // The direction to the next waypoint that the NPC needs to rotate towards
    private Vector3 directionToWaypoint;
    // Check if civilian/medical reach final waypoint so they can be despawned
    public bool despawnAtLastWaypoint = false;

    // Start is called before the first frame update
    void Start()
    {
        // Set inital postion to first waypoint
        currentWaypoint = waypoints.GetNextWaypoint(currentWaypoint);
        transform.position = currentWaypoint.position;

        // Set the next waypoint target
        currentWaypoint = waypoints.GetNextWaypoint(currentWaypoint);
        transform.LookAt(currentWaypoint);

    }

    // Update is called once per frame
    void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, currentWaypoint.position, moveSpeed * Time.deltaTime);
        if (Vector3.Distance(transform.position, currentWaypoint.position) < distanceThreshold)
        {
            // Get current phase
            PhaseManager phaseManager = FindObjectOfType<PhaseManager>();
            
            // Check if this is the last waypoint
            bool isLastWaypoint = currentWaypoint.GetSiblingIndex() == currentWaypoint.parent.childCount - 1;

            // If it's the last waypoint, we should despawn, we're not looping, and we're in Phase 2
            if (isLastWaypoint && despawnAtLastWaypoint && !waypoints.canLoop && 
                phaseManager != null && phaseManager.GetCurrentPhase() == GamePhase.Phase2) {
                // Despawn the NPC
                Debug.Log($"Phase 2: NPC {gameObject.name} reached final waypoint and is despawning");
                gameObject.SetActive(false);
                return;
            } else if(!despawnAtLastWaypoint){
                gameObject.SetActive(true);
            }

            // Otherwise, continue to the next waypoint
            currentWaypoint = waypoints.GetNextWaypoint(currentWaypoint);
        }
        if (waypoints.canLoop)
        {
            RotateTowardsWaypoint();
        }
        else
        {
            // If path is not looping, only rotate if not at the last waypoint
            if (currentWaypoint.GetSiblingIndex() < waypoints.transform.childCount)
            {
                // Not at the last waypoint, so continue rotating
                RotateTowardsWaypoint();
            }
            // If at the last waypoint, do nothing (don't rotate)
        }
    }

    // Will Slowly rotate the agent towards the current waypoint it is moving towards
    private void RotateTowardsWaypoint()
    {
        // Gets direction to waypoint
        directionToWaypoint = (currentWaypoint.position - transform.position).normalized;
        
        // Check to stop last waypoint rotation if Canloop = false
        if (directionToWaypoint != Vector3.zero)
        {
            rotationGoal = Quaternion.LookRotation(directionToWaypoint);

            // Slow rotation
            transform.rotation = Quaternion.Slerp(transform.rotation, rotationGoal, rotateSpeed * Time.deltaTime);
        }
    }
}
