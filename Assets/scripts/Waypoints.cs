using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using UnityEngine;
using UnityEngine.UIElements;

public class Waypoints : MonoBehaviour
{
    [Range(0f, 2f)] // Range Slider for range of size
    [SerializeField] private float size = 1f; // Set size of waypoint sphere

    [Header("Path Settings")]
    [SerializeField] public bool canLoop = true; // Sets path to be looped or not

    [SerializeField] public bool isMovingForward = true; // Sets path to reverse after at last waypoint

    [SerializeField] public bool PathBranching = false; // Sets path to be phase 1 if false and if true runs aways phase 2 
    
    [SerializeField] private int waypointsActiveInPhase1 = 4; // Number of waypoints active in Phase 1


    
    private void Start()
    {
        UpdateWaypointVisibility();
    }
    
    // Simple method to update waypoint visibility
    private void UpdateWaypointVisibility()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(PathBranching || i < waypointsActiveInPhase1);
        }
    }

    // Makes changes visible in the editor
    private void OnValidate()
    {
        if (Application.isEditor && !Application.isPlaying)
            UpdateWaypointVisibility();
    }

    private void OnDrawGizmos()
    {
        foreach (Transform t in transform)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(t.position, size);
        }

        Gizmos.color = isMovingForward ? Color.green : Color.red;
        
        for (int i = 0; i < transform.childCount - 1; i++)
        {
            // Draws lines based on where they are in the Hierarchy top down.
            Gizmos.DrawLine(transform.GetChild(i).position, transform.GetChild(i + 1).position);
        }

        if (canLoop == true)
        {
            // Connects last line to first line to finish the loop
            Gizmos.DrawLine(transform.GetChild(transform.childCount - 1).position, transform.GetChild(0).position);
        }
    }

    public Transform GetNextWaypoint(Transform currentWaypoint)
    {
        if (currentWaypoint == null)
        {
            // Returns first waypoint
            return transform.GetChild(0);
        } 

        // Gets the index of the current waypoint
        int currentIndex = currentWaypoint.GetSiblingIndex();
        // Stores the index of the next waypoint to trabel towards
        int nextIndex = currentIndex;


        // Check for if moving forward on the path
        if (isMovingForward)
        {
            nextIndex += 1;

            // If the next waypoint index is equal to the count of the childdren/waypoints
            // then it is Already at the last waypoint
            // Check if the path is set to a loop return the first waypoint as the current waypoint
            // otherwise we subtract 1 from next index which return the same waypoint that the agent is currently at,
            // which will cause it to stop moving since it is already there

            if (nextIndex == transform.childCount)
            {
                if (canLoop)
                {
                    nextIndex = 0;
                }
                else
                {
                    nextIndex -= 1;
                }
            }
        }
        // moving backwards on the path
        else
        {
            nextIndex -= 1;

            // If the nextIndex is below 0 then it means that you are
            // already at the first waypoint, check if the path is set
            // to loop and if so return the last waypoint, otherwise we add 1 to the next Index
            // which will return the current waypoint that you already at which will cause the 
            // agent to stop since it is already there

            if (nextIndex < 0)
            {
                if(canLoop)
                {
                    nextIndex = transform.childCount - 1;
                }
                else 
                {
                    nextIndex += 1;
                }
            }
        }
        
        // Return the waypoint that has an index of nextIndex
        return transform.GetChild(nextIndex);
    }
}
