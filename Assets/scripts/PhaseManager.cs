using UnityEngine;
using System.Collections.Generic;
using PhaseLink;
using System.Collections;

public class PhaseManager : MonoBehaviour
{
    private PhaseLinkedList phaseList;
    private PhaseMovementHelper npcMove;
    private playerController playerRole;
    private Coroutine currentPhaseCoroutine;
    private HostageTriggerArea hostageArea;
    
    // Reference to physician hostage for Phase 3
    private GameObject physicianHostage;
    
    // Reference to temporary gamma knife object
    private GameObject gammaKnifeObject;
    
    // References to different NPC groups
    private GameObject[] villainsInside;
    private GameObject[] villainsOutside;
    private GameObject superVillain;
    private GameObject receptionist;
    private int egress;
    private bool egressPhaseSelected = false;

    private void Start()
    {
        phaseList = new PhaseLinkedList();

        // Define the phases
        foreach (GamePhase phase in System.Enum.GetValues(typeof(GamePhase)))
        {
            phaseList.AddPhase(phase);
        }

        // Store initial positions and tags of all NPCs in the first phase node
        StoreInitialNPCState();
        
        // Find or create hostage trigger area
        SetupHostageArea();
        
        // Find key NPCs
        FindKeyNPCs();
        
        // Create temporary gamma knife object
        CreateTemporaryGammaKnife();

        phaseList.SetCurrentToHead();
        StartPhase();
    }

    private IEnumerator WaitForEgressSelection()
    {
        // Wait until the instructor selects an egress phase
        Debug.Log("Awaiting egress selection...");
        egressPhaseSelected = false;  // Ensure it's false when we start waiting

        while (!egressPhaseSelected)
        {
            egress = SetEgressPhase();
            if (egress != 0)  // If an egress phase is selected
            {
                egressPhaseSelected = true;  // Allow the game to continue
                Debug.Log($"Egress phase {egress} selected");
            }

            yield return null; // Wait until next frame to check again
        }
    }


    private int SetEgressPhase()
    {
        playerRole = FindObjectOfType<playerController>();
        if (playerRole.getPlayerRole() == playerController.Roles.Instructor)
        {
            if (Input.GetKeyDown(KeyCode.A)) return 1;
            if (Input.GetKeyDown(KeyCode.S)) return 2;
            if (Input.GetKeyDown(KeyCode.D)) return 3;
            if (Input.GetKeyDown(KeyCode.F)) return 4;
            if (Input.GetKeyDown(KeyCode.G)) return Random.Range(1, 5);  // Randomize between 1-4

            return 0;  // No valid key pressed, no selection made
        }
        else
        {
            Debug.Log("Only the instructor can select the egress phase.");
            return 0;  // No valid selection if not instructor
        }
    }


    
    private void FindKeyNPCs()
    {
        villainsInside = GameObject.FindGameObjectsWithTag("Villains");
        villainsOutside = GameObject.FindGameObjectsWithTag("OutsideVillains");
        superVillain = GameObject.FindGameObjectWithTag("SuperVillain");
        receptionist = GameObject.FindGameObjectWithTag("Receptionist");
        
        Debug.Log($"Found {villainsInside.Length} inside villains, {villainsOutside.Length} outside villains");
        if (superVillain != null) Debug.Log("Found SuperVillain");
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
        if (phaseList.Head == null)
            return;

        // Store civilians' initial state
        GameObject[] civilians = GameObject.FindGameObjectsWithTag("Civilians");
        foreach (GameObject civilian in civilians)
        {
            phaseList.Head.NPCPositions[civilian.name] = civilian.transform.position;
            phaseList.Head.NPCTags[civilian.name] = civilian.tag;
        }

        // Store medicals' initial state
        GameObject[] medicals = GameObject.FindGameObjectsWithTag("Medicals");
        foreach (GameObject medical in medicals)
        {
            phaseList.Head.NPCPositions[medical.name] = medical.transform.position;
            phaseList.Head.NPCTags[medical.name] = medical.tag;
        }

        // Store hostages' initial state
        GameObject[] hostages = GameObject.FindGameObjectsWithTag("Hostages");
        foreach (GameObject hostage in hostages)
        {
            phaseList.Head.NPCPositions[hostage.name] = hostage.transform.position;
            phaseList.Head.NPCTags[hostage.name] = hostage.tag;
        }

        // Debug.Log($"Stored initial state for {phaseList.Head.NPCPositions.Count} NPCs in Phase 1");
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
            // If we already have stored positions for the next phase, we'll use those
            return;
        }
        
        // Find all NPCs in the scene and store their current state
        GameObject[] allNPCs = GameObject.FindGameObjectsWithTag("Civilians");
        StoreNPCsInCurrentPhase(allNPCs);
        
        allNPCs = GameObject.FindGameObjectsWithTag("Medicals");
        StoreNPCsInCurrentPhase(allNPCs);
        
        allNPCs = GameObject.FindGameObjectsWithTag("Hostages");
        StoreNPCsInCurrentPhase(allNPCs);

        allNPCs = GameObject.FindGameObjectsWithTag("Villains");
        StoreNPCsInCurrentPhase(allNPCs);

        allNPCs = GameObject.FindGameObjectsWithTag("PhysicianHostage");
        StoreNPCsInCurrentPhase(allNPCs);
        
        Debug.Log($"Captured state for {phaseList.Current.NPCPositions.Count} NPCs in Phase {phaseList.Current.Phase}");
    }
    
    private void StoreNPCsInCurrentPhase(GameObject[] npcs)
    {
        foreach (GameObject npc in npcs)
        {
            phaseList.StoreNPCState(phaseList.Current, npc);
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
                // Re-enable if inactive
                if (!npc.activeInHierarchy)
                {
                    npc.SetActive(true);
                }
                
                // Reset tag to the one stored in the phase node
                if (node.NPCTags.ContainsKey(npc.name))
                {
                    npc.tag = node.NPCTags[npc.name];
                }
                
                ResetNPC(npc, node);
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
                animator.SetBool("IsHostage", npc.CompareTag("Hostages")); // Set hostage animation state based on tag
            }
            
            // Reset and enable AIMover
            AIMover mover = npc.GetComponent<AIMover>();
            if (mover != null)
            {
                mover.enabled = true;
                mover.SetTargetPosition(node.NPCPositions[npc.name]);
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
                // If coming back to Phase1, NPCs should already be reset to initial positions
                // by the RestoreNPCsFromPhaseNode method called in PreviousPhase()
                if (currentPhaseCoroutine != null) {
                    StopCoroutine(currentPhaseCoroutine);
                    currentPhaseCoroutine = null;
                }
                
                // Start random movement for civilians
                currentPhaseCoroutine = StartCoroutine(npcMove.MoveCiviliansRandomly(GetCurrentPhase()));
                break;
                
            case GamePhase.Phase2:
            // add alarm
            // add pull out gun
                if (currentPhaseCoroutine != null) {
                    StopCoroutine(currentPhaseCoroutine);
                    currentPhaseCoroutine = null;
                }
                
                // Villains pull out guns
                Debug.Log("Villains pull out long guns.");
                
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
                GameObject[] medicals = GameObject.FindGameObjectsWithTag("Medicals");
                if (medicals.Length > 0) {
                    physicianHostage = medicals[0]; // Take the first medical as hostage
                    physicianHostage.tag = "PhysicianHostage";
                    
                    Debug.Log($"The villains have taken {physicianHostage.name} hostage!");
                    
                    // Move the physician hostage to a villain
                    if (villainsInside != null && villainsInside.Length > 0) {
                        Vector3 hostagePosition = villainsInside[0].transform.position + new Vector3(1f, 0, 0);
                        npcMove.MoveNPCToTarget(physicianHostage, hostagePosition);
                    }
                } else {
                    // If no medicals are available, check for hostages that were originally medicals
                    GameObject[] hostages = GameObject.FindGameObjectsWithTag("Hostages");
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
                    NPCPosition = new Vector3(-14.3f, 0, 65.4f);
                    npcMove.MoveNPCToTarget(physicianHostage, NPCPosition);
                    
                    Debug.Log($"Two villains are taking {physicianHostage.name} to the gamma knife room");
                }
                
                // Outside villains move inside to reinforce
                if (villainsOutside != null) {
                    Vector3 lobbyPosition1 = new Vector3(2.3f, 0, 105.8f);
                    Vector3 lobbyPosition2 = new Vector3(5.8f, 0, 110.3f);
                    
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
                
                // Start the work on the gamma knife
                StartCoroutine(WorkOnGammaKnife());
                
                break;
            case GamePhase.Phase5:
                // law enforcement spawns in. 
                // tamper alarm goes off (dispatcher and those in the building can hear).
                // 3 baddies move to long hallway, cafeteria, and lobby - DONE
                // rad dose hemisphere is togglable by instructor.
                // Two villains take the physician hostage to the gamma knife room


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
                Vector3 youMoveHere = new Vector3(0f, 0, 113f);
                float radius = 3.5f;
                npcMove.MoveNPCToTarget(villainsInside[0], youMoveHere);
                for(int i=0; i<villainsInside.Length + villainsOutside.Length + 1; i++){
                    if(i==0){
                        npcMove.MoveNPCToTarget(physicianHostage, GetRandomPointInRadius(youMoveHere, radius));
                    } else if(i < 3){
                        npcMove.MoveNPCToTarget(villainsInside[i], GetRandomPointInRadius(youMoveHere, radius));
                    } else {
                        npcMove.MoveNPCToTarget(villainsOutside[i%2], GetRandomPointInRadius(youMoveHere, radius));
                    }
                }
                break;
            case GamePhase.Phase7:
                if (currentPhaseCoroutine != null) 
                {
                    StopCoroutine(currentPhaseCoroutine);
                    currentPhaseCoroutine = null;
                }
                currentPhaseCoroutine = StartCoroutine(WaitForEgressSelection());
                
                // Proceed with specific egress logic based on the selected phase
                switch(egress)
                {
                    case 1:
                        // Phase Egress 1: Adversaries move to the front emergency exit
                        Debug.Log("Phase Egress: " + egress);
                        break;
                    case 2:
                        // Phase Egress 2: Adversaries move to the rear emergency exit
                        Debug.Log("Phase Egress: " + egress);
                        break;
                    case 3:
                        // Phase Egress 3: Adversaries move to the lobby via the front entrance
                        Debug.Log("Phase Egress: " + egress);
                        break;
                    case 4:
                        // Phase Egress 4: Adversaries move to the rear exit
                        Debug.Log("Phase Egress: " + egress);
                        break;
                    default:
                        Debug.LogError("Invalid egress phase!");
                        break;
                }
                break;

        }
    }

    private Vector3 GetRandomPointInRadius(Vector3 center, float radius){
        float angle = Random.Range(0f, Mathf.PI * 2); // Random angle in radians
        float distance = Random.Range(0f, radius);    // Random distance within radius

        float x = center.x + Mathf.Cos(angle) * distance;
        float z = center.z + Mathf.Sin(angle) * distance;

        return new Vector3(x, center.y, z);
    }
    
    private IEnumerator WorkOnGammaKnife()
    {
        yield return new WaitForSeconds(3f); // Wait a bit for NPCs to reach positions
        Debug.Log("The villain is working on the gamma knife source!");
    }

    public GamePhase GetCurrentPhase(){
        return phaseList.Current.Phase;
    }
}