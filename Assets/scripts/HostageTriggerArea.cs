using UnityEngine;
using System.Collections.Generic;
using PhaseLink;

public class HostageTriggerArea : MonoBehaviour
{
    private PhaseManager phaseManager;
    
    // temporary visual indicator
    [SerializeField] private bool showDebugVisual = true;
    
    [SerializeField] private Material debugMaterial;
    
    [SerializeField] private Color triggerAreaColor = new Color(1f, 0f, 0f, 0.3f);

    // Cache converted NPCs to avoid converting multiple times
    private HashSet<GameObject> convertedNPCs = new HashSet<GameObject>();
    
    private void Start()
    {
        phaseManager = FindObjectOfType<PhaseManager>();
        if (phaseManager == null)
        {
            Debug.LogError("PhaseManager not found in the scene!");
        }
        // Debug.Log("Loaded!");
        
        if (showDebugVisual)
        {
            CreateDebugVisual();
        }
    }
    
    private void CreateDebugVisual()
    {
        // Skip if no collider is attached
        Collider collider = GetComponent<Collider>();
        if (collider == null) return;
        
        // Create a child GameObject for visualization
        GameObject visual = new GameObject("TriggerVisual");
        visual.transform.SetParent(transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        
        // Add mesh components based on collider type
        if (collider is BoxCollider boxCollider)
        {
            MeshFilter meshFilter = visual.AddComponent<MeshFilter>();
            meshFilter.mesh = CreateCubeMesh(boxCollider.size);
            visual.transform.localScale = boxCollider.size;
            
            MeshRenderer renderer = visual.AddComponent<MeshRenderer>();
            renderer.material = debugMaterial != null ? debugMaterial : CreateTransparentMaterial();
            renderer.material.color = triggerAreaColor;
        }
        
        // Make sure the visual doesn't interfere with collisions
        visual.layer = LayerMask.NameToLayer("Ignore Raycast");
    }
    
    private Mesh CreateCubeMesh(Vector3 size)
    {
        return Resources.GetBuiltinResource<Mesh>("Cube.fbx");
    }
    
    private Material CreateTransparentMaterial()
    {
        Material mat = new Material(Shader.Find("Transparent/Diffuse"));
        mat.color = triggerAreaColor;
        return mat;
    }

    private void OnTriggerEnter(Collider npc)
    {
        // Only process in Phase 2 or later
        if (phaseManager == null || phaseManager.GetCurrentPhase() < GamePhase.Phase2)
            return;

        // Check if this is a Civilian or Medical
        if ((npc.CompareTag("Civilians") || npc.CompareTag("Medicals") || npc.CompareTag("Receptionist")) && !convertedNPCs.Contains(npc.gameObject))
        {
            ConvertToHostage(npc.gameObject);
        }
    }
    
    private void OnTriggerStay(Collider npc)
    {
        // Same check for NPCs that might already be in the area when phase changes
        if (phaseManager == null || phaseManager.GetCurrentPhase() < GamePhase.Phase2)
            return;
        // Debug.Log($"OnTriggerStay called: {npc.name}");
        if ((npc.CompareTag("Civilians") || npc.CompareTag("Medicals") || npc.CompareTag("Receptionist")) && !convertedNPCs.Contains(npc.gameObject))
        {
            ConvertToHostage(npc.gameObject);
        }
    }
    
    private void ConvertToHostage(GameObject npc)
    {
        // Log the conversion
        // Debug.Log($"Converting {npc.name} from {npc.tag} to Hostage");

        // Store original tag for debugging/reference
        string originalTag = npc.tag;
        if (!npc.TryGetComponent<OriginalTag>(out var tagComponent))
        {
            tagComponent = npc.AddComponent<OriginalTag>();
            tagComponent.originalTag = originalTag;
        }

        // Change tag to Hostage
        npc.tag = "Hostages";

        // Add to converted list to avoid processing again
        convertedNPCs.Add(npc);

        // Toggle the yellow ring (ui_disk_02)
        ToggleYellowRing(npc);

        // Additional conversion logic...
        AIMover mover = npc.GetComponent<AIMover>();
        if (mover != null)
        {
            // Modify NPC's behavior for hostages
            // mover.StopAllMovement();
            mover.SetRunning(false);
            mover.SetTargetPosition(transform.position);
        } 
        Animator animator = npc.GetComponent<Animator>();
        if(animator != null){
            animator.SetBool("IsThreatPresent", true);
        }
        
        // Phase 2: NPCs who are now hostages should lie down
        // Debug.Log($"{npc.name} has become a hostage and lies down.");
    }

    private void ToggleYellowRing(GameObject npc)
    {
        // Debug.Log("Toggling the ring");

        // Find the 'ui_disk_02' ring prefab as a child of the NPC
        GameObject HostageRing = npc.transform.GetChild(2).gameObject;

        // Check if the ring object exists
        if (HostageRing != null)
        {
            // Debug.Log("Toggling the ring to true");
            HostageRing.SetActive(true);
            Vector3 newPosition = HostageRing.transform.localPosition;
            newPosition.y = 0.3f; 
            HostageRing.transform.localPosition = newPosition;

            // Get the Renderer component of the ring
            Renderer ringRenderer = HostageRing.GetComponent<Renderer>();
            if (ringRenderer != null)
            {
                // Load the material from the Resources folder
                Material highlightMaterial = Resources.Load<Material>("HighLight_Yellow");
                if (highlightMaterial != null)
                {
                    ringRenderer.material = highlightMaterial;
                }
                else
                {
                    Debug.LogError("Material 'HighLight_Yellow' not found in Resources.");
                }
            }
        }
        else
        {
            Debug.LogWarning("No ui_disk_02 found on " + npc.name);
        }
    }



    
    // For coordinate-based checking (alternative to trigger collider)
    public void CheckNPCsInArea(Vector3 center, Vector3 size)
    {
        if (phaseManager == null || phaseManager.GetCurrentPhase() < GamePhase.Phase2)
            return;
            
        // Find all potential NPCs
        GameObject[] civilians = GameObject.FindGameObjectsWithTag("Civilians");
        GameObject[] medicals = GameObject.FindGameObjectsWithTag("Medicals");
        
        // Combine arrays
        List<GameObject> npcsToCheck = new List<GameObject>(civilians);
        npcsToCheck.AddRange(medicals);
        
        // Check each NPC
        foreach (GameObject npc in npcsToCheck)
        {
            if (convertedNPCs.Contains(npc))
                continue;
                
            // Check if NPC is within the defined area
            Vector3 npcPos = npc.transform.position;
            if (npcPos.x >= center.x - size.x/2 && npcPos.x <= center.x + size.x/2 &&
                npcPos.y >= center.y - size.y/2 && npcPos.y <= center.y + size.y/2 &&
                npcPos.z >= center.z - size.z/2 && npcPos.z <= center.z + size.z/2)
            {
                ConvertToHostage(npc);
            }
        }
    }
}

// Helper component to remember original tag
public class OriginalTag : MonoBehaviour
{
    public string originalTag;
}