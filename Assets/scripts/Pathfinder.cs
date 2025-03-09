using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


// Custom priority queue implementation for A* pathfinding
public class PathPriorityQueue
{
    private readonly List<GridNode> nodes = new List<GridNode>();

    public int Count => nodes.Count;

    public void Enqueue(GridNode node)
    {
        nodes.Add(node);

        // Simple insertion sort to maintain queue order
        var i = nodes.Count - 1;
        while (i > 0)
        {
            var j = i - 1;
            if (CompareNodes(nodes[i], nodes[j]) < 0)
            {
                // Swap nodes
                var temp = nodes[i];
                nodes[i] = nodes[j];
                nodes[j] = temp;
                i = j;
            }
            else
            {
                break;
            }
        }
    }

    public GridNode Dequeue()
    {
        if (nodes.Count == 0)
            return null;

        var node = nodes[0];
        nodes.RemoveAt(0);
        return node;
    }

    public bool Contains(GridNode node)
    {
        return nodes.Contains(node);
    }

    public void Remove(GridNode node)
    {
        nodes.Remove(node);
    }

    private int CompareNodes(GridNode a, GridNode b)
    {
        // Compare by FCost first, then by HCost if equal
        var result = a.FCost.CompareTo(b.FCost);
        if (result == 0)
            result = a.HCost.CompareTo(b.HCost);
        return result;
    }
}


public class Pathfinder : MonoBehaviour
{
    private const int CACHE_MAX_SIZE = 50;

    // Path caching to stunt pathfinding lag across long distances.
    private readonly Dictionary<string, List<Vector3>> pathCache = new Dictionary<string, List<Vector3>>();

    private DynamicNavMesh navMesh;

    private void Start()
    {
        navMesh = GetComponent<DynamicNavMesh>();
    }

    // Helper method to create cache key
    private string GetPathKey(Vector3 start, Vector3 end)
    {
        // Round to reduce number of unique paths
        var startX = Mathf.RoundToInt(start.x);
        var startZ = Mathf.RoundToInt(start.z);
        var endX = Mathf.RoundToInt(end.x);
        var endZ = Mathf.RoundToInt(end.z);

        return $"{startX},{startZ}_{endX},{endZ}";
    }

    public List<Vector3> FindLongDistancePath(Vector3 startPos, Vector3 targetPos)
    {
        Debug.Log("initializing teleportation to: " + targetPos);
        // For long distances, create an extremely simple direct path
        var simplePath = new List<Vector3>();

        // Just use the start position
        simplePath.Add(startPos);

        // For very long paths, add at most ONE intermediate point
        var distance = Vector3.Distance(startPos, targetPos);
        if (distance > 100f)
        {
            // Add a single midpoint to help with navigation
            var midpoint = Vector3.Lerp(startPos, targetPos, 0.5f);
            simplePath.Add(midpoint);
        }

        // Add the target position
        simplePath.Add(targetPos);

        return simplePath;
    }

    public List<Vector3> FindPath(Vector3 startPos, Vector3 targetPos, AIMover npc)
    {
        var pathKey = GetPathKey(startPos, targetPos);
        // Check if the path is already contained in the cache.
        if (pathCache.ContainsKey(pathKey))
            // If so, return a copy of the path.
            return new List<Vector3>(pathCache[pathKey]);

        var startNode = navMesh.GetNodeFromWorldPoint(startPos);
        var targetNode = navMesh.GetNodeFromWorldPoint(targetPos);

        // Check if target is unreachable (in an unwalkable node)
        if (targetNode == null || !targetNode.IsWalkable)
        {
            // Debug.Log("Target position is unwalkable - path impossible");
            return null;
        }
        
        // Check if start position is valid
        if (startNode == null || !startNode.IsWalkable)
        {
            //Debug.LogError("Start position is unwalkable - path impossible from " + startPos + " or [" + startNode?.GridX + ", " + startNode?.GridY + "]"
            // + "for npc: " + npc.ToString());
            Vector3 temp = returnToSafety(GetBigNeighbors(startNode)).WorldPosition;
            return FindPath(temp, targetPos, npc);
        }
        
        // Check if target is the same as start (no need to pathfind)
        if (startNode == targetNode)
        {
            var directPath = new List<Vector3> { startPos, targetPos };
            return directPath;
        }
        
        // Initialize open/closed sets
        var openSet = new PathPriorityQueue();
        var closedSet = new HashSet<GridNode>();
        
        // Set maximum iterations to prevent infinite loops
        const int MAX_ITERATIONS = 50000;
        int iterations = 0;

        startNode.GCost = 0; // Reset start node's cost
        startNode.HCost = GetDistance(startNode, targetNode);
        openSet.Enqueue(startNode);

        var cameFrom = new Dictionary<GridNode, GridNode>();

        while (openSet.Count > 0)
        {
            iterations++;
            
            // Check if we've exceeded maximum iterations
            if (iterations > MAX_ITERATIONS)
            {
                Debug.LogWarning($"Pathfinding exceeded {MAX_ITERATIONS} iterations - terminating search (feel free to disable if causing issues)");
                return null; // Fallback to simple path
            }
            
            // Get node with lowest FCost
            var currentNode = openSet.Dequeue();
            closedSet.Add(currentNode);

            // Path found!
            if (currentNode == targetNode)
            {
                var path = RetracePath(startNode, targetNode, cameFrom);
                
                // Log iterations for performance monitoring

                // Debug.Log($"Path found in {iterations} iterations");

                // If path found, add to cache
                if (path != null && path.Count > 0)
                {
                    // Manage cache size
                    if (pathCache.Count >= CACHE_MAX_SIZE)
                    {
                        // Remove oldest entry
                        var firstKey = pathCache.Keys.First();
                        pathCache.Remove(firstKey);
                    }

                    // Store a copy
                    pathCache[pathKey] = new List<Vector3>(path);
                }

                return path;
            }

            // Explore neighbors
            foreach (var neighbor in GetNeighbors(currentNode))
            {
                if (!neighbor.IsWalkable || closedSet.Contains(neighbor))
                    continue;

                var wallPenalty = isNearWall(neighbor) ? 10 : 0;
                var tentativeGCost = currentNode.GCost + GetDistance(currentNode, neighbor) + wallPenalty;

                if (tentativeGCost < neighbor.GCost || !openSet.Contains(neighbor))
                {
                    cameFrom[neighbor] = currentNode;
                    neighbor.GCost = tentativeGCost;
                    neighbor.HCost = GetDistance(neighbor, targetNode);

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Enqueue(neighbor);
                    }
                    else
                    {
                        // Re-sort the node with updated costs
                        openSet.Remove(neighbor);
                        openSet.Enqueue(neighbor);
                    }
                }
            }
            
            // Check if open set is too large (another sign of a difficult/impossible path)
            if (openSet.Count > navMesh.GridSizeX * navMesh.GridSizeY / 2)
            {
                Debug.LogError("Open set too large - likely impossible path");
                return null; // Fallback to simple path
            }
        }

        Debug.LogWarning("No path found after exhaustive search for npc: " + npc.ToString());
        return null; // No path found
    }

    // Retrace the path from end to start
    private List<Vector3> RetracePath(GridNode startNode, GridNode endNode, Dictionary<GridNode, GridNode> cameFrom)
    {
        var path = new List<Vector3>();
        var currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode.WorldPosition);
            currentNode = cameFrom[currentNode];
        }

        path.Reverse();
        return path;
    }

    // Get neighboring nodes (8-directional)
    private List<GridNode> GetNeighbors(GridNode node)
    {
        var neighbors = new List<GridNode>();
        for (var x = -1; x <= 1; x++)
        for (var y = -1; y <= 1; y++)
        {
            if (x == 0 && y == 0) continue; // Skip self

            var checkX = node.GridX + x;
            var checkY = node.GridY + y;

            if (checkX >= 0 && checkX < navMesh.GridSizeX &&
                checkY >= 0 && checkY < navMesh.GridSizeY)
                neighbors.Add(navMesh.Grid[checkX, checkY]);
        }

        return neighbors;
    }

    private List<GridNode> GetBigNeighbors(GridNode node)
    {
        var neighbors = new List<GridNode>();
        for (var x = -3; x <= 3; x++)
        for (var y = -3; y <= 3; y++)
        {
            if (x == 0 && y == 0) continue; // Skip self

            var checkX = node.GridX + x;
            var checkY = node.GridY + y;

            if (checkX >= 0 && checkX < navMesh.GridSizeX &&
                checkY >= 0 && checkY < navMesh.GridSizeY)
                neighbors.Add(navMesh.Grid[checkX, checkY]);
        }

        return neighbors;
    }

    private GridNode returnToSafety(List<GridNode> neighbors){
        // Debug.LogWarning("Returning to safety");
        foreach(GridNode node in neighbors){
            if(node.IsWalkable) {
                Debug.LogWarning("returning to valid node: " + node.WorldPosition + "or [" + node.GridX + ", " + node.GridY + "]");
                return node;
            }
        }
        Debug.LogError("Couldnt find valid neighbor");
        return null;
    }

    private bool isNearWall(GridNode node)
    {
        // Debug.Log("near a wall! - ");
        for (var dx = -1; dx <= 1; dx++)
        for (var dy = -1; dy <= 1; dy++)
        {
            if (dx == 0 && dy == 0) continue;

            var checkX = node.GridX + dx;
            var checkY = node.GridY + dy;

            if (checkX >= 0 && checkX < navMesh.GridSizeX && checkY >= 0 && checkY < navMesh.GridSizeY)
                if (!navMesh.Grid[checkX, checkY].IsWalkable)
                    return true;
        }

        return false;
    }

    // Calculate Manhattan distance (for grid-based movement)
    private int GetDistance(GridNode a, GridNode b)
    {
        var dstX = Mathf.Abs(a.GridX - b.GridX);
        var dstY = Mathf.Abs(a.GridY - b.GridY);
        return dstX + dstY;
    }
}