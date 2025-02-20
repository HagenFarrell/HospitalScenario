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

    private Vector3 GetEdgePosition()
    {
        Vector3 edgePosition = new Vector3(61f, 0f, Random.Range(40f, 140f));

        if (Physics.Raycast(edgePosition + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f, groundLayer))
        {
            edgePosition.y = hit.point.y; // Ensure NPC is on the ground
        }

        return edgePosition;
    }

    private void MoveNPCToTarget(GameObject npc, Vector3 targetPosition)
    {
        AIMover mover = npc.GetComponent<AIMover>();
        if (mover != null)
        {
            npc.SetActive(true);
            mover.enabled = true;
            mover.SetTargetPosition(targetPosition);
            StartCoroutine(mover.UpdatePath()); // Start movement coroutine
        }
    }

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
            
            foreach (GameObject npc in MoveList)
            {
                // Generate random position within defined area
                Vector3 randomOffset = new Vector3(
                    Random.Range(-randomMovementArea.x/2, randomMovementArea.x/2),
                    0, 
                    Random.Range(-randomMovementArea.z/2, randomMovementArea.z/2)
                );
                
                // Limit minimum and maximum movement distance
                if (randomOffset.magnitude < minMoveDistance) {
                    randomOffset = randomOffset.normalized * minMoveDistance;
                } else if (randomOffset.magnitude > maxMoveDistance) {
                    randomOffset = randomOffset.normalized * maxMoveDistance;
                }
                
                // Calculate world position relative to the civilian's current position
                Vector3 targetPosition = npc.transform.position + randomOffset;
                
                // Raycast to find ground height at this position
                if (Physics.Raycast(targetPosition + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f, groundLayer))
                {
                    targetPosition.y = hit.point.y; // Set y position to ground level
                }

                MoveNPCToTarget(npc, targetPosition);
            }
            
            // Wait before moving NPCs again
            yield return new WaitForSeconds(randomMovementInterval + Random.Range(-1f, 1f)); // Add slight variation
        }
        yield return null;
    }

    public IEnumerator MoveToEdgeAndDespawn()
    {
        GameObject[] civilians = GameObject.FindGameObjectsWithTag("Civilians");
        GameObject[] medicals = GameObject.FindGameObjectsWithTag("Medicals");

        List<GameObject> MoveList = new List<GameObject>(civilians);
        MoveList.AddRange(medicals);

        Dictionary<GameObject, Vector3> targetPositions = new Dictionary<GameObject, Vector3>();

        // Set target positions for all NPCs
        foreach (GameObject npc in MoveList)
        {
            Vector3 targetPosition = GetEdgePosition();
            targetPositions[npc] = targetPosition;
            MoveNPCToTarget(npc, targetPosition);
        }

        // Check if NPCs have reached the target and clean up when they do
        while (MoveList.Count > 0)
        {
            for (int i = MoveList.Count - 1; i >= 0; i--)
            {
                GameObject npc = MoveList[i];

                if (npc == null || !npc.activeInHierarchy)
                {
                    MoveList.RemoveAt(i); // Remove from list if inactive
                    continue;
                }

                // Check if NPC has reached the target position
                if (Vector3.Distance(npc.transform.position, targetPositions[npc]) <= 0.5f)
                {
                    // Stop movement and clean up NPC
                    AIMover mover = npc.GetComponent<AIMover>();
                    if (mover != null)
                    {
                        mover.StopAllMovement(); // Stop movement coroutine
                        mover.enabled = false; // Disable movement script
                    }

                    npc.SetActive(false); // Disable NPC instead of destroying
                    MoveList.RemoveAt(i); // Remove NPC from movement list
                }
            }

            // Yield to avoid overloading frame updates
            yield return null;
        }
    }

}