using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaypointMover : MonoBehaviour
{
    // Stores a referece to the waypoint system this object will use
    [SerializeField] private Waypoints waypoints;

    [SerializeField] private float moveSpeed = 5f;

    [SerializeField] private float distanceThreshold = 0.1f;

    // The current waypoint target that the object is moving towards
    public Transform currentWaypoint;


    // Start is called before the first frame update
    void Start()
    {
        // Set inital postion to first waypoint
        currentWaypoint = waypoints.GetNextWaypoint(currentWaypoint);
        transform.position = currentWaypoint.position;

        // Set the next waypoint target
        currentWaypoint = waypoints.GetNextWaypoint(currentWaypoint);

    }

    // Update is called once per frame
    void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, currentWaypoint.position, moveSpeed * Time.deltaTime);
        if (Vector3.Distance(transform.position, currentWaypoint.position) < distanceThreshold)
        {
            currentWaypoint = waypoints.GetNextWaypoint(currentWaypoint);
        }
    }
}
