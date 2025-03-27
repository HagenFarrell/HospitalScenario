using UnityEngine;

public class GridNode
{
    public Vector3 WorldPosition;
    public bool IsWalkable;
    public int GridX, GridY;

    // Costs for A* pathfinding
    public int GCost; // Cost from start to this node
    public int HCost; // Heuristic cost to target
    public int FCost => GCost + HCost; // Total cost

    public GridNode(bool walkable, Vector3 worldPos, int x, int y)
    {
        IsWalkable = walkable;
        WorldPosition = worldPos;
        GridX = x;
        GridY = y;

        // Initialize costs to default values
        GCost = int.MaxValue;
        HCost = 0;
    }
}