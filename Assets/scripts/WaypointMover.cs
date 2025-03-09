using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaypointMover : MonoBehaviour
{
    [SerializeField] public Waypoints waypoints;
    
    [Range(1f, 10f)]
    [SerializeField] private float moveSpeed = 2f;
    
    [SerializeField] private float distanceThreshold = 0.1f;
    
    [Range(1f, 20f)]
    [SerializeField] private float rotateSpeed = 10f;
    
    [Header("Path Selection")]
    [SerializeField] private bool useRandomPathSelection = false;
    
    // The current waypoint target that the object is moving towards
    public Transform currentWaypoint;
    
    // Reference to the game phase manager
    private PhaseManager phaseManager;
    
    // The rotation target for the current frame
    private Quaternion rotationGoal;
    // The direction to the next waypoint that the NPC needs to rotate towards
    private Vector3 directionToWaypoint;
    
    private void Start()
    {
        // Find the game phase manager
        phaseManager = FindObjectOfType<PhaseManager>();
        
        // Set initial position to first waypoint
        GamePhase currentPhase = GetCurrentGamePhase();
        currentWaypoint = waypoints.GetNextWaypoint(null, currentPhase);
        transform.position = currentWaypoint.position;
        
        // Set the next waypoint target
        currentWaypoint = GetNextWaypointBasedOnStrategy(currentWaypoint, currentPhase);
        transform.LookAt(currentWaypoint);
    }
    
    private void Update()
    {
        // Move towards the current waypoint
        transform.position = Vector3.MoveTowards(transform.position, currentWaypoint.position, moveSpeed * Time.deltaTime);
        
        // Check if we've reached the waypoint
        if (Vector3.Distance(transform.position, currentWaypoint.position) < distanceThreshold)
        {
            // Get the current game phase
            GamePhase currentPhase = GetCurrentGamePhase();
            
            // Get the next waypoint based on the current phase
            Transform nextWaypoint = GetNextWaypointBasedOnStrategy(currentWaypoint, currentPhase);
            
            // Only update if we got a different waypoint
            if (nextWaypoint != currentWaypoint)
            {
                currentWaypoint = nextWaypoint;
            }
        }
        
        // Always rotate towards the current waypoint
        RotateTowardsWaypoint();
    }
    
    private Transform GetNextWaypointBasedOnStrategy(Transform current, GamePhase phase)
    {
        if (useRandomPathSelection)
        {
            return waypoints.GetRandomNextWaypoint(current, phase);
        }
        else
        {
            return waypoints.GetNextWaypoint(current, phase);
        }
    }
    
    private GamePhase GetCurrentGamePhase()
    {
        if (phaseManager != null)
        {
            return phaseManager.GetCurrentPhase();
        }
        return GamePhase.Phase1; // Default to Phase1
    }
    
    private void RotateTowardsWaypoint()
    {
        // Gets direction to waypoint
        directionToWaypoint = (currentWaypoint.position - transform.position).normalized;
        
        // Check if there's a valid direction
        if (directionToWaypoint != Vector3.zero)
        {
            rotationGoal = Quaternion.LookRotation(directionToWaypoint);
            
            // Smooth rotation
            transform.rotation = Quaternion.Slerp(transform.rotation, rotationGoal, rotateSpeed * Time.deltaTime);
        }
    }
    
    // Add this utility method for debugging
    public void LogAvailablePaths()
    {
        if (waypoints != null && currentWaypoint != null)
        {
            GamePhase phase = GetCurrentGamePhase();
            Transform nextWaypoint = waypoints.GetNextWaypoint(currentWaypoint, phase);
            Debug.Log($"Current waypoint: {currentWaypoint.name}, Next waypoint: {nextWaypoint.name} for phase {phase}");
        }
    }
}