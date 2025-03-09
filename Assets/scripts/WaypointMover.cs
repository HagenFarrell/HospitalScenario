using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public class WaypointMover : MonoBehaviour
{
    // Stores a referece to the waypoint system this object will use
    [SerializeField] private Waypoints waypoints;

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
            currentWaypoint = waypoints.GetNextWaypoint(currentWaypoint);
        }
        RotateTowardsWaypoint();
    }

    // Will Slowly rotate the agent towards the current waypoint it is moving towards
    private void RotateTowardsWaypoint()
    {
        // Gets direction to waypoint
        directionToWaypoint = (currentWaypoint.position - transform.position).normalized;
        rotationGoal = Quaternion.LookRotation(directionToWaypoint);

        // Slow rotation
        transform.rotation = Quaternion.Slerp(transform.rotation, rotationGoal, rotateSpeed * Time.deltaTime);
    }
}
