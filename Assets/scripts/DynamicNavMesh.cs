using UnityEngine;

public class DynamicNavMesh : MonoBehaviour
{
    public LayerMask walkableLayer;
    public Vector2 gridSize = new Vector2(10, 10);
    public float nodeRadius = 0.5f;
    public float checkHeight = 2f;

    private GridNode[,] grid;
    private float nodeDiameter;
    private int gridSizeX, gridSizeY;

    void Start()
    {
        nodeDiameter = nodeRadius * 2;
        gridSizeX = Mathf.RoundToInt(gridSize.x / nodeDiameter);
        gridSizeY = Mathf.RoundToInt(gridSize.y / nodeDiameter);
        CreateGrid();
    }

    void CreateGrid()
    {
        grid = new GridNode[gridSizeX, gridSizeY];
        Vector3 worldBottomLeft = transform.position - Vector3.right * gridSize.x / 2 - Vector3.forward * gridSize.y / 2;

        // Configure raycast grid density (e.g., 3x3 rays per node)
        int raysPerAxis = 3;
        float stepSize = nodeDiameter / (raysPerAxis - 1);

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                Vector3 worldPoint = worldBottomLeft +
                    Vector3.right * (x * nodeDiameter + nodeRadius) +
                    Vector3.forward * (y * nodeDiameter + nodeRadius);

                bool walkable = false;

                // Cast rays in a grid pattern within the node
                for (int i = 0; i < raysPerAxis; i++)
                {
                    for (int j = 0; j < raysPerAxis; j++)
                    {
                        // Calculate offsets for sub-ray positions
                        float xOffset = -nodeRadius + i * stepSize;
                        float zOffset = -nodeRadius + j * stepSize;
                        Vector3 rayOrigin = worldPoint + new Vector3(xOffset, checkHeight, zOffset);

                        // Cast ray downward and check for walkable surfaces
                        if (Physics.Raycast(rayOrigin, Vector3.down, checkHeight + 0.1f, walkableLayer))
                        {
                            walkable = true;
                            goto EndOfRaycasts; // If any raycast hits, exit early. Performance optimization.
                        }
                    }
                }
            EndOfRaycasts:

                grid[x, y] = new GridNode(walkable, worldPoint, x, y);
            }
        }
    }

    public GridNode GetNodeFromWorldPoint(Vector3 worldPosition)
    {
        float percentX = (worldPosition.x + gridSize.x / 2) / gridSize.x;
        float percentY = (worldPosition.z + gridSize.y / 2) / gridSize.y;
        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);

        int x = Mathf.FloorToInt((gridSizeX) * percentX);
        int y = Mathf.FloorToInt((gridSizeY) * percentY);

        // Debug logs to check alignment
        //Debug.Log($"World Position: {worldPosition}");
        //Debug.Log($"Grid Index: ({x}, {y})");
        //Debug.Log($"Node Walkable: {grid[x, y].IsWalkable}");

        return grid[x, y];
    }

    public int GridSizeX => gridSizeX;
    public int GridSizeY => gridSizeY;
    public GridNode[,] Grid => grid;

    void OnDrawGizmos()
    {
        if (grid != null)
        {
            foreach (GridNode node in grid)
            {
                Gizmos.color = node.IsWalkable ? Color.green : Color.red;
                Gizmos.DrawCube(node.WorldPosition, Vector3.one * (nodeDiameter - 0.1f));
            }
        }
    }
}