using System.Collections.Generic;
using UnityEngine;

public class Pathfinder : MonoBehaviour
{
    private DynamicNavMesh navMesh;

    void Start()
    {
        navMesh = GetComponent<DynamicNavMesh>();
    }

    public List<Vector3> FindPath(Vector3 startPos, Vector3 targetPos)
    {
        GridNode startNode = navMesh.GetNodeFromWorldPoint(startPos);
        GridNode targetNode = navMesh.GetNodeFromWorldPoint(targetPos);

        Debug.Log($"Start Node: {startNode?.WorldPosition} (Walkable: {startNode?.IsWalkable})");
        Debug.Log($"Target Node: {targetNode?.WorldPosition} (Walkable: {targetNode?.IsWalkable})");

        // Debug statements, needed to see if path is actually being generated.
        if (startNode == null || !startNode.IsWalkable)
        {
            Debug.LogError("Start node is unwalkable or null!");
            return null;
        }
        if (targetNode == null || !targetNode.IsWalkable)
        {
            Debug.LogError("Target node is unwalkable or null!");
            return null;
        }

        // Initialize open/closed sets
        List<GridNode> openSet = new List<GridNode>();
        HashSet<GridNode> closedSet = new HashSet<GridNode>();
        startNode.GCost = 0; // Reset start node's cost
        startNode.HCost = GetDistance(startNode, targetNode);
        openSet.Add(startNode);

        Dictionary<GridNode, GridNode> cameFrom = new Dictionary<GridNode, GridNode>();

        while (openSet.Count > 0)
        {
            GridNode currentNode = openSet[0];

            // Find the node with the lowest FCost (and HCost as a tiebreaker)
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].FCost < currentNode.FCost ||
                    (openSet[i].FCost == currentNode.FCost && openSet[i].HCost < currentNode.HCost))
                {
                    currentNode = openSet[i];
                }
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            // Path found!
            if (currentNode == targetNode)
            {
                return RetracePath(startNode, targetNode, cameFrom);
            }

            // Explore neighbors
            foreach (GridNode neighbor in GetNeighbors(currentNode))
            {
                if (!neighbor.IsWalkable || closedSet.Contains(neighbor)) continue;

                int tentativeGCost = currentNode.GCost + GetDistance(currentNode, neighbor);
                if (tentativeGCost < neighbor.GCost || !openSet.Contains(neighbor))
                {
                    cameFrom[neighbor] = currentNode;
                    neighbor.GCost = tentativeGCost;
                    neighbor.HCost = GetDistance(neighbor, targetNode);

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
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

    // Scrapped Manhattan distance over Euclidean distance as its more accurate.
    int GetDistance(GridNode a, GridNode b)
    {
        int dx = Mathf.Abs(a.GridX - b.GridX);
        int dy = Mathf.Abs(a.GridY - b.GridY);
        return Mathf.RoundToInt(Mathf.Sqrt(dx * dx + dy * dy) * 10);
    }
}