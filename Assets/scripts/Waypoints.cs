using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Waypoints : MonoBehaviour
{
    [Range(0f, 2f)] // Range Slider for range of size
    [SerializeField] private float size = 1f; // Set size of waypoint sphere
    private void OnDrawGizmos()
    {
        foreach (Transform t in transform)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(t.position, size);
        }

        Gizmos.color = Color.red;
        for (int i = 0; i < transform.childCount - 1; i++)
        {
            // Draws lines based on where they are in the Hierarchy top down.
            Gizmos.DrawLine(transform.GetChild(i).position, transform.GetChild(i + 1).position);
        }
        // Connects last line to first line to finish the loop
        Gizmos.DrawLine(transform.GetChild(transform.childCount - 1).position, transform.GetChild(0).position);
    }

    public Transform GetNextWaypoint(Transform currentWaypoint)
    {
        if (currentWaypoint == null)
        {
            // Returns first waypoint
            return transform.GetChild(0);
        } 
        // All waypoints in between
        else if (currentWaypoint.GetSiblingIndex() < transform.childCount - 1)
        {
            return transform.GetChild(currentWaypoint.GetSiblingIndex() + 1);
        }
        else
        {
            // Returns first waypoint
            return transform.GetChild(0);
        }
    }
}
