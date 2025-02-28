using UnityEngine;
using System.Collections.Generic;
using System.Linq;


// Custom priority queue implementation for A* pathfinding
public class PathPriorityQueue
{
    private List<GridNode> nodes = new List<GridNode>();
    
    public int Count => nodes.Count;
    
    public void Enqueue(GridNode node)
    {
        nodes.Add(node);
        
        // Simple insertion sort to maintain queue order
        int i = nodes.Count - 1;
        while (i > 0)
        {
            int j = i - 1;
            if (CompareNodes(nodes[i], nodes[j]) < 0)
            {
                // Swap nodes
                GridNode temp = nodes[i];
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
            
        GridNode node = nodes[0];
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
        int result = a.FCost.CompareTo(b.FCost);
        if (result == 0)
            result = a.HCost.CompareTo(b.HCost);
        return result;
    }
}


public class Pathfinder : MonoBehaviour
{
    // Path caching to stunt pathfinding lag across long distances.
    private Dictionary<string, List<Vector3>> pathCache = new Dictionary<string, List<Vector3>>();
    private const int CACHE_MAX_SIZE = 50;

    // Helper method to create cache key
    private string GetPathKey(Vector3 start, Vector3 end)
    {
        // Round to reduce number of unique paths
        int startX = Mathf.RoundToInt(start.x);
        int startZ = Mathf.RoundToInt(start.z);
        int endX = Mathf.RoundToInt(end.x);
        int endZ = Mathf.RoundToInt(end.z);
    
        return $"{startX},{startZ}_{endX},{endZ}";
    }
    
    private DynamicNavMesh navMesh;

    void Start()
    {
        navMesh = GetComponent<DynamicNavMesh>();
    }

    public List<Vector3> FindPath(Vector3 startPos, Vector3 targetPos)
    {
        
        string pathKey = GetPathKey(startPos, targetPos);
        // Check if the path is already contained in the cache.
        if (pathCache.ContainsKey(pathKey))
        {
            // If so, return a copy of the path.
            return new List<Vector3>(pathCache[pathKey]);
        }
        
        
        GridNode startNode = navMesh.GetNodeFromWorldPoint(startPos);
        GridNode targetNode = navMesh.GetNodeFromWorldPoint(targetPos);

        // Initialize open/closed sets
        PathPriorityQueue openSet = new PathPriorityQueue();
        HashSet<GridNode> closedSet = new HashSet<GridNode>();
        
        
        startNode.GCost = 0; // Reset start node's cost
        startNode.HCost = GetDistance(startNode, targetNode);
        openSet.Enqueue(startNode);

        Dictionary<GridNode, GridNode> cameFrom = new Dictionary<GridNode, GridNode>();

        while (openSet.Count > 0)
        {
            // Get node with lowest FCost
            GridNode currentNode = openSet.Dequeue();
            closedSet.Add(currentNode);

            // Path found!
            if (currentNode == targetNode)
            {
                List<Vector3> path = RetracePath(startNode, targetNode, cameFrom);

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
            foreach (GridNode neighbor in GetNeighbors(currentNode))
            {
                if (!neighbor.IsWalkable || closedSet.Contains(neighbor)) 
                    continue;

                int wallPenalty = isNearWall(neighbor) ? 10 : 0;
                int tentativeGCost = currentNode.GCost + GetDistance(currentNode, neighbor) + wallPenalty;
            
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
        }

        return null; // No path found
    }

    // Retrace the path from end to start
    List<Vector3> RetracePath(GridNode startNode, GridNode endNode, Dictionary<GridNode, GridNode> cameFrom)
    {
        List<Vector3> path = new List<Vector3>();
        GridNode currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode.WorldPosition);
            currentNode = cameFrom[currentNode];
        }
        path.Reverse();
        return path;
    }

    // Get neighboring nodes (8-directional)
    List<GridNode> GetNeighbors(GridNode node)
    {
        List<GridNode> neighbors = new List<GridNode>();
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue; // Skip self

                int checkX = node.GridX + x;
                int checkY = node.GridY + y;

                if (checkX >= 0 && checkX < navMesh.GridSizeX &&
                    checkY >= 0 && checkY < navMesh.GridSizeY)
                {
                    neighbors.Add(navMesh.Grid[checkX, checkY]);
                }
            }
        }
        return neighbors;
    }

    bool isNearWall(GridNode node)
    {
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                int checkX = node.GridX + dx;
                int checkY = node.GridY + dy;

                if (checkX >= 0 && checkX < navMesh.GridSizeX && checkY >= 0 && checkY < navMesh.GridSizeY)
                {
                    if (!navMesh.Grid[checkX, checkY].IsWalkable)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    // Calculate Manhattan distance (for grid-based movement)
    int GetDistance(GridNode a, GridNode b)
    {
        int dstX = Mathf.Abs(a.GridX - b.GridX);
        int dstY = Mathf.Abs(a.GridY - b.GridY);
        return dstX + dstY;
    }
}