// using UnityEngine;
// using System.Collections.Generic;
// using PhaseLink;

/* DEPRECATED */

// public class HostageTriggerArea : MonoBehaviour
// {
//     private PhaseManager phaseManager;
    
//     // temporary visual indicator
//     [SerializeField] private bool showDebugVisual = true;
    
//     [SerializeField] private Material debugMaterial;
    
//     [SerializeField] private Color triggerAreaColor = new Color(1f, 0f, 0f, 0.3f);

//     // Cache converted NPCs to avoid converting multiple times
//     private HashSet<GameObject> convertedNPCs = new HashSet<GameObject>();
    
//     // Define a minimum distance between hostages to prevent clumping
//     [SerializeField] private float minDistanceBetweenHostages = 1.5f;
    
//     // Keep track of hostage positions for spacing
//     private List<Vector3> hostagePositions = new List<Vector3>();
    
//     // Define area bounds for positioning hostages
//     private Vector3 areaSize;
//     private Vector3 areaCenter;
    
//     private void Start()
//     {
//         phaseManager = FindObjectOfType<PhaseManager>();
//         if (phaseManager == null)
//         {
//             Debug.LogError("PhaseManager not found in the scene!");
//         }
        
//         // Get the trigger area size from collider if available
//         Collider collider = GetComponent<Collider>();
//         if (collider != null && collider is BoxCollider boxCollider)
//         {
//             areaSize = boxCollider.size;
//             areaCenter = transform.position;
//         }
//         else
//         {
//             // Default size if no box collider
//             areaSize = new Vector3(5f, 1f, 5f);
//             areaCenter = transform.position;
//         }
        
//         if (showDebugVisual)
//         {
//             CreateDebugVisual();
//         }
//     }
    
//     private void CreateDebugVisual()
//     {
//         // Skip if no collider is attached
//         Collider collider = GetComponent<Collider>();
//         if (collider == null) return;
        
//         // Create a child GameObject for visualization
//         GameObject visual = new GameObject("TriggerVisual");
//         visual.transform.SetParent(transform);
//         visual.transform.localPosition = Vector3.zero;
//         visual.transform.localRotation = Quaternion.identity;
        
//         // Add mesh components based on collider type
//         if (collider is BoxCollider boxCollider)
//         {
//             MeshFilter meshFilter = visual.AddComponent<MeshFilter>();
//             meshFilter.mesh = CreateCubeMesh(boxCollider.size);
//             visual.transform.localScale = boxCollider.size;
            
//             MeshRenderer renderer = visual.AddComponent<MeshRenderer>();
//             renderer.material = debugMaterial != null ? debugMaterial : CreateTransparentMaterial();
//             renderer.material.color = triggerAreaColor;
//         }
        
//         // Make sure the visual doesn't interfere with collisions
//         visual.layer = LayerMask.NameToLayer("Ignore Raycast");
//     }
    
//     private Mesh CreateCubeMesh(Vector3 size)
//     {
//         return Resources.GetBuiltinResource<Mesh>("Cube.fbx");
//     }
    
//     private Material CreateTransparentMaterial()
//     {
//         Material mat = new Material(Shader.Find("Transparent/Diffuse"));
//         mat.color = triggerAreaColor;
//         return mat;
//     }

//     private void OnTriggerEnter(Collider npc)
//     {
//         // Only process in Phase 2 or later
//         if (phaseManager == null || phaseManager.GetCurrentPhase() < GamePhase.Phase2)
//             return;

//         // Check if this is a Civilian or Medical
//         if ((npc.CompareTag("Civilians") || npc.CompareTag("Medicals") || npc.CompareTag("Receptionist")) && !convertedNPCs.Contains(npc.gameObject))
//         {
//             ConvertToHostage(npc.gameObject);
//         }
//     }
    
//     private void OnTriggerStay(Collider npc)
//     {
//         // Same check for NPCs that might already be in the area when phase changes
//         if (phaseManager == null || phaseManager.GetCurrentPhase() < GamePhase.Phase2)
//             return;
        
//         if ((npc.CompareTag("Civilians") || npc.CompareTag("Medicals") || npc.CompareTag("Receptionist")) && !convertedNPCs.Contains(npc.gameObject))
//         {
//             ConvertToHostage(npc.gameObject);
//         }
//     }
    
//     private void ConvertToHostage(GameObject npc)
//     {
//         // Store original tag for debugging/reference
//         string originalTag = npc.tag;
//         if (!npc.TryGetComponent<OriginalTag>(out var tagComponent))
//         {
//             tagComponent = npc.AddComponent<OriginalTag>();
//             tagComponent.originalTag = originalTag;
//         }

//         // Change tag to Hostage
//         npc.tag = "Hostages";

//         // Add to converted list to avoid processing again
//         convertedNPCs.Add(npc);

//         // Toggle the yellow ring (ui_disk_02)
//         ToggleYellowRing(npc);

//         // Additional conversion logic...
//         AIMover mover = npc.GetComponent<AIMover>();
//         if (mover != null)
//         {
//             // Modify NPC's behavior for hostages
//             mover.SetRunning(false);
            
//             // Find a non-clumped position for this hostage
//             Vector3 targetPosition = GetNonClumpedPosition(npc.transform.position);
            
//             // Move the hostage to the selected position
//             mover.SetTargetPosition(targetPosition);
            
//             // Add this position to our tracking list
//             hostagePositions.Add(targetPosition);
//         }
        
//         Animator animator = npc.GetComponent<Animator>();
//         if(animator != null){
//             animator.SetBool("IsThreatPresent", true);
//         }
//     }
    
//     private Vector3 GetNonClumpedPosition(Vector3 currentPosition)
//     {
//         // First, try to keep the NPC near their current position if possible
//         if (IsPositionValid(currentPosition))
//         {
//             return currentPosition;
//         }
        
//         // Maximum attempts to find a valid position
//         int maxAttempts = 5;
        
//         for (int i = 0; i < maxAttempts; i++)
//         {
//             // Generate a random position within the trigger area
//             float x = areaCenter.x + Random.Range(-areaSize.x/2 * 0.8f, areaSize.x/2 * 0.8f);
//             float z = areaCenter.z + Random.Range(-areaSize.z/2 * 0.8f, areaSize.z/2 * 0.8f);
//             Vector3 randomPos = new Vector3(x, areaCenter.y, z);
            
//             // Check if the position is valid (not too close to other hostages)
//             if (IsPositionValid(randomPos))
//             {
//                 return randomPos;
//             }
//         }
        
//         // If we couldn't find a good position after max attempts, use a simpler approach
//         // Move the NPC to a position slightly offset from their current location
//         Vector3 offset = new Vector3(
//             Random.Range(-2f, 2f),
//             0,
//             Random.Range(-2f, 2f)
//         );
        
//         return currentPosition + offset;
//     }
    
//     private bool IsPositionValid(Vector3 position)
//     {
//         // Check if this position is too close to any existing hostage position
//         foreach (Vector3 existingPos in hostagePositions)
//         {
//             if (Vector3.Distance(existingPos, position) < minDistanceBetweenHostages)
//             {
//                 return false;
//             }
//         }
        
//         // Also check that the position is within the trigger area bounds
//         Collider collider = GetComponent<Collider>();
//         if (collider != null)
//         {
//             return collider.bounds.Contains(position);
//         }
        
//         return true;
//     }

//     private void ToggleYellowRing(GameObject npc)
//     {
//         // Find the 'ui_disk_02' ring prefab as a child of the NPC
//         GameObject HostageRing = npc.transform.GetChild(2).gameObject;

//         // Check if the ring object exists
//         if (HostageRing != null)
//         {
//             HostageRing.SetActive(true);
//             Vector3 newPosition = HostageRing.transform.localPosition;
//             newPosition.y = 0.3f; 
//             HostageRing.transform.localPosition = newPosition;

//             // Get the Renderer component of the ring
//             Renderer ringRenderer = HostageRing.GetComponent<Renderer>();
//             if (ringRenderer != null)
//             {
//                 // Load the material from the Resources folder
//                 Material highlightMaterial = Resources.Load<Material>("HighLight_Yellow");
//                 if (highlightMaterial != null)
//                 {
//                     ringRenderer.material = highlightMaterial;
//                 }
//                 else
//                 {
//                     Debug.LogError("Material 'HighLight_Yellow' not found in Resources.");
//                 }
//             }
//         }
//         else
//         {
//             Debug.LogWarning("No ui_disk_02 found on " + npc.name);
//         }
//     }

//     // For coordinate-based checking (alternative to trigger collider)
//     public void CheckNPCsInArea(Vector3 center, Vector3 size)
//     {
//         if (phaseManager == null || phaseManager.GetCurrentPhase() < GamePhase.Phase2)
//             return;
            
//         // Find all potential NPCs
//         GameObject[] civilians = GameObject.FindGameObjectsWithTag("Civilians");
//         GameObject[] medicals = GameObject.FindGameObjectsWithTag("Medicals");
        
//         // Combine arrays
//         List<GameObject> npcsToCheck = new List<GameObject>(civilians);
//         npcsToCheck.AddRange(medicals);
        
//         // Check each NPC
//         foreach (GameObject npc in npcsToCheck)
//         {
//             if (convertedNPCs.Contains(npc))
//                 continue;
                
//             // Check if NPC is within the defined area
//             Vector3 npcPos = npc.transform.position;
//             if (npcPos.x >= center.x - size.x/2 && npcPos.x <= center.x + size.x/2 &&
//                 npcPos.y >= center.y - size.y/2 && npcPos.y <= center.y + size.y/2 &&
//                 npcPos.z >= center.z - size.z/2 && npcPos.z <= center.z + size.z/2)
//             {
//                 ConvertToHostage(npc);
//             }
//         }
//     }
    
//     // Reset our tracking when phases change
//     private void OnEnable()
//     {
//         // Subscribe to phase change events if available
//         hostagePositions.Clear();
//     }
    
//     // You might want to reset positions when the scene restarts
//     private void OnDisable()
//     {
//         hostagePositions.Clear();
//         convertedNPCs.Clear();
//     }
// }

// // Helper component to remember original tag
// public class OriginalTag : MonoBehaviour
// {
//     public string originalTag;
// }