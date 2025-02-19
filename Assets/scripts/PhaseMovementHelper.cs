using UnityEngine;
using System.Collections.Generic;
using PhaseLink;
using System.Collections;

public class PhaseMovementHelper : MonoBehaviour
{
    [SerializeField] private Vector3 randomMovementArea = new Vector3(20f, 0f, 20f); // Define the area size
    [SerializeField] private float minMoveDistance = 5f; // Minimum distance to move
    [SerializeField] private float maxMoveDistance = 15f; // Maximum distance to move
    [SerializeField] private float randomMovementInterval = 3f; // Time between random movements
    [SerializeField] private LayerMask groundLayer; // Layer to raycast against to find valid positions

    // Coroutine to handle random civilian movement
    public IEnumerator MoveCiviliansRandomly(GamePhase currentPhase)
    {
        PhaseManager current = FindObjectOfType<PhaseManager>();
        if (current == null)
        {
            Debug.LogError("PhaseManager not found in scene!");
            yield return null;
        }
        while (current.GetCurrentPhase() == GamePhase.Phase1) // Keep moving civilians until coroutine is stopped
        {
            
            // Find all civilian NPCs
            GameObject[] medicals = GameObject.FindGameObjectsWithTag("Medicals");
            GameObject[] hostages = GameObject.FindGameObjectsWithTag("Hostages");
            List<GameObject> MoveList = new List<GameObject>(GameObject.FindGameObjectsWithTag("Civilians"));

            // I want civilians to be more active, etc. etc.
            int rand = UnityEngine.Random.Range(0, 3);
            if(rand == 0){
                Debug.Log("moving medicals and hostages");
                MoveList.AddRange(medicals);
                MoveList.AddRange(hostages);
            } else if(rand <= 1) {
                Debug.Log("Moving medicals");
                MoveList.AddRange(medicals);
            } else Debug.Log("Moving all");
            
            foreach (GameObject MoveMe in MoveList)
            {
                AIMover mover = MoveMe.GetComponent<AIMover>();
                if (mover != null)
                {
                    // Generate random position within defined area
                    Vector3 randomOffset = new Vector3(
                        Random.Range(-randomMovementArea.x/2, randomMovementArea.x/2),
                        0, // Keep y-axis movement at 0
                        Random.Range(-randomMovementArea.z/2, randomMovementArea.z/2)
                    );
                    
                    // Limit minimum and maximum movement distance
                    if (randomOffset.magnitude < minMoveDistance)
                    {
                        randomOffset = randomOffset.normalized * minMoveDistance;
                    }
                    else if (randomOffset.magnitude > maxMoveDistance)
                    {
                        randomOffset = randomOffset.normalized * maxMoveDistance;
                    }
                    
                    // Calculate world position relative to the civilian's current position
                    Vector3 targetWorldPos = MoveMe.transform.position + randomOffset;
                    
                    // Raycast to find ground height at this position
                    if (Physics.Raycast(targetWorldPos + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f, groundLayer))
                    {
                        targetWorldPos.y = hit.point.y; // Set y position to ground level
                    }
                    
                    // Set the target position for the NPC
                    // if (IsPositionWalkable(targetWorldPos))
                    // {
                    mover.SetTargetPosition(targetWorldPos);
                    StartCoroutine(mover.UpdatePath());
                    // }
                }
            }
            
            // Wait before moving NPCs again
            yield return new WaitForSeconds(randomMovementInterval + Random.Range(-1f, 1f)); // Add slight variation
        }
        yield return null;
    }
    
    // Check if a position is walkable using the NavMesh
    private bool IsPositionWalkable(Vector3 position)
    {
        DynamicNavMesh navMesh = GetComponent<DynamicNavMesh>();
        if (navMesh != null)
        {
            GridNode node = navMesh.GetNodeFromWorldPoint(position);
            return node != null && node.IsWalkable;
        }
        return true; // Default to true if navmesh not available
    }
}