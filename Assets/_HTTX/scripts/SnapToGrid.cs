using UnityEngine;

public class SnapToGrid : MonoBehaviour
{
    private DynamicNavMesh navMesh;

    void Start()
    {
        navMesh = FindObjectOfType<DynamicNavMesh>();
    }

    void Update()
    {
        GridNode nearestNode = navMesh.GetNodeFromWorldPoint(transform.position);
        if (nearestNode != null && nearestNode.IsWalkable)
        {
            transform.position = nearestNode.WorldPosition;
        }
    }
}