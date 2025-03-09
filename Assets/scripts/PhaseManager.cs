using UnityEngine;
using System.Collections.Generic;
using PhaseLink;
using System.Collections;

public class PhaseManager : MonoBehaviour
{
    private PhaseLinkedList phaseList;
    private PhaseMovementHelper npcMove;
    private Player playerRole;
    private Coroutine currentPhaseCoroutine;
    private HostageTriggerArea hostageArea;
    
    // Reference to physician hostage for Phase 3
    private GameObject physicianHostage;
    
    // Reference to temporary gamma knife object
    private GameObject gammaKnifeObject;
    
    // References to different NPC groups
    private GameObject[] villainsInside;
    private GameObject[] villainsOutside;
    private List<GameObject> playerUnits;
    private GameObject[] FD;
    private GameObject[] LLE;
    private List<GameObject> villains;
    // private GameObject superVillain;
    private GameObject receptionist;
    private int egress;
    public delegate void EgressSelectedHandler(int egressPhase);
    public static event EgressSelectedHandler OnEgressSelected;


    private void Start()
    {
        phaseList = new PhaseLinkedList();

        // Define the phases
        foreach (GamePhase phase in System.Enum.GetValues(typeof(GamePhase)))
        {
            phaseList.AddPhase(phase);
        }

        FD = GameObject.FindGameObjectsWithTag("FireDepartment");
        LLE = GameObject.FindGameObjectsWithTag("LawEnforcement");
        playerUnits = new List<GameObject>(FD);
        playerUnits.AddRange(LLE);

        // Store initial positions and tags of all NPCs in the first phase node
        StoreInitialNPCState();
        
        // Find or create hostage trigger area
        SetupHostageArea();
        
        // Find key NPCs
        FindKeyNPCs();
        
        // Create temporary gamma knife object
        CreateTemporaryGammaKnife();
        

        phaseList.SetCurrentToHead();
        OnEgressSelected += ExecuteEgressPhase;
        StartPhase();
    }

    private void Update()
    {
        if (phaseList.Current.Phase == GamePhase.Phase7)
        {
            SetEgressPhase();
        }
    }


    // Phase 1 with new waypoint paths
    private void ExecutePhase1()
    {
        Debug.Log("Executing Phase 1: NPCs begin waypoint movement");
        
        // Initialize civilians on their waypoint paths
        GameObject[] civilians = GameObject.FindGameObjectsWithTag("Civilians");
        foreach (GameObject civilian in civilians)
        {
            // Set animation state to walking
            Animator animator = civilian.GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetBool("IsWalking", true);
            }
            
            // Enable the WaypointMover component
            WaypointMover mover = civilian.GetComponent<WaypointMover>();
            if (mover != null)
            {
                // Reset to first waypoint in the path
                if (mover.waypoints != null && mover.waypoints.transform.childCount > 0)
                {
                    mover.currentWaypoint = mover.waypoints.GetNextWaypoint(null, GetCurrentPhase());
                    mover.enabled = true;
                }
            }
        }
        
        // Initialize medicals on their waypoint paths
        GameObject[] medicals = GameObject.FindGameObjectsWithTag("Medicals");
        foreach (GameObject medical in medicals)
        {
            // Set animation state to walking
            Animator animator = medical.GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetBool("IsWalking", true);
            }
            
            // Enable the WaypointMover component
            WaypointMover mover = medical.GetComponent<WaypointMover>();
            if (mover != null)
            {
                // Reset to first waypoint in the path
                if (mover.waypoints != null && mover.waypoints.transform.childCount > 0)
                {
                    mover.currentWaypoint = mover.waypoints.GetNextWaypoint(null, GetCurrentPhase());
                    mover.enabled = true;
                }
            }
        }
    }



















    private int SetEgressPhase()
    {
        // Debug.Log("Awaiting Egress Selection...");
        playerRole = FindObjectOfType<Player>();
        if(playerRole == null){
            Debug.LogError("playerRole null");
        }
        if (playerRole.getPlayerRole() == Player.Roles.Instructor)
        {
            // Debug.Log("Instructor! Hi!!!");
            if (Input.GetKeyDown(KeyCode.Z)) return TriggerEgressSelected(1);
            if (Input.GetKeyDown(KeyCode.X)) return TriggerEgressSelected(2);
            if (Input.GetKeyDown(KeyCode.C)) return TriggerEgressSelected(3);
            if (Input.GetKeyDown(KeyCode.V)) return TriggerEgressSelected(4);
            if (Input.GetKeyDown(KeyCode.B)) return TriggerEgressSelected(Random.Range(1, 5));

            return 0;
        }
        else
        {
            Debug.Log("Only the instructor can select the egress phase.");
            return 0;
        }
    }

    private int TriggerEgressSelected(int phase)
    {
        Debug.Log($"TriggerEgressSelected called with phase {phase}");

        if (OnEgressSelected != null)
        {
            Debug.Log("Triggering OnEgressSelected event");
            OnEgressSelected.Invoke(phase);
        }
        else
        {
            Debug.LogError("OnEgressSelected is NULL! No subscribers.");
        }
        
        return phase;
    }

    private void FindKeyNPCs()
    {
        villainsInside = GameObject.FindGameObjectsWithTag("Villains");
        villainsOutside = GameObject.FindGameObjectsWithTag("OutsideVillains");
        // superVillain = GameObject.FindGameObjectWithTag("SuperVillain");
        receptionist = GameObject.FindGameObjectWithTag("Receptionist");
        
        villains = new List<GameObject>(villainsInside);
        villains.AddRange(villainsOutside);
        
        Debug.Log($"Found {villainsInside.Length} inside villains, {villainsOutside.Length} outside villains");
        // if (superVillain != null) Debug.Log("Found SuperVillain");
        if (receptionist != null) Debug.Log("Found Receptionist");
    }
    
    private void CreateTemporaryGammaKnife()
    {
        // Create a temporary placeholder for the gamma knife
        gammaKnifeObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gammaKnifeObject.name = "TemporaryGammaKnife";
        gammaKnifeObject.transform.position = new Vector3(120f, 1f, 80f); // Placeholder position
        gammaKnifeObject.transform.localScale = new Vector3(2f, 1f, 1f);
        gammaKnifeObject.GetComponent<Renderer>().material.color = Color.red;
        gammaKnifeObject.SetActive(false); // Hide initially
        
        Debug.Log("Created temporary gamma knife object");
    }
    
    private void SetupHostageArea()
    {
        hostageArea = FindObjectOfType<HostageTriggerArea>();
    }
    
    private void ConvertToHostage(GameObject npc)
    {
        // Skip if already a hostage
        if (npc.CompareTag("Hostages"))
            return;
            
        // Store original tag if not already stored in current phase
        if (!phaseList.Current.NPCTags.ContainsKey(npc.name))
        {
            phaseList.Current.NPCTags[npc.name] = npc.tag;
        }
        
        // Log the conversion
        // Debug.Log($"Converting {npc.name} from {npc.tag} to Hostage");
        
        // Change tag to Hostage
        npc.tag = "Hostages";
        
        // Optional: Modify behavior
        AIMover mover = npc.GetComponent<AIMover>();
        if (mover != null)
        {
            // mover.StopAllMovement();
        }
        
        // Optional: Change animation
        Animator animator = npc.GetComponent<Animator>();
        if (animator != null)
        {
            // animator.SetBool("IsHostage", true);
            animator.SetBool("IsWalking", false);
        }
    }

    private void StoreInitialNPCState()
    {
        if (phaseList.Head == null)
            return;

        // Store all types of NPCs initial state
        StoreNPCTypeState("Civilians", phaseList.Head);
        StoreNPCTypeState("Medicals", phaseList.Head);
        StoreNPCTypeState("Hostages", phaseList.Head);
        StoreNPCTypeState("Villains", phaseList.Head);
        StoreNPCTypeState("OutsideVillains", phaseList.Head);
        StoreNPCTypeState("Receptionist", phaseList.Head);
        
        Debug.Log($"Stored initial state for {phaseList.Head.NPCPositions.Count} NPCs in Phase 1");
    }
    
    private void StoreNPCTypeState(string tag, PhaseNode node)
    {
        GameObject[] npcs = GameObject.FindGameObjectsWithTag(tag);
        foreach (GameObject npc in npcs)
        {
            node.NPCPositions[npc.name] = npc.transform.position;
            node.NPCTags[npc.name] = npc.tag;
            
            // Store active state as well in NPCTags by prepending "Active_" to indicate the GameObject is active
            string activeKey = "Active_" + npc.name;
            node.NPCTags[activeKey] = npc.activeInHierarchy ? "True" : "False";
        }
    }

    private void StartPhase()
    {
        Debug.Log($"Entering Phase: {phaseList.Current.Phase}");
        
        // First, check if we need to despawn civilians and medicals in Phase 3
        if (phaseList.Current.Phase == GamePhase.Phase3)
        {
            DespawnRemainingCiviliansAndMedicals();
        }
        
        MoveNPCsForPhase(phaseList.Current.Phase);
    }

    private void DespawnRemainingCiviliansAndMedicals()
    {
        // Find and disable any remaining civilians
        GameObject[] civilians = GameObject.FindGameObjectsWithTag("Civilians");
        foreach (GameObject civilian in civilians)
        {
            Debug.Log($"Phase 3: Despawning civilian {civilian.name}");
            civilian.SetActive(false);
        }
        
        // Find and disable any remaining medicals
        GameObject[] medicals = GameObject.FindGameObjectsWithTag("Medicals");
        foreach (GameObject medical in medicals)
        {
            Debug.Log($"Phase 3: Despawning medical {medical.name}");
            medical.SetActive(false);
        }
        
        // Note: We're not storing disabled state for these NPCs anymore
        // to optimize performance
    }

    public void NextPhase()
    {
        // Stop any ongoing coroutines from the current phase
        if (currentPhaseCoroutine != null)
        {
            StopCoroutine(currentPhaseCoroutine);
            currentPhaseCoroutine = null;
        }
        
        // Store the current positions of NPCs before moving to the next phase
        CaptureCurrentNPCState();

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
            RestoreNPCsFromPhaseNode(phaseList.Current);
            StartPhase();
        }
        else
        {
            Debug.Log("Already at the first phase!");
        }
    }
    
    private void CaptureCurrentNPCState()
    {
        if (phaseList.Current.Next != null)
        {
            // Capture state even if the next phase already exists - we want to update
            // with the latest info each time we move through phases
            PhaseNode nextPhase = phaseList.Current.Next;
            
            // Clear existing data in the next phase to avoid duplicates or stale data
            nextPhase.NPCPositions.Clear();
            nextPhase.NPCTags.Clear();
            
            // Store only active NPCs' current state in the next phase
            StoreActiveNPCsCurrentState(nextPhase);
            
            Debug.Log($"Updated state for next Phase {nextPhase.Phase} with {nextPhase.NPCPositions.Count} NPCs");
            return;
        }
        
        // If we're at the last node, there's no next phase to capture state for
        Debug.Log("Already at the last phase, no need to capture state for next phase");
    }
    
    private void StoreActiveNPCsCurrentState(PhaseNode targetNode)
    {
        // Store only active NPCs by tag type - skip disabled ones
        StoreActiveNPCTypeState("Hostages", targetNode);
        StoreActiveNPCTypeState("Villains", targetNode);
        StoreActiveNPCTypeState("OutsideVillains", targetNode);
        StoreActiveNPCTypeState("PhysicianHostage", targetNode);
        StoreActiveNPCTypeState("Receptionist", targetNode);
        
        // Only store active civilians and medicals - skip disabled ones
        if (phaseList.Current.Phase < GamePhase.Phase3)
        {
            StoreActiveNPCTypeState("Civilians", targetNode);
            StoreActiveNPCTypeState("Medicals", targetNode);
        }
    }
    
    private void StoreActiveNPCTypeState(string tag, PhaseNode targetNode)
    {
        GameObject[] npcs = GameObject.FindGameObjectsWithTag(tag);
        foreach (GameObject npc in npcs)
        {
            // Only store state for active NPCs
            if (npc.activeInHierarchy)
            {
                targetNode.NPCPositions[npc.name] = npc.transform.position;
                targetNode.NPCTags[npc.name] = npc.tag;
                
                // Store active state
                string activeKey = "Active_" + npc.name;
                targetNode.NPCTags[activeKey] = "True";
            }
        }
    }
    
    private void RestoreNPCsFromPhaseNode(PhaseNode node)
    {
        Debug.Log($"Restoring NPCs to state from Phase {node.Phase}");
        
        // Get references to all NPCs - whether active or inactive
        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform transform in allTransforms)
        {
            GameObject npc = transform.gameObject;
            
            // Check if this is one of our tracked NPCs
            if (node.NPCPositions.ContainsKey(npc.name))
            {
                // Check if we should enable or disable based on stored state
                string activeKey = "Active_" + npc.name;
                if (node.NPCTags.ContainsKey(activeKey))
                {
                    bool shouldBeActive = node.NPCTags[activeKey] == "True";
                    if (npc.activeInHierarchy != shouldBeActive)
                    {
                        npc.SetActive(shouldBeActive);
                    }
                }
                
                // Only proceed with position and tag restoration if the object should be active
                if (npc.activeInHierarchy)
                {
                    // Reset tag to the one stored in the phase node
                    if (node.NPCTags.ContainsKey(npc.name))
                    {
                        npc.tag = node.NPCTags[npc.name];
                    }
                    
                    ResetNPC(npc, node);
                }
            }
        }
    }
    
    private void ResetNPC(GameObject npc, PhaseNode node)
    {
        if (node.NPCPositions.ContainsKey(npc.name))
        {
            // Reset position to the one stored in the phase node
            npc.transform.position = node.NPCPositions[npc.name];
            
            // Reset animator if available
            Animator animator = npc.GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetBool("IsWalking", false);
                animator.SetBool("IsThreatPresent", npc.CompareTag("Hostages") || npc.CompareTag("PhysicianHostage"));
                animator.SetBool("ToRummaging", false);
            }
            
            // Reset and enable AIMover
            AIMover mover = npc.GetComponent<AIMover>();
            if (mover != null)
            {
                mover.enabled = true;
                mover.SetTargetPosition(node.NPCPositions[npc.name]);
                // mover.StopAllMovement(); // Stop any ongoing movement
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
                // If coming back to Phase1, NPCs should already be reset to initial positions
                ExecutePhase1();
                break;
                
            case GamePhase.Phase2:
            // add alarm
            // add pull out gun
                if (currentPhaseCoroutine != null) {
                    StopCoroutine(currentPhaseCoroutine);
                    currentPhaseCoroutine = null;
                }
                
                // Villains pull out guns
                foreach(GameObject villain in villains){
                    GameObject gun = villain.transform.GetChild(2).gameObject;
                    gun.SetActive(true);
                }
                
                // Receptionist hits duress alarm
                if (receptionist != null) {
                    Debug.Log("Duress alarm activated. Dispatcher notified.");
                }
                
                // Start civilians and medicals running for the exit
                currentPhaseCoroutine = StartCoroutine(npcMove.MoveToEdgeAndDespawn());
                break;
                
            case GamePhase.Phase3:
                if (currentPhaseCoroutine != null) {
                    StopCoroutine(currentPhaseCoroutine);
                    currentPhaseCoroutine = null;
                }
                
                // Select a medical NPC to be the physician hostage
                GameObject[] hostages = GameObject.FindGameObjectsWithTag("Hostages");
                if (hostages.Length > 0) {
                    foreach (GameObject hostage in hostages) {
                        OriginalTag originalTag = hostage.GetComponent<OriginalTag>();
                        if (originalTag != null && originalTag.originalTag == "Medicals") {
                            physicianHostage = hostage;
                            physicianHostage.tag = "PhysicianHostage";
                            Debug.Log($"The villains have taken {physicianHostage.name} hostage!");
                            break;
                        }
                    }
                }
                
                break;
                
            case GamePhase.Phase4:
            // add animation for tampering with machine
                if (currentPhaseCoroutine != null) {
                    StopCoroutine(currentPhaseCoroutine);
                    currentPhaseCoroutine = null;
                }
                
                // Activate the gamma knife object
                if (gammaKnifeObject != null) {
                    gammaKnifeObject.SetActive(true);
                }
                
                // Two villains take the physician hostage to the gamma knife room
                if (physicianHostage != null && villainsInside != null && villainsInside.Length >= 2) {
                    // Move two inside villains with the hostage
                    Vector3 gammaKnifePosition = gammaKnifeObject.transform.position;
                    
                    // First villain moves with hostage
                    Vector3 NPCPosition = new Vector3(-16.0f, 0, 66.6f);
                    npcMove.MoveNPCToTarget(villainsInside[0], NPCPosition);
                    
                    // Second villain moves with hostage
                    NPCPosition = new Vector3(-13.3f, 0, 68.8f);
                    npcMove.MoveNPCToTarget(villainsInside[1], NPCPosition);
                    
                    // Move hostage to gamma knife
                    NPCPosition = new Vector3(-15.6f, 0, 65f);
                    npcMove.MoveNPCToTarget(physicianHostage, NPCPosition);
                    Animator animator = physicianHostage.GetComponent<Animator>();
                    if(animator != null) animator.SetBool("IsThreatPresent", false);
                    
                    Debug.Log($"Two villains are taking {physicianHostage.name} to the gamma knife room");
                    // Start the work on the gamma knife
                    currentPhaseCoroutine = StartCoroutine(WorkOnGammaKnife(physicianHostage, NPCPosition));
                }
                
                // Outside villains move inside to reinforce
                if (villainsOutside != null) {
                    Vector3 lobbyPosition1 = new Vector3(2.3f, 0, 105.8f);
                    Vector3 lobbyPosition2 = new Vector3(5.8f, 0, 112.3f);
                    
                    if (villainsOutside.Length > 0) {
                        npcMove.MoveNPCToTarget(villainsOutside[0], lobbyPosition1);
                    }
                    
                    if (villainsOutside.Length > 1) {
                        npcMove.MoveNPCToTarget(villainsOutside[1], lobbyPosition2);
                    }
                    
                    Debug.Log("Outside villains move into the lobby to reinforce");
                }
                
                // Last inside villain stays put
                if (villainsInside != null && villainsInside.Length >= 3) {
                    // Do nothing with the third villain - they stay in place
                    Debug.Log($"{villainsInside[2].name} stays behind in the lobby");
                }
                
                
                break;
            case GamePhase.Phase5:
                // law enforcement spawns in. 
                // tamper alarm goes off (dispatcher and those in the building can hear).
                // 3 baddies move to long hallway, cafeteria, and lobby - DONE
                // rad dose hemisphere is togglable by instructor.
                // Two villains take the physician hostage to the gamma knife room
                if (currentPhaseCoroutine != null) {
                    StopCoroutine(currentPhaseCoroutine);
                    currentPhaseCoroutine = null;
                }

                // Ensure the physician hostage stops rummaging animation when moving
                if (physicianHostage != null) {
                    Animator physicianAnimator = physicianHostage.GetComponent<Animator>();
                    if (physicianAnimator != null) {
                        physicianAnimator.SetBool("ToRummaging", false);
                        physicianAnimator.SetBool("IsWalking", true);
                    }
                }

                if (villainsOutside != null && villainsInside.Length >= 2) {
                    // Move two inside villains with the hostage
                    Vector3 gammaKnifePosition = gammaKnifeObject.transform.position;
                    
                    // First villain moves with hostage
                    Vector3 NPCPosition = new Vector3(3.3f, 0, 73.3f);
                    npcMove.MoveNPCToTarget(villainsOutside[0], NPCPosition);
                    
                    // Second villain moves with hostage
                    NPCPosition = new Vector3(-8.3f, 0, 95.3f);
                    npcMove.MoveNPCToTarget(villainsOutside[1], NPCPosition);
                    
                    // Move hostage to gamma knife
                    NPCPosition = new Vector3(-6.8f, 0, 112.3f);
                    npcMove.MoveNPCToTarget(villainsInside[2], NPCPosition);
                    
                }
                break;
            case GamePhase.Phase6:
                // VFD pulls up
                // source goes into canister into backpack
                // all adversaries and physicianhostage group up & get ready to leave
                if (currentPhaseCoroutine != null) {
                    StopCoroutine(currentPhaseCoroutine);
                    currentPhaseCoroutine = null;
                }

                Vector3 youMoveHere = new Vector3(0f, 0, 113f);
                float radius = 3.5f;
                npcMove.MoveNPCToTarget(physicianHostage, youMoveHere);
                foreach(GameObject villain in villains){
                    npcMove.MoveNPCToTarget(villain, GetRandomPointInRadius(youMoveHere, radius));
                }
                break;
            case GamePhase.Phase7:
                if (currentPhaseCoroutine != null) 
                {
                    StopCoroutine(currentPhaseCoroutine);
                    currentPhaseCoroutine = null;
                }

                if (physicianHostage != null) {
                    Animator physicianAnimator = physicianHostage.GetComponent<Animator>();
                    if (physicianAnimator != null) {
                        physicianAnimator.SetBool("IsThreatPresent", true);
                    }
                }
                
                // currentPhaseCoroutine = StartCoroutine(WaitForEgressSelection());
                OnEgressSelected += ExecuteEgressPhase;
                break;

        }
        
        // After moving NPCs for the phase, store their new positions
        StoreCurrentPhaseState();
    }
    
    private void StoreCurrentPhaseState()
    {
        // Store the state of only active NPCs in the current phase
        StoreActiveNPCsCurrentState(phaseList.Current);
        Debug.Log($"Stored state for {phaseList.Current.NPCPositions.Count} NPCs in Phase {phaseList.Current.Phase}");
    }

    private void ExecuteEgressPhase(int selectedEgress)
    {
        OnEgressSelected -= ExecuteEgressPhase; // Unsubscribe to prevent multiple calls

        Debug.Log($"Egress phase {selectedEgress} selected!");
        egress = selectedEgress;

        switch(egress) // g = random
        {
            case 1: // a
                // Phase Egress 1: Adversaries move to the front emergency exit
                Debug.Log("Phase Egress: " + egress);
                Vector3 youMoveHere = new Vector3(21.8f, 0, 72.3f);
                float radius = 2f;
                moveEgress(youMoveHere, radius);
                break;
            case 2: // s
                // Phase Egress 2: Adversaries move to the rear emergency exit
                Debug.Log("Phase Egress: " + egress);
                youMoveHere = new Vector3(-12.3f, 0, 95.8f);
                radius = 3f;
                moveEgress(youMoveHere, radius);
                break;
            case 3: // d
                // Phase Egress 3: Adversaries move to the lobby exit
                Debug.Log("Phase Egress: " + egress);
                youMoveHere = new Vector3(20.8f, 0, 113.3f);
                radius = 3f;
                moveEgress(youMoveHere, radius);
                break;
            case 4: // f
                // Phase Egress 4: Adversaries move to the rear exit
                Debug.Log("Phase Egress: " + egress);
                youMoveHere = new Vector3(-12.8f, 0, 112.3f);
                radius = 3f;
                moveEgress(youMoveHere, radius);
                break;
            default:
                Debug.LogWarning("Invalid egress phase!");
                break;
        }
        
        // Store the final egress state
        StoreCurrentPhaseState();
    }

    private void moveEgress(Vector3 move, float radius) {
        // Create a list to store already assigned positions
        List<Vector3> assignedPositions = new List<Vector3>();
        
        foreach(GameObject villain in villains) {
            // Try to find a non-overlapping position
            Vector3 targetPosition = GetNonOverlappingPosition(move, radius, assignedPositions);
            assignedPositions.Add(targetPosition); // this position has been used
            npcMove.MoveNPCToTarget(villain, targetPosition);
        }
    }

    private Vector3 GetNonOverlappingPosition(Vector3 center, float radius, List<Vector3> usedPositions) {
        float minDistanceBetweenNPCs = 1f;
        
        // Maximum attempts to find a suitable position
        int maxAttempts = 5;
        
        for (int attempt = 0; attempt < maxAttempts; attempt++) {
            // Get a random position within the radius
            Vector3 candidatePosition = GetRandomPointInRadius(center, radius);
            
            // Check if this position is far enough from all existing positions
            bool positionValid = true;
            foreach (Vector3 usedPos in usedPositions) {
                if (Vector3.Distance(candidatePosition, usedPos) < minDistanceBetweenNPCs) {
                    positionValid = false;
                    break;
                }
            }
            
            // If position is valid, return it
            if (positionValid) {
                return candidatePosition;
            }
        }
        
        // If we couldn't find a non-overlapping position after many attempts,
        // expand the search area slightly and try again
        if (usedPositions.Count > 0) {
            return GetNonOverlappingPosition(center, radius * 1.2f, usedPositions);
        }
        
        // Fallback - this should rarely happen
        return GetRandomPointInRadius(center, radius);
    }

    // Keep your original random point method
    private Vector3 GetRandomPointInRadius(Vector3 center, float radius) {
        float angle = Random.Range(0f, Mathf.PI * 2);
        float distance = Random.Range(0f, radius);

        float x = center.x + Mathf.Cos(angle) * distance;
        float z = center.z + Mathf.Sin(angle) * distance;

        return new Vector3(x, center.y, z);
    }
    
    private IEnumerator WorkOnGammaKnife(GameObject npc, Vector3 targetposition)
    {
        // Debug.Log("Started WorkOnGammaKnife Coroutine");

        while (Vector3.Distance(npc.transform.position, targetposition) > 1f)
        {
            // Debug.Log($"Waiting... Current Pos: {npc.transform.position}, Target: {targetposition}");
            yield return new WaitForSeconds(3f);
        }

        Debug.Log("The villain is working on the gamma knife source!");
        
        Animator animator = npc.GetComponent<Animator>();
        if (animator != null)
        {
            // Debug.Log("Animator found! Changing states.");
            animator.SetBool("IsWalking", false);
            animator.SetBool("ToRummaging", true);
        }
        else
        {
            Debug.LogError("Animator null for rummaging");
        }
    }


    public GamePhase GetCurrentPhase(){
        return phaseList.Current.Phase;
    }
}