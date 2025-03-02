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
    private List<GameObject> villains;
    // private GameObject superVillain;
    private GameObject receptionist;
    private int egress;
    private bool egressPhaseSelected = false;
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

    private int SetEgressPhase()
    {
        // Debug.Log("Awaiting Egress Selection...");
        playerRole = FindObjectOfType<Player>();
        if(playerRole == null){
            Debug.LogError("playerRole null");
        }
        if (playerRole.getPlayerRole() == Player.Roles.Instructor)
        {
            Debug.Log("Instructor! Hi!!!");
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
                    NPCPosition = new Vector3(-14.3f, 0, 65.4f);
                    npcMove.MoveNPCToTarget(physicianHostage, NPCPosition);
                    
                    Debug.Log($"Two villains are taking {physicianHostage.name} to the gamma knife room");
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
                // currentPhaseCoroutine = StartCoroutine(WaitForEgressSelection());
                OnEgressSelected += ExecuteEgressPhase;
                break;

        }
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
    }

    private void moveEgress(Vector3 move, float radius){
        foreach(GameObject villain in villains){
            npcMove.MoveNPCToTarget(villain, GetRandomPointInRadius(move, radius));
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