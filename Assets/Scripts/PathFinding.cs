using UnityEngine;
using System.Collections.Generic;
using System.Linq;
public class Pathfinding : MonoBehaviour {
    PathGrid grid;
    
    void Awake() {
        grid = GetComponent<PathGrid>();
    }
    
    public List<Vector3> FindPath(Vector3 startPos, Vector3 targetPos) {
        PathNode startNode = grid.NodeFromWorldPoint(startPos);
        PathNode targetNode = grid.NodeFromWorldPoint(targetPos);
        
        List<PathNode> openSet = new List<PathNode>();
        HashSet<PathNode> closedSet = new HashSet<PathNode>();
        openSet.Add(startNode);
        
        while (openSet.Count > 0) {
            PathNode currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++) {
                if (openSet[i].fCost < currentNode.fCost || 
                        openSet[i].fCost == currentNode.fCost && 
                            openSet[i].hCost < currentNode.hCost) {
                    currentNode = openSet[i];
                }
            }
            
            openSet.Remove(currentNode);
            closedSet.Add(currentNode);
            
            if (currentNode == targetNode) {
                return RetracePath(startNode, targetNode);
            }
            
            foreach (PathNode neighbor in grid.GetNeighbors(currentNode)) {
                if (!neighbor.isWalkable || closedSet.Contains(neighbor)) continue;
                
                int newMovementCostToNeighbor = currentNode.gCost + GetDistance(currentNode, neighbor);
                if (newMovementCostToNeighbor < neighbor.gCost || !openSet.Contains(neighbor)) {
                    neighbor.gCost = newMovementCostToNeighbor;
                    neighbor.hCost = GetDistance(neighbor, targetNode);
                    neighbor.parent = currentNode;
                    
                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }
        
        return null;
    }
    
    List<Vector3> RetracePath(PathNode startNode, PathNode endNode) {
        List<Vector3> path = new List<Vector3>();
        PathNode currentNode = endNode;
        
        while (currentNode != startNode) {
            path.Add(currentNode.worldPosition);
            currentNode = currentNode.parent;
        }
        path.Reverse();
        return path;
    }
    
    int GetDistance(PathNode nodeA, PathNode nodeB) {
        int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int dstY = Mathf.Abs(nodeA.gridY - nodeB.gridY);
        
        if (dstX > dstY)
            return 14 * dstY + 10 * (dstX - dstY);
        return 14 * dstX + 10 * (dstY - dstX);
    }
}