using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PathNode {
    public Vector3 worldPosition;
    public bool isWalkable;
    public int gridX, gridY;
    
    public int gCost;
    public int hCost;
    public PathNode parent;
    
    public int fCost { get { return gCost + hCost; } }
    
    public PathNode(bool walkable, Vector3 position, int x, int y) {
        isWalkable = walkable;
        worldPosition = position;
        gridX = x;
        gridY = y;
    }
}

public class PathGrid : MonoBehaviour {
    public Vector2 gridWorldSize = new Vector2(10, 10);
    public float nodeRadius = 0.5f;
    public LayerMask unwalkableMask;
    
    PathNode[,] grid;
    float nodeDiameter;
    int gridSizeX, gridSizeY;
    
    void Awake() {
        nodeDiameter = nodeRadius * 2;
        gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);
        CreateGrid();
    }
    
    void CreateGrid() {
        grid = new PathNode[gridSizeX, gridSizeY];
        Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x/2 - Vector3.forward * gridWorldSize.y/2;
        
        for (int x = 0; x < gridSizeX; x++) {
            for (int y = 0; y < gridSizeY; y++) {
                Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.forward * (y * nodeDiameter + nodeRadius);
                bool walkable = !Physics.CheckSphere(worldPoint, nodeRadius, unwalkableMask);
                grid[x,y] = new PathNode(walkable, worldPoint, x, y);
            }
        }
    }
    
    public List<PathNode> GetNeighbors(PathNode node) {
        List<PathNode> neighbors = new List<PathNode>();
        
        for (int x = -1; x <= 1; x++) {
            for (int y = -1; y <= 1; y++) {
                if (x == 0 && y == 0) continue;
                
                int checkX = node.gridX + x;
                int checkY = node.gridY + y;
                
                if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY) {
                    neighbors.Add(grid[checkX, checkY]);
                }
            }
        }
        return neighbors;
    }
    
    public PathNode NodeFromWorldPoint(Vector3 worldPosition) {
        float percentX = (worldPosition.x + gridWorldSize.x/2) / gridWorldSize.x;
        float percentY = (worldPosition.z + gridWorldSize.y/2) / gridWorldSize.y;
        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);
        
        int x = Mathf.RoundToInt((gridSizeX-1) * percentX);
        int y = Mathf.RoundToInt((gridSizeY-1) * percentY);
        return grid[x,y];
    }
}