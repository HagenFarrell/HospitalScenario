using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Define a waypoint with branching capabilities
[System.Serializable]
public class WaypointNode
{
    public Transform waypoint;
    public List<WaypointConnection> connections = new List<WaypointConnection>();
    
    // Optional: Add a name/identifier for editor convenience
    public string nodeName;
}

// Define a connection between waypoints with conditions
[System.Serializable]
public class WaypointConnection
{
    public Transform targetWaypoint;
    public GamePhase requiredPhase = GamePhase.Phase1; // Default to Phase1
    public bool anyPhase = true; // If true, this connection is valid for any phase
    public float probability = 1f; // For random path selection (1.0 = 100%)
    
    // Optional: Add a description for editor convenience
    public string connectionName;
}

public class Waypoints : MonoBehaviour
{
    [Range(0f, 2f)]
    [SerializeField] private float waypointSize = 1f;
    
    [Header("Path Settings")]
    [SerializeField] public bool visualizeAllPaths = true;
    
    [Header("Waypoint Configuration")]
    [SerializeField] private List<WaypointNode> waypointNodes = new List<WaypointNode>();
    
    // Cache for faster lookup
    private Dictionary<Transform, WaypointNode> nodeMap = new Dictionary<Transform, WaypointNode>();
    
    private void OnValidate()
    {
        // Auto-populate waypoints if not already set up
        if (waypointNodes.Count == 0 || NeedsRebuild())
        {
            RebuildWaypointNodes();
        }
    }
    
    private void Awake()
    {
        // Build the lookup dictionary
        BuildNodeMap();
    }
    
    private bool NeedsRebuild()
    {
        // Check if the child transforms match the waypoint nodes
        if (transform.childCount != waypointNodes.Count)
            return true;
            
        for (int i = 0; i < waypointNodes.Count; i++)
        {
            if (waypointNodes[i].waypoint != transform.GetChild(i))
                return true;
        }
        
        return false;
    }
    
    private void RebuildWaypointNodes()
    {
        waypointNodes.Clear();
        
        // Create a node for each child transform
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform waypointTransform = transform.GetChild(i);
            WaypointNode node = new WaypointNode
            {
                waypoint = waypointTransform,
                nodeName = waypointTransform.name
            };
            
            // If this isn't the last waypoint, add a default connection to the next waypoint
            if (i < transform.childCount - 1)
            {
                WaypointConnection defaultConnection = new WaypointConnection
                {
                    targetWaypoint = transform.GetChild(i + 1),
                    anyPhase = true, // Default connection works for any phase
                    connectionName = "Default to " + transform.GetChild(i + 1).name
                };
                node.connections.Add(defaultConnection);
            }
            
            waypointNodes.Add(node);
        }
    }
    
    private void BuildNodeMap()
    {
        nodeMap.Clear();
        foreach (WaypointNode node in waypointNodes)
        {
            if (node.waypoint != null)
            {
                nodeMap[node.waypoint] = node;
            }
        }
    }
    
    public Transform GetNextWaypoint(Transform currentWaypoint, GamePhase currentPhase)
    {
        // If no current waypoint, return the first one
        if (currentWaypoint == null && waypointNodes.Count > 0)
        {
            return waypointNodes[0].waypoint;
        }
        
        // If the current waypoint is in our node map
        if (nodeMap.ContainsKey(currentWaypoint))
        {
            WaypointNode currentNode = nodeMap[currentWaypoint];
            
            // Filter connections by phase if specified
            List<WaypointConnection> validConnections = new List<WaypointConnection>();
            
            foreach (WaypointConnection connection in currentNode.connections)
            {
                // Connection is valid if target exists and either anyPhase is true or phase matches
                if (connection.targetWaypoint != null && 
                    (connection.anyPhase || connection.requiredPhase == currentPhase))
                {
                    validConnections.Add(connection);
                }
            }
            
            // If we have valid connections
            if (validConnections.Count > 0)
            {
                // For now, just return the first valid connection
                // You could implement probability-based selection here if needed
                return validConnections[0].targetWaypoint;
            }
        }
        
        // If no connections or if waypoint isn't in the map, stay at current waypoint
        return currentWaypoint;
    }
    
    // Implement probability-based selection
    public Transform GetRandomNextWaypoint(Transform currentWaypoint, GamePhase currentPhase)
    {
        if (currentWaypoint == null && waypointNodes.Count > 0)
        {
            return waypointNodes[0].waypoint;
        }
        
        if (nodeMap.ContainsKey(currentWaypoint))
        {
            WaypointNode currentNode = nodeMap[currentWaypoint];
            List<WaypointConnection> validConnections = new List<WaypointConnection>();
            
            foreach (WaypointConnection connection in currentNode.connections)
            {
                if (connection.targetWaypoint != null && 
                    (connection.anyPhase || connection.requiredPhase == currentPhase))
                {
                    validConnections.Add(connection);
                }
            }
            
            if (validConnections.Count > 0)
            {
                // Calculate total probability
                float totalProbability = 0f;
                foreach (WaypointConnection connection in validConnections)
                {
                    totalProbability += connection.probability;
                }
                
                // Select a random point along the probability spectrum
                float randomPoint = Random.Range(0, totalProbability);
                float runningTotal = 0f;
                
                // Find which connection was selected
                foreach (WaypointConnection connection in validConnections)
                {
                    runningTotal += connection.probability;
                    if (randomPoint <= runningTotal)
                    {
                        return connection.targetWaypoint;
                    }
                }
                
                // Fallback
                return validConnections[0].targetWaypoint;
            }
        }
        
        return currentWaypoint;
    }
    
    private void OnDrawGizmos()
    {
        if (waypointNodes.Count == 0 && Application.isEditor && !Application.isPlaying)
        {
            // Auto-generate for gizmo drawing if needed
            RebuildWaypointNodes();
            BuildNodeMap();
        }
        
        // Draw waypoint spheres
        foreach (WaypointNode node in waypointNodes)
        {
            if (node.waypoint != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(node.waypoint.position, waypointSize);
            }
        }
        
        // Draw connection lines
        foreach (WaypointNode node in waypointNodes)
        {
            if (node.waypoint != null)
            {
                foreach (WaypointConnection connection in node.connections)
                {
                    if (connection.targetWaypoint != null)
                    {
                        // Use different colors for different phases
                        if (connection.anyPhase)
                        {
                            Gizmos.color = Color.red; // Default connection for any phase
                        }
                        else
                        {
                            // Use different colors for different phases
                            // This creates unique colors based on the phase enum value
                            float hue = ((int)connection.requiredPhase * 0.15f) % 1.0f;
                            Gizmos.color = Color.HSVToRGB(hue, 0.7f, 0.9f);
                        }
                        
                        Gizmos.DrawLine(node.waypoint.position, connection.targetWaypoint.position);
                        
                        // Draw a small arrow to indicate direction
                        Vector3 direction = (connection.targetWaypoint.position - node.waypoint.position).normalized;
                        Vector3 arrowPos = Vector3.Lerp(node.waypoint.position, connection.targetWaypoint.position, 0.7f);
                        float arrowSize = waypointSize * 0.5f;
                        Vector3 right = Quaternion.Euler(0, 30, 0) * -direction * arrowSize;
                        Vector3 left = Quaternion.Euler(0, -30, 0) * -direction * arrowSize;
                        
                        Gizmos.DrawLine(arrowPos, arrowPos + right);
                        Gizmos.DrawLine(arrowPos, arrowPos + left);
                    }
                }
            }
        }
    }
    
    // Editor utility to add a connection between waypoints
    public void AddConnection(Transform source, Transform target, GamePhase phaseRequirement, bool anyPhase, float connectionProbability = 1f)
    {
        if (source == null || target == null)
            return;
            
        // Update maps if needed
        if (nodeMap.Count != waypointNodes.Count)
            BuildNodeMap();
            
        if (nodeMap.ContainsKey(source))
        {
            WaypointNode sourceNode = nodeMap[source];
            
            // Check if connection already exists
            bool exists = false;
            foreach (WaypointConnection conn in sourceNode.connections)
            {
                if (conn.targetWaypoint == target && conn.anyPhase == anyPhase && 
                    (!anyPhase && conn.requiredPhase == phaseRequirement))
                {
                    exists = true;
                    break;
                }
            }
            
            if (!exists)
            {
                WaypointConnection newConnection = new WaypointConnection
                {
                    targetWaypoint = target,
                    requiredPhase = phaseRequirement,
                    anyPhase = anyPhase,
                    probability = connectionProbability,
                    connectionName = "To " + target.name + (anyPhase ? " (Any Phase)" : " (Phase " + phaseRequirement.ToString() + ")")
                };
                
                sourceNode.connections.Add(newConnection);
            }
        }
    }
}