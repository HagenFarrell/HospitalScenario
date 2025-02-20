using UnityEngine;
using System.Collections.Generic;
using PhaseLink;
using System.Collections;

public class PhaseManager : MonoBehaviour
{
    private PhaseLinkedList phaseList;
    private PhaseMovementHelper npcMove;
    private Dictionary<string, Vector3> initialPositions = new Dictionary<string, Vector3>(); // Store initial positions of NPCs
    private Dictionary<string, string> initialTags = new Dictionary<string, string>(); // Store initial tags of NPCs
    private Coroutine currentPhaseCoroutine;
    
    [Tooltip("Optional: Use coordinate-based hostage area instead of trigger collider")]
    [SerializeField] private bool useCoordinateCheck = false;
    
    [Tooltip("Center of hostage area (if using coordinate check)")]
    [SerializeField] private Vector3 hostageAreaCenter = Vector3.zero;
    
    [Tooltip("Size of hostage area (if using coordinate check)")]
    [SerializeField] private Vector3 hostageAreaSize = new Vector3(10f, 5f, 10f);
    
    [Tooltip("How often to check for NPCs in the hostage area")]
    [SerializeField] private float hostageCheckInterval = 1.0f;
    
    private HostageTriggerArea hostageArea;

    private void Start()
    {
        phaseList = new PhaseLinkedList();

        // Define the phases
        foreach (GamePhase phase in System.Enum.GetValues(typeof(GamePhase)))
        {
            phaseList.AddPhase(phase);
        }

        // Store initial positions and tags of all NPCs
        StoreInitialNPCState();
        
        // Find or create hostage trigger area
        SetupHostageArea();

        phaseList.SetCurrentToHead();
        StartPhase();
    }
    
    private void SetupHostageArea()
    {
        hostageArea = FindObjectOfType<HostageTriggerArea>();
        
        // If no hostage area exists and we're using coordinate check, start the check coroutine
        if (hostageArea == null && useCoordinateCheck)
        {
            StartCoroutine(PeriodicHostageCheck());
        }
    }
    
    private IEnumerator PeriodicHostageCheck()
    {
        while (true)
        {
            // Only check in Phase 2 or later
            if (phaseList.Current.Phase >= GamePhase.Phase2)
            {
                CheckNPCsInHostageArea();
            }
            yield return new WaitForSeconds(hostageCheckInterval);
        }
    }
    
    private void CheckNPCsInHostageArea()
    {
        if (hostageArea != null)
        {
            // Use the existing hostage area component
            hostageArea.CheckNPCsInArea(hostageAreaCenter, hostageAreaSize);
        }
        else
        {
            // Direct coordinate-based check
            GameObject[] civilians = GameObject.FindGameObjectsWithTag("Civilians");
            GameObject[] medicals = GameObject.FindGameObjectsWithTag("Medicals");
            
            List<GameObject> npcsToCheck = new List<GameObject>(civilians);
            npcsToCheck.AddRange(medicals);
            
            foreach (GameObject npc in npcsToCheck)
            {
                Vector3 npcPos = npc.transform.position;
                if (npcPos.x >= hostageAreaCenter.x - hostageAreaSize.x/2 && 
                    npcPos.x <= hostageAreaCenter.x + hostageAreaSize.x/2 &&
                    npcPos.y >= hostageAreaCenter.y - hostageAreaSize.y/2 && 
                    npcPos.y <= hostageAreaCenter.y + hostageAreaSize.y/2 &&
                    npcPos.z >= hostageAreaCenter.z - hostageAreaSize.z/2 && 
                    npcPos.z <= hostageAreaCenter.z + hostageAreaSize.z/2)
                {
                    // Convert to hostage
                    ConvertToHostage(npc);
                }
            }
        }
    }
    
    private void ConvertToHostage(GameObject npc)
    {
        // Skip if already a hostage
        if (npc.CompareTag("Hostages"))
            return;
            
        // Store original tag if not already stored
        if (!initialTags.ContainsKey(npc.name))
        {
            initialTags[npc.name] = npc.tag;
        }
        
        // Log the conversion
        Debug.Log($"Converting {npc.name} from {npc.tag} to Hostage");
        
        // Change tag to Hostage
        npc.tag = "Hostages";
        
        // Optional: Modify behavior
        AIMover mover = npc.GetComponent<AIMover>();
        if (mover != null)
        {
            mover.StopAllMovement();
        }
        
        // Optional: Change animation
        Animator animator = npc.GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetBool("IsHostage", true);
            animator.SetBool("IsWalking", false);
        }
    }

    private void StoreInitialNPCState()
    {
        // Store civilians' initial state
        GameObject[] civilians = GameObject.FindGameObjectsWithTag("Civilians");
        foreach (GameObject civilian in civilians)
        {
            initialPositions[civilian.name] = civilian.transform.position;
            initialTags[civilian.name] = civilian.tag;
        }

        // Store medicals' initial state
        GameObject[] medicals = GameObject.FindGameObjectsWithTag("Medicals");
        foreach (GameObject medical in medicals)
        {
            initialPositions[medical.name] = medical.transform.position;
            initialTags[medical.name] = medical.tag;
        }

        // Store hostages' initial state
        GameObject[] hostages = GameObject.FindGameObjectsWithTag("Hostages");
        foreach (GameObject hostage in hostages)
        {
            initialPositions[hostage.name] = hostage.transform.position;
            initialTags[hostage.name] = hostage.tag;
        }

        Debug.Log($"Stored initial state for {initialPositions.Count} NPCs");
    }

    private void StartPhase()
    {
        Debug.Log($"Entering Phase: {phaseList.Current.Phase}");
        MoveNPCsForPhase(phaseList.Current.Phase);
    }

    public void NextPhase()
    {
        // Stop any ongoing coroutines from the current phase
        if (currentPhaseCoroutine != null)
        {
            StopCoroutine(currentPhaseCoroutine);
            currentPhaseCoroutine = null;
        }

        if (phaseList.MoveNext())
        {
            Debug.Log("Moving to next phase.");
            StartPhase();
        }
        else
        {
            Debug.Log("Already at the last phase!");
        }
    }

    public void PreviousPhase()
    {
        // Stop any ongoing coroutines
        if (currentPhaseCoroutine != null)
        {
            StopCoroutine(currentPhaseCoroutine);
            currentPhaseCoroutine = null;
        }

        if (phaseList.MovePrevious())
        {
            Debug.Log("Moving to previous phase.");
            StartPhase();
        }
        else
        {
            Debug.Log("Already at the first phase!");
        }
    }

    private void ResetNPCsToInitialPositions()
    {
        Debug.Log("Resetting NPCs to initial positions for Phase 1");
        
        // Get references to all NPCs - whether active or inactive
        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform transform in allTransforms)
        {
            GameObject npc = transform.gameObject;
            
            // Check if this is one of our tracked NPCs
            if (initialPositions.ContainsKey(npc.name))
            {
                // Re-enable if inactive
                if (!npc.activeInHierarchy)
                {
                    npc.SetActive(true);
                }
                
                // Reset tag to original
                if (initialTags.ContainsKey(npc.name))
                {
                    npc.tag = initialTags[npc.name];
                }
                
                ResetNPC(npc);
            }
        }
    }
    
    private void ResetNPC(GameObject npc)
    {
        if (initialPositions.ContainsKey(npc.name))
        {
            // Reset position to initial
            npc.transform.position = initialPositions[npc.name];
            
            // Reset animator if available
            Animator animator = npc.GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetBool("IsWalking", false);
                animator.SetBool("IsHostage", npc.CompareTag("Hostages")); // Set hostage animation state based on tag
            }
            
            // Reset and enable AIMover
            AIMover mover = npc.GetComponent<AIMover>();
            if (mover != null)
            {
                mover.enabled = true;
                mover.SetTargetPosition(initialPositions[npc.name]);
                mover.StopAllMovement(); // Stop any ongoing movement
            }
        }
    }

    private void MoveNPCsForPhase(GamePhase phase)
    {
        Debug.Log($"Moving NPCs for phase: {phase}");
        
        npcMove = FindObjectOfType<PhaseMovementHelper>();
        
        switch (phase)
        {
            case GamePhase.Phase1:
                // If coming back to Phase1, reset NPCs to initial positions
                if (currentPhaseCoroutine != null) {
                    StopCoroutine(currentPhaseCoroutine);
                    currentPhaseCoroutine = null;
                }
                
                // Reset NPCs to their initial positions when returning to Phase 1
                ResetNPCsToInitialPositions();
                
                // Start random movement for civilians
                currentPhaseCoroutine = StartCoroutine(npcMove.MoveCiviliansRandomly(GetCurrentPhase()));
                break;
                
            case GamePhase.Phase2:
                if (currentPhaseCoroutine != null) {
                    StopCoroutine(currentPhaseCoroutine);
                    currentPhaseCoroutine = null;
                }
                currentPhaseCoroutine = StartCoroutine(npcMove.MoveToEdgeAndDespawn());
                break;
            // Add cases for other phases as needed
        }
    }

    public GamePhase GetCurrentPhase(){
        return phaseList.Current.Phase;
    }
}