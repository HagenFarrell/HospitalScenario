using UnityEngine;
using System.Collections.Generic;
using PhaseLink;
using System.Collections;

public class PhaseManager : MonoBehaviour
{
    private PhaseLinkedList phaseList;
    // private PhaseMovementHelper npcMove;
    private Player playerRole;
    private Coroutine currentPhaseCoroutine;
    // private HostageTriggerArea hostageArea;
    
    // Reference to temporary gamma knife object
    private GameObject gammaKnifeObject;
    
    // References to different NPC groups
    private GameObject[] villainsInside;
    private GameObject[] villainsOutside;
    private List<GameObject> playerUnits;
    private GameObject[] FD;
    private GameObject[] LLE;
    private List<GameObject> allNPCs;
    private GameObject[] newCivilians;
    private GameObject[] newHostages;
    private GameObject[] newMedicals;
    private GameObject physicianHostage;
    private List<GameObject> villains;
    // private GameObject superVillain;
    // private GameObject receptionist;
    private int egress;
    public delegate void EgressSelectedHandler(int egressPhase);
    public static event EgressSelectedHandler OnEgressSelected;

    public int currentPhase;

    private void Start()
    {
        currentPhase = 0;
        phaseList = new PhaseLinkedList();
        allNPCs = new List<GameObject>();

        // Define the phases
        foreach (GamePhase phase in System.Enum.GetValues(typeof(GamePhase)))
        {
            phaseList.AddPhase(phase);
        }

        // FD = GameObject.FindGameObjectsWithTag("FireDepartment");
        // LLE = GameObject.FindGameObjectsWithTag("LawEnforcement");
        // playerUnits = new List<GameObject>(FD);
        // playerUnits.AddRange(LLE);
        physicianHostage = GameObject.FindGameObjectsWithTag("PhysicianHostage")[0];
        newCivilians = GameObject.FindGameObjectsWithTag("Civilians");
        newHostages = GameObject.FindGameObjectsWithTag("Hostages");
        newMedicals = GameObject.FindGameObjectsWithTag("Medicals");

        allNPCs.Add(physicianHostage);
        allNPCs.AddRange(newHostages);
        allNPCs.AddRange(newCivilians);
        allNPCs.AddRange(newMedicals);

        // Store initial positions and tags of all NPCs in the first phase node
        StoreInitialNPCState();
        
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

    private IEnumerator WaitForGetUpAnimation(Animator animator, WaypointMover mover)
    {
        // Wait until the "GettingUp" animation is fully played
        while (animator.GetCurrentAnimatorStateInfo(0).IsName("Idle_Sitting") &&
            animator.GetCurrentAnimatorStateInfo(0).IsName("ToStand") &&
            animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
        {
            yield return null;
        }

        yield return new WaitForSeconds(0.1f);

        // Now set the running animation and allow movement
        animator.SetBool("IsRunning", true);

        // Activate waypoint movement
        mover.enabled = true;
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
        // receptionist = GameObject.FindGameObjectWithTag("Receptionist");
        
        villains = new List<GameObject>(villainsInside);
        villains.AddRange(villainsOutside);
        
        Debug.Log($"Found {villainsInside.Length} inside villains, {villainsOutside.Length} outside villains");
        // if (superVillain != null) Debug.Log("Found SuperVillain");
        // if (receptionist != null) Debug.Log("Found Receptionist");
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

    private void StoreInitialNPCState()
    {
        if (phaseList.Head == null)
            return;

        // Store all types of NPCs initial state
        StoreNPCTypeState("Civilians", phaseList.Head);
        // StoreNPCTypeState("Medicals", phaseList.Head);
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

    public void DespawnRemainingCiviliansAndMedicals()
    {
        // Find and disable any remaining civilians
        foreach (GameObject civilian in newCivilians)
        {
            Debug.Log($"Phase 3: Despawning civilian {civilian.name}");
            civilian.SetActive(false);
        }
        
        // Find and disable any remaining medicals
        foreach (GameObject medical in newMedicals)
        {
            Debug.Log($"Phase 3: Despawning medical {medical.name}");
            medical.SetActive(false);
        }
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

    // Phase 1 with new waypoint paths
    private void ExecutePhase1()
    {
        currentPhase = 1;
        Debug.Log("Executing Phase 1: NPCs begin waypoint movement");
        if (gammaKnifeObject != null) {
            gammaKnifeObject.SetActive(false);
        }
        
        // Hide gun
        villains[0].transform.GetChild(1).gameObject.SetActive(false);
        
        foreach (GameObject civilian in allNPCs)
        {
            Debug.Log("Enabling NPC: " + civilian);
            civilian.SetActive(true);
            // Set animation state to walking
            Animator animator = civilian.GetComponent<Animator>();
            
            // Enable the WaypointMover component
            WaypointMover mover = civilian.GetComponent<WaypointMover>();
            if (mover != null)
            {
                Debug.Log(mover.waypoints.waypointsActiveInPhase + " waypoints active for " + civilian);
                // Reset to first waypoint in the path
                if (mover.waypoints != null && mover.waypoints.transform.childCount > 0)
                {
                    Debug.Log("Resetting to initial waypoint");
                    mover.currentWaypoint = mover.waypoints.GetNextWaypoint(null);
                    mover.enabled = true;
                    mover.despawnAtLastWaypoint = false;
                }
                if(mover.waypoints.waypointsActiveInPhase == 1){
                    Debug.Log("Sitting down");
                    animator.SetBool("IsWalking", false);
                    animator.SetBool("IsRunning", false);
                    animator.SetBool("ToSitting", true);
                } else {
                    if (animator != null)
                    {
                        animator.SetBool("IsWalking", true);
                        animator.SetBool("IsRunning", false); // this is fine right
                    }
                }
            }
        }

        // Sets villains disc to green to make then try to blend in
        foreach(GameObject villain in villains) {
            GameObject disc = villain.transform.GetChild(2).gameObject;
            disc.SetActive(true);
            
            // Change the disc color to green
            Renderer discRenderer = disc.GetComponent<Renderer>();
            if (discRenderer != null) {
                discRenderer.material.color = Color.green;
            }
        }
    }

    private void ExecutePhase2(){
        currentPhase = 2;
        Debug.Log("executing the phase of tuah");
        Waypoints[] waypoint = FindObjectsOfType<Waypoints>();
        if(waypoint == null){
            Debug.LogError("waypoint object not found!");
        }
        foreach(Waypoints temp in waypoint){
            temp.isMovingForward = true;
            temp.canLoop = false;

            temp.enableAll();
        }

        // Configure all civilian WaypointMovers to despawn when they reach the last waypoint
        foreach (GameObject civilian in allNPCs) {
            WaypointMover mover = civilian.GetComponent<WaypointMover>();
            if (mover != null) {
                // Set up despawn behavior - we'll add a simple check to the component
                mover.despawnAtLastWaypoint = true;
                
                // Make sure animations are playing
                Animator animator = civilian.GetComponent<Animator>();
                StartCoroutine(WaitForGetUpAnimation(animator, mover));
            }
            
        }
        // Shows gun
        villains[0].transform.GetChild(1).gameObject.SetActive(true);

        // Sets villains disc to red to show they are bad
        foreach(GameObject villain in villains) {
            GameObject disc = villain.transform.GetChild(2).gameObject;
            
            // Change the disc color to green
            Renderer discRenderer = disc.GetComponent<Renderer>();
            if (discRenderer != null) {
                discRenderer.material.color = Color.red;
            }
        }
        
        
        // Receptionist hits duress alarm
        Debug.Log("Duress alarm activated. Dispatcher notified.");

    }

    private void ExecutePhase3(){
        Debug.Log($"The villains have taken {physicianHostage.name} hostage!");
        Animator animator = physicianHostage.GetComponent<Animator>();
        if(animator != null) animator.SetBool("IsThreatPresent", false);
    }

    private void ExecutePhase4(){
        // Activate the gamma knife object
        if (gammaKnifeObject != null) {
            gammaKnifeObject.SetActive(true);
        }

        WaypointMover mover = villains[0].GetComponent<WaypointMover>();
        mover.waypoints = GameObject.Find("Waypoints15")?.GetComponent<Waypoints>();
        mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0)); //this is how we get first waypoint externally

        Animator animator = mover.GetComponent<Animator>();
        // Change animation to walking
        if (animator != null)
        {
            animator.SetBool("IsRunning", true);
            mover.moveSpeed = 5;
        }

        mover = villains[1].GetComponent<WaypointMover>();
        mover.waypoints = GameObject.Find("Waypoints14")?.GetComponent<Waypoints>();
        mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));

        Animator animator1 = mover.GetComponent<Animator>();
        // Change animation to walking
        if (animator1 != null)
        {
            animator1.SetBool("IsRunning", true);
            mover.moveSpeed = 5;
        }

        mover = villains[3].GetComponent<WaypointMover>();
        mover.waypoints = GameObject.Find("Waypoints16")?.GetComponent<Waypoints>();
        mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));

        Animator animator2 = mover.GetComponent<Animator>();
        // Change animation to walking
        if (animator2 != null)
        {
            animator2.SetBool("IsRunning", true);
            mover.moveSpeed = 5;
        }

        mover = villains[4].GetComponent<WaypointMover>();
        mover.waypoints = GameObject.Find("Waypoints17")?.GetComponent<Waypoints>();
        mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));

        Animator animator3 = mover.GetComponent<Animator>();
        // Change animation to walking
        if (animator3 != null)
        {
            animator3.SetBool("IsRunning", true);
            mover.moveSpeed = 5;
        }

        // Doctor that gets taken hostage moves them to gamma knife
        mover = physicianHostage.GetComponent<WaypointMover>();

        Animator animator4 = physicianHostage.GetComponent<Animator>();
        // Change animation to walking
        if (animator4 != null)
        {
            animator4.SetBool("IsRunning", true);
            mover.moveSpeed = 5;
        }


        mover.waypoints = GameObject.Find("Waypoints18")?.GetComponent<Waypoints>();
        mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));


        
        // Two villains take the physician hostage to the gamma knife room
        if (physicianHostage != null && villainsInside != null && villainsInside.Length >= 2) {
            Debug.Log($"Two villains are taking {physicianHostage.name} to the gamma knife room");
            // // Start the work on the gamma knife
            // currentPhaseCoroutine = StartCoroutine(WorkOnGammaKnife(physicianHostage, NPCPosition));
        }
        
        // Last inside villain stays put
        if (villainsInside != null && villainsInside.Length >= 3) {
            // Do nothing with the third villain - they stay in place
            Debug.Log($"{villainsInside[2].name} stays behind in the lobby");
        }
        
    }

    private void ExecutePhase5(){

        WaypointMover mover = villains[2].GetComponent<WaypointMover>();
        mover.waypoints = GameObject.Find("Waypoints13")?.GetComponent<Waypoints>();
        mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));

        Animator animator1 = mover.GetComponent<Animator>();
        // Change animation to walking
        if (animator1 != null)
        {
            animator1.SetBool("IsRunning", true);
            mover.moveSpeed = 5;
        }

        mover = villains[3].GetComponent<WaypointMover>();
        mover.waypoints = GameObject.Find("Waypoints24")?.GetComponent<Waypoints>();
        mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));

        Animator animator2 = mover.GetComponent<Animator>();
        // Change animation to walking
        if (animator2 != null)
        {
            animator2.SetBool("IsRunning", true);
            mover.moveSpeed = 5;
        }

        mover = villains[4].GetComponent<WaypointMover>();
        mover.waypoints = GameObject.Find("Waypoints25")?.GetComponent<Waypoints>();
        mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));

        Animator animator3 = mover.GetComponent<Animator>();
        // Change animation to walking
        if (animator3 != null)
        {
            animator3.SetBool("IsRunning", true);
            mover.moveSpeed = 5;
        }

        Animator animator4 = physicianHostage.GetComponent<Animator>();
        // Change animation to walking
        if (animator4 != null)
        {
            animator4.SetBool("ToRummaging", false);
        }
    }

    private void ExecutePhase6(){
        WaypointMover mover = villains[0].GetComponent<WaypointMover>();
        mover.waypoints = GameObject.Find("Waypoints26")?.GetComponent<Waypoints>();
        mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0)); //this is how we get first waypoint externally

        Animator animator = mover.GetComponent<Animator>();
        // Change animation to walking
        if (animator != null)
        {
            animator.SetBool("IsRunning", true);
            mover.moveSpeed = 5;
        }

        mover = villains[1].GetComponent<WaypointMover>();
        mover.waypoints = GameObject.Find("Waypoints27")?.GetComponent<Waypoints>();
        mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));

        Animator animator1 = mover.GetComponent<Animator>();
        // Change animation to walking
        if (animator1 != null)
        {
            animator1.SetBool("IsRunning", true);
            mover.moveSpeed = 5;
        }

        mover = villains[2].GetComponent<WaypointMover>();
        mover.waypoints = GameObject.Find("Waypoints28")?.GetComponent<Waypoints>();
        mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));

        animator1 = mover.GetComponent<Animator>();
        // Change animation to walking
        if (animator1 != null)
        {
            animator1.SetBool("IsRunning", true);
            mover.moveSpeed = 5;
        }

        mover = villains[3].GetComponent<WaypointMover>();
        mover.waypoints = GameObject.Find("Waypoints29")?.GetComponent<Waypoints>();
        mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));

        Animator animator2 = mover.GetComponent<Animator>();
        // Change animation to walking
        if (animator2 != null)
        {
            animator2.SetBool("IsRunning", true);
            mover.moveSpeed = 5;
        }

        mover = villains[4].GetComponent<WaypointMover>();
        mover.waypoints = GameObject.Find("Waypoints30")?.GetComponent<Waypoints>();
        mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));

        Animator animator3 = mover.GetComponent<Animator>();
        // Change animation to walking
        if (animator3 != null)
        {
            animator3.SetBool("IsRunning", true);
            mover.moveSpeed = 5;
        }

        // physician hostage goes with them
        mover = physicianHostage.GetComponent<WaypointMover>();

        Animator animator4 = physicianHostage.GetComponent<Animator>();
        // Change animation to walking
        if (animator4 != null)
        {
            animator4.SetBool("IsRunning", true);
            mover.moveSpeed = 5;
        }
        mover.waypoints = GameObject.Find("Waypoints31")?.GetComponent<Waypoints>();
        mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));

        // Remaining hostages get rounded up
        mover = newHostages[0].GetComponent<WaypointMover>();

        animator4 = newHostages[0].GetComponent<Animator>();
        // Change animation to walking
        if (animator4 != null)
        {
            animator4.SetBool("IsRunning", true);
            animator4.SetBool("IsThreatPresent", false);
            mover.moveSpeed = 5;
        }
        mover.waypoints = GameObject.Find("Waypoints32")?.GetComponent<Waypoints>();
        mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));

        // Remaining hostages get rounded up
        mover = newHostages[1].GetComponent<WaypointMover>();

        animator4 = newHostages[1].GetComponent<Animator>();
        // Change animation to walking
        if (animator4 != null)
        {
            animator4.SetBool("IsRunning", true);
            animator4.SetBool("IsThreatPresent", false);
            mover.moveSpeed = 5;
        }
        mover.waypoints = GameObject.Find("Waypoints33")?.GetComponent<Waypoints>();
        mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));
    }

    private void ExecuteEgressPhase(int selectedEgress)
    {
        OnEgressSelected -= ExecuteEgressPhase; // Unsubscribe to prevent multiple calls

        Debug.Log($"Egress phase {selectedEgress} selected!");
        egress = selectedEgress;

        switch(egress) // b = random
        {
            case 1: // z
                // Phase Egress 1: Adversaries move to the front emergency exit
                Debug.Log("Phase Egress: " + egress);
                
                WaypointMover mover = villains[0].GetComponent<WaypointMover>();
                mover.waypoints = GameObject.Find("Waypoints34")?.GetComponent<Waypoints>();
                mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0)); //this is how we get first waypoint externally

                Animator animator = mover.GetComponent<Animator>();
                // Change animation to walking
                if (animator != null)
                {
                    animator.SetBool("IsRunning", true);
                    mover.moveSpeed = 5;
                }

                mover = villains[1].GetComponent<WaypointMover>();
                mover.waypoints = GameObject.Find("Waypoints35")?.GetComponent<Waypoints>();
                mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));

                Animator animator1 = mover.GetComponent<Animator>();
                // Change animation to walking
                if (animator1 != null)
                {
                    animator1.SetBool("IsRunning", true);
                    mover.moveSpeed = 5;
                }

                mover = villains[2].GetComponent<WaypointMover>();
                mover.waypoints = GameObject.Find("Waypoints36")?.GetComponent<Waypoints>();
                mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));

                animator1 = mover.GetComponent<Animator>();
                // Change animation to walking
                if (animator1 != null)
                {
                    animator1.SetBool("IsRunning", true);
                    mover.moveSpeed = 5;
                }

                mover = villains[3].GetComponent<WaypointMover>();
                mover.waypoints = GameObject.Find("Waypoints37")?.GetComponent<Waypoints>();
                mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));

                Animator animator2 = mover.GetComponent<Animator>();
                // Change animation to walking
                if (animator2 != null)
                {
                    animator2.SetBool("IsRunning", true);
                    mover.moveSpeed = 5;
                }

                mover = villains[4].GetComponent<WaypointMover>();
                mover.waypoints = GameObject.Find("Waypoints38")?.GetComponent<Waypoints>();
                mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));

                Animator animator3 = mover.GetComponent<Animator>();
                // Change animation to walking
                if (animator3 != null)
                {
                    animator3.SetBool("IsRunning", true);
                    mover.moveSpeed = 5;
                }

                // physician hostage goes with them
                mover = physicianHostage.GetComponent<WaypointMover>();

                Animator animator4 = physicianHostage.GetComponent<Animator>();
                // Change animation to walking
                if (animator4 != null)
                {
                    animator4.SetBool("IsRunning", true);
                    mover.moveSpeed = 5;
                }
                mover.waypoints = GameObject.Find("Waypoints39")?.GetComponent<Waypoints>();
                mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));

                // Remaining hostages get rounded up
                mover = newHostages[0].GetComponent<WaypointMover>();

                animator4 = newHostages[0].GetComponent<Animator>();
                // Change animation to walking
                if (animator4 != null)
                {
                    animator4.SetBool("IsRunning", true);
                    animator4.SetBool("IsThreatPresent", false);
                    mover.moveSpeed = 5;
                }
                mover.waypoints = GameObject.Find("Waypoints40")?.GetComponent<Waypoints>();
                mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));

                // Remaining hostages get rounded up
                mover = newHostages[1].GetComponent<WaypointMover>();

                animator4 = newHostages[1].GetComponent<Animator>();
                // Change animation to walking
                if (animator4 != null)
                {
                    animator4.SetBool("IsRunning", true);
                    animator4.SetBool("IsThreatPresent", false);
                    mover.moveSpeed = 5;
                }
                mover.waypoints = GameObject.Find("Waypoints41")?.GetComponent<Waypoints>();
                mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));


                break;
            case 2: // x
                // Phase Egress 2: Adversaries move to the rear emergency exit
                Debug.Log("Phase Egress: " + egress);
                
                mover = villains[0].GetComponent<WaypointMover>();
                mover.waypoints = GameObject.Find("Waypoints42")?.GetComponent<Waypoints>();
                mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0)); //this is how we get first waypoint externally

                animator = mover.GetComponent<Animator>();
                // Change animation to walking
                if (animator != null)
                {
                    animator.SetBool("IsRunning", true);
                    mover.moveSpeed = 5;
                }

                mover = villains[1].GetComponent<WaypointMover>();
                mover.waypoints = GameObject.Find("Waypoints43")?.GetComponent<Waypoints>();
                mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));

                animator1 = mover.GetComponent<Animator>();
                // Change animation to walking
                if (animator1 != null)
                {
                    animator1.SetBool("IsRunning", true);
                    mover.moveSpeed = 5;
                }

                mover = villains[2].GetComponent<WaypointMover>();
                mover.waypoints = GameObject.Find("Waypoints44")?.GetComponent<Waypoints>();
                mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));

                animator1 = mover.GetComponent<Animator>();
                // Change animation to walking
                if (animator1 != null)
                {
                    animator1.SetBool("IsRunning", true);
                    mover.moveSpeed = 5;
                }

                mover = villains[3].GetComponent<WaypointMover>();
                mover.waypoints = GameObject.Find("Waypoints45")?.GetComponent<Waypoints>();
                mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));

                animator2 = mover.GetComponent<Animator>();
                // Change animation to walking
                if (animator2 != null)
                {
                    animator2.SetBool("IsRunning", true);
                    mover.moveSpeed = 5;
                }

                mover = villains[4].GetComponent<WaypointMover>();
                mover.waypoints = GameObject.Find("Waypoints46")?.GetComponent<Waypoints>();
                mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));

                animator3 = mover.GetComponent<Animator>();
                // Change animation to walking
                if (animator3 != null)
                {
                    animator3.SetBool("IsRunning", true);
                    mover.moveSpeed = 5;
                }

                // physician hostage goes with them
                mover = physicianHostage.GetComponent<WaypointMover>();

                animator4 = physicianHostage.GetComponent<Animator>();
                // Change animation to walking
                if (animator4 != null)
                {
                    animator4.SetBool("IsRunning", true);
                    mover.moveSpeed = 5;
                }
                mover.waypoints = GameObject.Find("Waypoints47")?.GetComponent<Waypoints>();
                mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));

                // Remaining hostages get rounded up
                mover = newHostages[0].GetComponent<WaypointMover>();

                animator4 = newHostages[0].GetComponent<Animator>();
                // Change animation to walking
                if (animator4 != null)
                {
                    animator4.SetBool("IsRunning", true);
                    animator4.SetBool("IsThreatPresent", false);
                    mover.moveSpeed = 5;
                }
                mover.waypoints = GameObject.Find("Waypoints48")?.GetComponent<Waypoints>();
                mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));

                // Remaining hostages get rounded up
                mover = newHostages[1].GetComponent<WaypointMover>();

                animator4 = newHostages[1].GetComponent<Animator>();
                // Change animation to walking
                if (animator4 != null)
                {
                    animator4.SetBool("IsRunning", true);
                    animator4.SetBool("IsThreatPresent", false);
                    mover.moveSpeed = 5;
                }
                mover.waypoints = GameObject.Find("Waypoints49")?.GetComponent<Waypoints>();
                mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));


                break;
            case 3: // c
                // Phase Egress 3: Adversaries move to the lobby exit
                Debug.Log("Phase Egress: " + egress);
                // youMoveHere = new Vector3(20.8f, 0, 113.3f);
                // radius = 3f;
                // moveEgress(youMoveHere, radius);
                break;
            case 4: // v
                // Phase Egress 4: Adversaries move to the rear exit
                Debug.Log("Phase Egress: " + egress);
                // youMoveHere = new Vector3(-12.8f, 0, 112.3f);
                // radius = 3f;
                // moveEgress(youMoveHere, radius);
                break;
            default:
                Debug.LogWarning("Invalid egress phase!");
                break;
        }
    }

    private void MoveNPCsForPhase(GamePhase phase)
    {
        Debug.Log($"Moving NPCs for phase: {phase}");
        
        // npcMove = FindObjectOfType<PhaseMovementHelper>();
        
        switch (phase)
        {
            case GamePhase.Phase1:
                // If coming back to Phase1, NPCs should already be reset to initial positions
                ExecutePhase1();
                break;
                
            case GamePhase.Phase2:
                ExecutePhase2();
                break;
                
            case GamePhase.Phase3:
                if (currentPhaseCoroutine != null) {
                    StopCoroutine(currentPhaseCoroutine);
                    currentPhaseCoroutine = null;
                }
                ExecutePhase3();
                break;
                
            case GamePhase.Phase4:
            // add animation for tampering with machine
                if (currentPhaseCoroutine != null) {
                    StopCoroutine(currentPhaseCoroutine);
                    currentPhaseCoroutine = null;
                }
                ExecutePhase4();
                
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
                ExecutePhase5();

                break;
            case GamePhase.Phase6:
                // // VFD pulls up
                // // source goes into canister into backpack
                // // all adversaries and physicianhostage group up & get ready to leave
                ExecutePhase6();
                
                break;
            case GamePhase.Phase7:
                SetEgressPhase();

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

    public GamePhase GetCurrentPhase(){
        return phaseList.Current.Phase;
    }
}