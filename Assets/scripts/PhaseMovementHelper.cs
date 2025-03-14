// using UnityEngine;
// using System.Collections.Generic;
// using PhaseLink;
// using System.Collections;

/* DEPRECATED */

// public class PhaseMovementHelper : MonoBehaviour
// {
//     [SerializeField] private Vector3 randomMovementArea = new Vector3(20f, 0f, 20f); // Define the area size
//     [SerializeField] private float minMoveDistance = 5f; // Minimum distance to move
//     [SerializeField] private float maxMoveDistance = 15f; // Maximum distance to move
//     [SerializeField] private float randomMovementInterval = 3f; // Time between random movements
//     [SerializeField] private LayerMask groundLayer; // Layer to raycast against to find valid positions
//     [SerializeField] private int npcsToMovePerFrame = 3; // Amount of npcs to move per frame 

//     private readonly Queue<GameObject> npcUpdateQueue = new Queue<GameObject>();
//     private bool isProcessingUpdates = false;
//     private List<GameObject> cachedMoveList = new List<GameObject>();

//     // Call this method once when the scene starts to cache NPCs
//     private void CacheNPCs()
//     {
//         cachedMoveList.Clear();
//         cachedMoveList.AddRange(GameObject.FindGameObjectsWithTag("Civilians"));
//         cachedMoveList.AddRange(GameObject.FindGameObjectsWithTag("Medicals"));
//         cachedMoveList.AddRange(GameObject.FindGameObjectsWithTag("Hostages"));
//     }

//     // Modify the MoveCiviliansRandomly method
//     public IEnumerator MoveCiviliansRandomly(GamePhase currentPhase)
//     {
//         PhaseManager current = FindObjectOfType<PhaseManager>();
//         if (current == null)
//         {
//             Debug.LogError("PhaseManager not found in scene!");
//             yield return null;
//         }

//         // Cache NPCs at the start
//         CacheNPCs();

//         // Wait until navmesh is ready before starting movement
//         while (!IsNavMeshReady())
//         {
//             Debug.Log("Waiting for NavMesh to initialize...");
//             yield return new WaitForSeconds(1f);
//         }
        
//         Debug.Log("NavMesh is ready! Starting NPC movement.");

//         while (current.GetCurrentPhase() == GamePhase.Phase1)
//         {
//             // Clear previous queue
//             npcUpdateQueue.Clear();

//             // Randomize movement selection strategy
//             int rand = Random.Range(0, 3);
//             List<GameObject> movementList = new List<GameObject>();

//             switch (rand)
//             {
//                 case 0: // Move everyone
//                     movementList.AddRange(cachedMoveList);
//                     break;
//                 case 1: // Move only medicals
//                     movementList.AddRange(cachedMoveList.FindAll(npc => npc.CompareTag("Medicals")));
//                     break;
//                 default: // Move civilians
//                     movementList.AddRange(cachedMoveList.FindAll(npc => npc.CompareTag("Civilians")));
//                     break;
//             }

//             // Queue active NPCs
//             foreach (GameObject npc in movementList)
//             {
//                 if (npc != null && npc.activeInHierarchy)
//                 {
//                     npcUpdateQueue.Enqueue(npc);
//                 }
//             }

//             // Process NPCs in batches
//             if (!isProcessingUpdates && npcUpdateQueue.Count > 0)
//             {
//                 StartCoroutine(ProcessNPCUpdates());
//             }

//             // Wait before next movement
//             yield return new WaitForSeconds(randomMovementInterval + Random.Range(-1f, 1f));
//         }
//     }
    
//     // Make the method public so PhaseManager can use it directly
//     public void MoveNPCToTarget(GameObject npc, Vector3 targetPosition)
//     {
//         if (npc == null) return;
        
//         AIMover mover = npc.GetComponent<AIMover>();
//         if (mover != null)
//         {
//             npc.SetActive(true);
//             mover.enabled = true;
//             mover.SetTargetPosition(targetPosition);
//             StartCoroutine(mover.UpdatePath(mover)); // Start movement coroutine
//         }
//         else
//         {
//             Debug.LogWarning($"No AIMover component found on {npc.name}");
//         }
//     }

//     private Vector3 GetEdgePosition()
//     {
//         Vector3 edgePosition = new Vector3(30f, 0f, Random.Range(35f, 140f));

//         if (Physics.Raycast(edgePosition + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f, groundLayer))
//         {
//             edgePosition.y = hit.point.y; // Ensure NPC is on the ground
//         }

//         return edgePosition;
//     }


//     // Process NPC movements in small batches for better performance
//     private IEnumerator ProcessNPCUpdates()
//     {
//         isProcessingUpdates = true;

//         while (npcUpdateQueue.Count > 0)
//         {
//             // Process only a few NPCs each frame to prevent performance spikes
//             for (int i = 0; i < npcsToMovePerFrame && npcUpdateQueue.Count > 0; i++)
//             {
//                 GameObject npc = npcUpdateQueue.Dequeue();
//                 if (npc != null && npc.activeInHierarchy)
//                 {
//                     // Generate random position within defined area
//                     Vector3 randomOffset = new Vector3(
//                         Random.Range(-randomMovementArea.x/2, randomMovementArea.x/2),
//                         0, 
//                         Random.Range(-randomMovementArea.z/2, randomMovementArea.z/2)
//                     );
                    
//                     // Limit minimum and maximum movement distance
//                     if (randomOffset.magnitude < minMoveDistance)
//                     {
//                         randomOffset = randomOffset.normalized * minMoveDistance;
//                     }
//                     else if (randomOffset.magnitude > maxMoveDistance)
//                     {
//                         randomOffset = randomOffset.normalized * maxMoveDistance;
//                     }
                    
//                     // Calculate world position relative to the NPC's current position
//                     Vector3 targetPosition = npc.transform.position + randomOffset;
                    
//                     // Raycast to find ground height at this position
//                     if (Physics.Raycast(targetPosition + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f, groundLayer))
//                     {
//                         targetPosition.y = hit.point.y; // Set y position to ground level
//                     }

//                     MoveNPCToTarget(npc, targetPosition);
//                 }
//             }
//             yield return null;  // Wait a frame between batches to spread out processing
//         }

//         isProcessingUpdates = false;
//     }

//     public IEnumerator MoveToEdgeAndDespawn()
//     {
//         GameObject[] civilians = GameObject.FindGameObjectsWithTag("Civilians");
//         GameObject[] medicals = GameObject.FindGameObjectsWithTag("Medicals");
//         GameObject[] receptionist = GameObject.FindGameObjectsWithTag("Receptionist");

//         List<GameObject> MoveList = new List<GameObject>(civilians);
//         MoveList.AddRange(medicals);
//         MoveList.AddRange(receptionist);

//         Dictionary<GameObject, Vector3> targetPositions = new Dictionary<GameObject, Vector3>();

//         // Set target positions for all NPCs
//         foreach (GameObject npc in MoveList)
//         {
//             // Animator animator = npc.GetComponent<Animator>();
//             // animator.SetBool("IsWalking", false);
//             // animator.SetBool("IsRunning", true);
//             AIMover mover = npc.GetComponent<AIMover>();
//             if (mover != null)
//             {
//                 mover.SetRunning(true); // Set the AI to run instead of walk
//             }

//             Vector3 targetPosition = GetEdgePosition();
//             targetPositions[npc] = targetPosition;
//             Rigidbody rb = npc.GetComponent<Rigidbody>();
//             if (rb == null)
//             {
//                 rb = npc.gameObject.AddComponent<Rigidbody>();
//                 rb.isKinematic = true; // Prevents physics movement but allows trigger detection
//             }
//             MoveNPCToTarget(npc, targetPosition);
//         }

//         // Check if NPCs have reached the target and clean up when they do
//         while (MoveList.Count > 0)
//         {
//             for (int i = MoveList.Count - 1; i >= 0; i--)
//             {
//                 GameObject npc = MoveList[i];

//                 if (npc == null || !npc.activeInHierarchy)
//                 {
//                     MoveList.RemoveAt(i); // Remove from list if inactive
//                     continue;
//                 }

//                 // Check if NPC has reached the target position
//                 if (Vector3.Distance(npc.transform.position, targetPositions[npc]) <= 10f)
//                 {
//                     // Stop movement and clean up NPC
//                     AIMover mover = npc.GetComponent<AIMover>();
//                     // mover.SetRunning(true);
//                     if (mover != null)
//                     {
//                         // mover.StopAllMovement(); // Stop movement coroutine
//                         mover.enabled = false; // Disable movement script
//                     }

//                     npc.SetActive(false); // Disable NPC instead of destroying
//                     MoveList.RemoveAt(i); // Remove NPC from movement list
//                 }
//             }

//             // Yield to avoid overloading frame updates
//             yield return null;
//         }
//     }

//     private bool IsNavMeshReady()
//     {
//         Pathfinder pathfinder = FindObjectOfType<Pathfinder>();
//         if (pathfinder == null)
//             return false;
            
//         DynamicNavMesh navMesh = FindObjectOfType<DynamicNavMesh>();
        
//         if (navMesh == null || navMesh.Grid == null){
//             return false;
//         }
//         return true;
//     }

// }