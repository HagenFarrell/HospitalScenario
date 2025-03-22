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

    [SerializeField] public int waypointsActiveInPhase; // Number of waypoints active in Phase 1
    public int ActiveChildLength;
    PhaseManager phasemanager;


    
    private void Start()
    {
        phasemanager = FindObjectOfType<PhaseManager>();
        ActiveChildLength = waypointsActiveInPhase;
        UpdateWaypointVisibility();
    }
    
    // Simple method to update waypoint visibility
    private void UpdateWaypointVisibility()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(i < ActiveChildLength);
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
        ActiveChildLength = 0;

        foreach (Transform t in transform)
        {
            if (t.gameObject.activeSelf)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(t.position, size);
            }
        }

        Gizmos.color = isMovingForward ? Color.green : Color.red;

        for (int i = 0; i < transform.childCount - 1; i++)
        {
            if (transform.GetChild(i + 1).gameObject.activeSelf)
            {
            // Draws lines based on where they are in the Hierarchy top down.
            Gizmos.DrawLine(transform.GetChild(i).position, transform.GetChild(i + 1).position);
            ActiveChildLength++;
            }
        }

        if (canLoop == true)
        {
            // Connects last line to first line to finish the loop
            Gizmos.DrawLine(transform.GetChild(ActiveChildLength).position, transform.GetChild(0).position);
        }
    }

    public Transform GetNextWaypoint(Transform currentWaypoint)
    {
        if (currentWaypoint == null || ActiveChildLength == 1)
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


            // Do not go to next node if disabled
            if (nextIndex < transform.childCount && !transform.GetChild(nextIndex).gameObject.activeSelf)
            {
                if(!canLoop && phasemanager != null) { 
                    if(phasemanager.GetCurrentPhase() == GamePhase.Phase1){
                        // Debug.Log("at last node, moving fro");
                        isMovingForward = !isMovingForward;
                        return transform.GetChild(nextIndex-1);
                    }
                    return currentWaypoint;
                }
            }

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

            // Do not go to next node if disabled
            if (nextIndex == 0 && !transform.GetChild(nextIndex).gameObject.activeSelf)
            {
                if(!canLoop) { 
                    return currentWaypoint;
                }
            }

            // If the nextIndex is below 0 then it means that you are
            // already at the first waypoint, check if the path is set
            // to loop and if so return the last waypoint, otherwise we add 1 to the next Index
            // which will return the current waypoint that you already at which will cause the 
            // agent to stop since it is already there

            if (nextIndex < 0)
            {
                if(canLoop)
                {
                    nextIndex = ActiveChildLength;
                }
                else 
                {
                    if(phasemanager.GetCurrentPhase() == GamePhase.Phase1){
                        Debug.Log("back to uhh first node, moving fro");
                        isMovingForward = !isMovingForward;
                        return transform.GetChild(currentIndex+1);
                    }
                    nextIndex += 1;
                }
            }
        }
        
        // Return the waypoint that has an index of nextIndex
        return transform.GetChild(nextIndex);
    }

    public void ResetToPhaseSettings()
    {
        ActiveChildLength = waypointsActiveInPhase; // Reset to phase-specific value
        UpdateWaypointVisibility();
    }

    public void enableAll(){
        // Debug.Log("enabling all");
        foreach(Transform t in transform){
            t.gameObject.SetActive(true);
            ActiveChildLength++;
        }
    }
}
