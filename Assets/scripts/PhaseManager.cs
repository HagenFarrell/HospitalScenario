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
    public GameObject gammaKnifeObject;
    
    // References to different NPC groups
    private GameObject[] villainsInside;
    private GameObject[] villainsOutside;
    private List<GameObject> playerUnits;
    private GameObject[] FD;
    private GameObject[] LLE;
    private List<GameObject> allCivilians;
    private GameObject[] newCivilians;
    private GameObject[] newHostages;
    private GameObject[] newMedicals;
    private GameObject physicianHostage;
    private List<GameObject> allVillains;
    private List<GameObject> allNPCs;
    // private GameObject pathObject;
    // private bool sameOld;
    private int egress;
    private bool reverting;
    public delegate void EgressSelectedHandler(int egressPhase);
    public static event EgressSelectedHandler OnEgressSelected;

    // public int currentPhase;

    private void Start()
    {
        // currentPhase = 1;
        // sameOld = false;
        phaseList = new PhaseLinkedList();
        allCivilians = new List<GameObject>();
        allVillains = new List<GameObject>();
        allNPCs = new List<GameObject>();
        FindKeyNPCs();

        // Define the phases
        foreach (GamePhase phase in System.Enum.GetValues(typeof(GamePhase)))
        {
            phaseList.AddPhase(phase);
        }
        

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
        if(Input.GetKeyDown(KeyCode.Alpha8)){
            Debug.LogWarning("resetting the current phase doesnt really work rn...");
            // ResetCurrent();
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

        physicianHostage.transform.rotation = new Quaternion(0f, 0f, 0f, 1f);

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
        // Debug.Log($"TriggerEgressSelected called with phase {phase}");

        if (OnEgressSelected != null)
        {
            // Debug.Log("Triggering OnEgressSelected event");
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
        // find villains
        villainsInside = GameObject.FindGameObjectsWithTag("Villains");
        villainsOutside = GameObject.FindGameObjectsWithTag("OutsideVillains");
        allVillains = new List<GameObject>(villainsInside);
        allVillains.AddRange(villainsOutside);
        allNPCs.AddRange(allVillains);

        // find player characters
        FD = GameObject.FindGameObjectsWithTag("FireDepartment");
        LLE = GameObject.FindGameObjectsWithTag("LawEnforcement");
        playerUnits = new List<GameObject>(FD);
        playerUnits.AddRange(LLE);

        // find civilians
        physicianHostage = GameObject.FindGameObjectsWithTag("PhysicianHostage")[0];
        newCivilians = GameObject.FindGameObjectsWithTag("Civilians");
        newHostages = GameObject.FindGameObjectsWithTag("Hostages");
        newMedicals = GameObject.FindGameObjectsWithTag("Medicals");

        if (physicianHostage != null) allCivilians.Add(physicianHostage);
        if (newHostages != null) allCivilians.AddRange(newHostages);
        if (newCivilians != null) allCivilians.AddRange(newCivilians);
        if (newMedicals != null) allCivilians.AddRange(newMedicals);

        allNPCs.AddRange(allCivilians);
    }
    
    private void StartPhase()
    {
        Debug.Log($"Entering Phase: {phaseList.Current.Phase}");
        
        // First, check if we need to despawn civilians and medicals in Phase 3
        if (phaseList.Current.Phase == GamePhase.Phase3)
        {
            despawnLeftovers();
        }
        
        MoveNPCsForPhase(phaseList.Current.Phase);
    }

    public void despawnLeftovers()
    {
        // Find and disable any remaining civilians
        foreach (GameObject civilian in newCivilians)
        {
            // Debug.Log($"Phase 3: Despawning civilian {civilian.name}");
            civilian.SetActive(false);
        }
        
        // Find and disable any remaining medicals
        foreach (GameObject medical in newMedicals)
        {
            // Debug.Log($"Phase 3: Despawning medical {medical.name}");
            medical.SetActive(false);
        }
    }

    public void NextPhase()
    {
        reverting = false;
        // Stop any ongoing coroutines from the current phase
        if (currentPhaseCoroutine != null)
        {
            StopCoroutine(currentPhaseCoroutine);
            currentPhaseCoroutine = null;
        }

        if (phaseList.MoveNext())
        {
            ResetForward(phaseList.Current.Phase);
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
        reverting = true;
        // Stop any ongoing coroutines
        if (currentPhaseCoroutine != null)
        {
            StopCoroutine(currentPhaseCoroutine);
            currentPhaseCoroutine = null;
        }
        if (phaseList.MovePrevious())
        {
            if(OnEgressSelected == null) OnEgressSelected += ExecuteEgressPhase;
            ResetBackwards();
            Debug.Log("Moving to previous phase.");
            StartPhase();
        }
        else
        {
            Debug.Log("Already at the first phase!");
        }
    }

    private GameObject getRadSource(){
        return allVillains[0].transform.GetChild(4).gameObject;
    }

    // Phase 1 with new waypoint paths
    private void ExecutePhase1()
    {
        // Debug.LogError("executing phase numero uno");
        if(playerUnits == null || playerUnits.Count == 0){
            GameObject temp = GameObject.Find("Player Units");
            temp.transform.position = new Vector3(temp.transform.position.x, temp.transform.position.y-9000f, temp.transform.position.z);
        }
        else foreach(GameObject unit in playerUnits){
            Debug.Log("moving down " + unit);
            unit.transform.position = new Vector3(unit.transform.position.x, unit.transform.position.y-9000f, unit.transform.position.z);
        }
        
        // currentPhase = 1;
        // Debug.Log("Executing Phase 1: NPCs begin waypoint movement");
        gammaKnifeObject = getRadSource();
        if (gammaKnifeObject != null) {
            gammaKnifeObject.SetActive(false);
            // gammaKnifeObject.transform.GetChild(0).localScale = new Vector3(1.125f, 1.125f, 1.125f);
        }
        
        // Hide gun
        allVillains[0].transform.GetChild(1).gameObject.SetActive(false);
        
        foreach (GameObject civilian in allCivilians)
        {
            // Debug.Log("Enabling NPC: " + civilian);
            civilian.SetActive(true);
            // Set animation state to walking
            Animator animator = civilian.GetComponent<Animator>();
            
            // Enable the WaypointMover component
            WaypointMover mover = civilian.GetComponent<WaypointMover>();

            if (mover != null)
            {
                // Debug.Log(mover.waypoints.waypointsActiveInPhase + " waypoints active for " + civilian);
                // Reset to first waypoint in the path
                if (mover.waypoints != null && mover.waypoints.transform.childCount > 0)
                {
                    // Debug.Log("Resetting to initial waypoint");
                    mover.currentWaypoint = mover.waypoints.GetNextWaypoint(null);
                    mover.enabled = true;
                    mover.despawnAtLastWaypoint = false;
                }
                if(mover.waypoints.waypointsActiveInPhase == 1){
                    // Debug.Log("Sitting down");
                    animator.SetBool("IsWalking", false);
                    animator.SetBool("IsRunning", false);
                    animator.SetBool("ToSitting", true);
                } else {
                    // Debug.Log("going for a walk");
                    if (animator != null)
                    {
                        // Debug.Log("Animator NOT null for " + civilian);
                        animator.SetBool("IsWalking", true);
                        animator.SetBool("IsRunning", false); // this is fine right
                    } else Debug.LogError("Animator null for " + civilian);
                }
            }
        }

        // Sets allVillains disc to green to make then try to blend in
        foreach(GameObject villain in allVillains) {
            WaypointMover mover = villain.GetComponent<WaypointMover>();
            // if(reverting) mover.waypointStorage.Push(mover.waypoints);
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
        // currentPhase = 2;
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

        foreach (GameObject hostage in newHostages)
        {
            // Change disk color to yellow at last waypoint
            GameObject disc = hostage.transform.GetChild(2).gameObject;
            Renderer discRenderer = disc.GetComponent<Renderer>();

            if (discRenderer != null && !gameObject.CompareTag("PhysicianHostage")) {
                discRenderer.material.color = Color.yellow;
                // Debug.Log($"Changed {gameObject.name} disc to yellow at last waypoint");
            } 
        }

        GameObject disc1 = physicianHostage.transform.GetChild(2).gameObject;
        Renderer discRenderer1 = disc1.GetComponent<Renderer>();
        discRenderer1.material.color = new Color(1f, 0.5f, 0f);



        // Configure all civilian WaypointMovers to despawn when they reach the last waypoint
        foreach (GameObject civilian in allCivilians) {
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
        allVillains[0].transform.GetChild(1).gameObject.SetActive(true);

        // Sets allVillains disc to red to show they are bad
        foreach(GameObject villain in allVillains) {
            GameObject disc = villain.transform.GetChild(2).gameObject;
            
            // Change the disc color to green
            Renderer discRenderer = disc.GetComponent<Renderer>();
            if (discRenderer != null) {
                discRenderer.material.color = Color.red;
            }
        }
        
        
        // Receptionist hits duress alarm
        Debug.Log("Duress alarm activated. Dispatcher notified.");
        physicianHostage.transform.rotation = new Quaternion(0f, 0f, 0f, 1f);

    }

    private void ExecutePhase3(){
        
        // Reset physicianHostage animator from bugging out
        Animator animator = physicianHostage.GetComponent<Animator>();
        if(animator != null) {
            animator.Rebind();  // This completely resets the animator state machine
            animator.Update(0f); // This forces an immediate update
            
            // THEN set all animation parameters explicitly to known states
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsRunning", false);
            animator.SetBool("ToSitting", false);
            
            foreach(GameObject hostage in newHostages){
                animator = hostage.GetComponent<Animator>();
                animator.Rebind();  // This completely resets the animator state machine
                animator.Update(0f); // This forces an immediate update
                
                // THEN set all animation parameters explicitly to known states
                animator.SetBool("IsWalking", false);
                animator.SetBool("IsRunning", false);
                animator.SetBool("ToSitting", false);
                animator.SetBool("IsThreatPresent", true);
            }
        }
        
        // THEN set position and rotation (order matters)
        physicianHostage.transform.rotation = Quaternion.identity;
        Debug.Log($"The allVillains have taken {physicianHostage.name} hostage!");
        if(animator != null) animator.SetBool("IsThreatPresent", false);
    }

    private void ExecutePhase4(){
        WaypointMover mover = allVillains[0].GetComponent<WaypointMover>();
        mover.waypoints = GameObject.Find("Waypoints15")?.GetComponent<Waypoints>();
       if(!reverting) mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0)); //this is how we get first waypoint externally
        // if(reverting) mover.waypointStorage.Push(mover.waypoints);

        Animator animator = mover.GetComponent<Animator>();
        // Change animation to walking
        if (animator != null)
        {
            animator.SetBool("IsRunning", true);
            mover.moveSpeed = 5;
        }

        mover = allVillains[1].GetComponent<WaypointMover>();
        mover.waypoints = GameObject.Find("Waypoints14")?.GetComponent<Waypoints>();
       if(!reverting) mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));
        // if(reverting) mover.waypointStorage.Push(mover.waypoints);

        Animator animator1 = mover.GetComponent<Animator>();
        // Change animation to walking
        if (animator1 != null)
        {
            animator1.SetBool("IsRunning", true);
            mover.moveSpeed = 5;
        }

        mover = allVillains[3].GetComponent<WaypointMover>();
        mover.waypoints = GameObject.Find("Waypoints16")?.GetComponent<Waypoints>();
       if(!reverting) mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));
        // if(reverting) mover.waypointStorage.Push(mover.waypoints);

        Animator animator2 = mover.GetComponent<Animator>();
        // Change animation to walking
        if (animator2 != null)
        {
            animator2.SetBool("IsRunning", true);
            mover.moveSpeed = 5;
        }

        mover = allVillains[4].GetComponent<WaypointMover>();
        mover.waypoints = GameObject.Find("Waypoints17")?.GetComponent<Waypoints>();
       if(!reverting) mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));
        // if(reverting) mover.waypointStorage.Push(mover.waypoints);

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
       if(!reverting) mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));
        // if(reverting) mover.waypointStorage.Push(mover.waypoints);

        // Debug.Log($"Two Villains are taking {physicianHostage.name} to the gamma knife room");
        // Debug.Log($"{villainsInside[2].name} stays behind in the lobby");
        
    }

    private void ExecutePhase5(){
        if (gammaKnifeObject != null) {
            Debug.Log("gammaknifeobject SPAWNING!!!!");
            gammaKnifeObject.SetActive(true);
            gammaKnifeObject.transform.GetChild(0).gameObject.SetActive(true);
            // gammaKnifeObject.transform.localScale = new Vector3(25f, 25f, 25f);
        }

        if(LLE == null || LLE.Length == 0) LLE = GameObject.FindGameObjectsWithTag("LawEnforcement");
        foreach(GameObject unit in LLE){
            unit.transform.position = new Vector3(unit.transform.position.x, unit.transform.position.y+9000f, unit.transform.position.z);
        }


        WaypointMover mover = allVillains[2].GetComponent<WaypointMover>();
        mover.waypoints = GameObject.Find("Waypoints13")?.GetComponent<Waypoints>();
       if(!reverting) mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));
        // if(reverting) mover.waypointStorage.Push(mover.waypoints);

        Animator animator1 = mover.GetComponent<Animator>();
        // Change animation
        if (animator1 != null)
        {
            animator1.SetBool("IsRunning", true);
            mover.moveSpeed = 5;
        }

        mover = allVillains[3].GetComponent<WaypointMover>();
        mover.waypoints = GameObject.Find("Waypoints24")?.GetComponent<Waypoints>();
       if(!reverting) mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));
        // if(reverting) mover.waypointStorage.Push(mover.waypoints);

        Animator animator2 = mover.GetComponent<Animator>();
        // Change animation 
        if (animator2 != null)
        {
            animator2.SetBool("IsRunning", true);
            mover.moveSpeed = 5;
        }

        mover = allVillains[4].GetComponent<WaypointMover>();
        mover.waypoints = GameObject.Find("Waypoints25")?.GetComponent<Waypoints>();
       if(!reverting) mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));
        // if(reverting) mover.waypointStorage.Push(mover.waypoints);

        Animator animator3 = mover.GetComponent<Animator>();
        // Change animation 
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
        if (FD == null || FD.Length == 0) FD = GameObject.FindGameObjectsWithTag("FireDepartment");
        foreach(GameObject unit in FD){
            unit.transform.position = new Vector3(unit.transform.position.x, unit.transform.position.y+9000f, unit.transform.position.z);
        }

        WaypointMover mover = allVillains[0].GetComponent<WaypointMover>();
        mover.waypoints = GameObject.Find("Waypoints26")?.GetComponent<Waypoints>();
       if(!reverting) mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));
        // if(reverting) mover.waypointStorage.Push(mover.waypoints);

        Animator animator = mover.GetComponent<Animator>();
        // Change animation to walking
        if (animator != null)
        {
            animator.SetBool("IsRunning", true);
            mover.moveSpeed = 5;
        }

        mover = allVillains[1].GetComponent<WaypointMover>();
        mover.waypoints = GameObject.Find("Waypoints27")?.GetComponent<Waypoints>();
       if(!reverting) mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));
        // if(reverting) mover.waypointStorage.Push(mover.waypoints);

        Animator animator1 = mover.GetComponent<Animator>();
        // Change animation to walking
        if (animator1 != null)
        {
            animator1.SetBool("IsRunning", true);
            mover.moveSpeed = 5;
        }

        mover = allVillains[2].GetComponent<WaypointMover>();
        mover.waypoints = GameObject.Find("Waypoints28")?.GetComponent<Waypoints>();
       if(!reverting) mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));
        // if(reverting) mover.waypointStorage.Push(mover.waypoints);

        animator1 = mover.GetComponent<Animator>();
        // Change animation to walking
        if (animator1 != null)
        {
            animator1.SetBool("IsRunning", true);
            mover.moveSpeed = 5;
        }

        mover = allVillains[3].GetComponent<WaypointMover>();
        mover.waypoints = GameObject.Find("Waypoints29")?.GetComponent<Waypoints>();
       if(!reverting) mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));
        // if(reverting) mover.waypointStorage.Push(mover.waypoints);

        Animator animator2 = mover.GetComponent<Animator>();
        // Change animation to walking
        if (animator2 != null)
        {
            animator2.SetBool("IsRunning", true);
            mover.moveSpeed = 5;
        }

        mover = allVillains[4].GetComponent<WaypointMover>();
        mover.waypoints = GameObject.Find("Waypoints30")?.GetComponent<Waypoints>();
       if(!reverting) mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));
        // if(reverting) mover.waypointStorage.Push(mover.waypoints);

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
       if(!reverting) mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));
        // if(reverting) mover.waypointStorage.Push(mover.waypoints);

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
       if(!reverting) mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));
        // if(reverting) mover.waypointStorage.Push(mover.waypoints);

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
       if(!reverting) mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));
        // if(reverting) mover.waypointStorage.Push(mover.waypoints);
    }

    private void ExecuteEgressPhase(int selectedEgress)
    {
        if(OnEgressSelected == null) return;
        OnEgressSelected -= ExecuteEgressPhase; // Unsubscribe to prevent multiple calls

        Debug.Log($"Egress phase {selectedEgress} selected!");
        egress = selectedEgress;

        foreach (GameObject npc in allNPCs)
        {
            if (!npc.activeSelf) continue;

            WaypointMover mover = npc.GetComponent<WaypointMover>();
            if (mover == null || mover.paths == null || mover.waypoints == null)
            {
                Debug.LogWarning($"NPC {npc.name} has missing WaypointMover or path references.");
                resetAnimator(npc);
                continue;
            }
            if(mover.waypoints.ActiveChildLength < 2) continue;

            Transform path = mover.paths.transform.GetChild(mover.paths.transform.childCount-1);
            Waypoints waypoints = path.transform.GetChild(egress-1).GetComponent<Waypoints>();
            if(waypoints == null){
                Debug.LogWarning("Waypoints null, so " + npc + " wont update their paths");
                continue;
            }else {
                mover.waypoints = waypoints;
                mover.currentWaypoint = waypoints.GetNextWaypoint(waypoints.transform.GetChild(0));
                mover.pathidx = mover.paths.transform.childCount-1;

                Animator animator = mover.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.SetBool("IsRunning", true);
                    mover.moveSpeed = 5;
                }
            } 
        }
    }


    private void ResetForward(GamePhase phase)
    {
        if (phase != GamePhase.Phase1 && phase != GamePhase.Phase2)
        {
            foreach (GameObject npc in allNPCs)
            {
                if(npc.activeSelf){
                    npc.transform.rotation = new Quaternion(0f, 0f, 0f, 1f);
                    WaypointMover mover = npc.GetComponent<WaypointMover>();
                    if (mover != null && mover.waypoints != null)
                    {
                        Transform lastWaypoint = mover.waypoints.transform.GetChild(mover.waypoints.transform.childCount - 1);
                        // if(reverting) mover.waypointStorage.Push(mover.waypoints);
                        
                        mover.currentWaypoint = lastWaypoint;
                        npc.transform.position = mover.currentWaypoint.position;
                        
                        // Reset animations when teleporting
                        Animator animator = npc.GetComponent<Animator>();
                        if (animator != null)
                        {
                            animator.SetBool("IsWalking", false);
                            animator.SetBool("IsRunning", false);
                            animator.SetBool("ToSitting", false);
                            animator.SetBool("IsThreatPresent", false);

                            // Reset crotching for hostages
                            if ((phase == GamePhase.Phase3 || phase == GamePhase.Phase4 || phase == GamePhase.Phase5) && npc.CompareTag("Hostages"))
                            {
                                animator.SetBool("IsThreatPresent", true);
                            }
                        }
                    }
                }
            }
        }
    }

    private void SaveWaypointState()
    {
        foreach (GameObject npc in allNPCs)
        {
            if (npc.activeSelf)
            {
                WaypointMover mover = npc.GetComponent<WaypointMover>();
                if (mover != null && mover.waypoints != null)
                {
                    if(mover.waypointStorage == null){
                        // Debug.LogError("Waypointstorage null??");
                        mover.waypointStorage = new Stack<WaypointState>();
                    }
                    Animator animator = npc.GetComponent<Animator>();
                    bool isWalking = animator != null && animator.GetBool("IsWalking");
                    bool isRunning = animator != null && animator.GetBool("IsRunning");
                    bool isSitting = animator != null && animator.GetBool("ToSitting");
                    bool old = false;
                    if(mover.waypointStorage.Count > 0) old = mover.waypoints == mover.waypointStorage.Peek().waypoints;
                    if(GetCurrentPhase() == GamePhase.Phase5) old = true; //bandaid fix, to be changed
                    var state = new WaypointState(
                        mover.waypoints,
                        mover.waypoints.ActiveChildLength,
                        mover.waypoints.isMovingForward,
                        mover.waypoints.canLoop,
                        isWalking,
                        isRunning,
                        isSitting,
                        old
                    );
                    
                    mover.waypointStorage.Push(state);
                }
            }
        }
    }

    private void ResetCurrent(){
        foreach(GameObject npc in allNPCs){
            if (npc.activeSelf)
            {
                npc.transform.rotation = Quaternion.identity;
                WaypointMover mover = npc.GetComponent<WaypointMover>();
                if (mover != null && mover.waypoints != null && mover.waypointStorage.Count > 0)
                {
                    var state = mover.waypointStorage.Peek();
                    // Debug.Log("state waypoints: " + state.waypoints);
                    // Debug.Log("national waypoints: " + mover.waypoints);
                    mover.waypoints = state.waypoints;
                    // Debug.Log("child length state: " + state.activeChildLength);
                    // Debug.Log("child length current: " + mover.waypoints.ActiveChildLength);
                    mover.waypoints.ActiveChildLength = state.activeChildLength;
                    
                    mover.waypoints.isMovingForward = state.isMovingForward;
                    mover.waypoints.canLoop = state.canLoop;
                    if(mover.waypointStorage.Peek().sameOld){
                        Transform LastWaypoint = mover.waypoints.transform.GetChild(mover.waypoints.transform.childCount - 1);
                        mover.currentWaypoint = LastWaypoint;
                        npc.transform.position = LastWaypoint.position;
                    }
                    else {
                        Transform firstWaypoint = mover.waypoints.transform.GetChild(0);
                        mover.currentWaypoint = firstWaypoint;
                        npc.transform.position = firstWaypoint.position;
                    }

                    // Restore animation states
                    Animator animator = npc.GetComponent<Animator>();
                    if (animator != null)
                    {
                        animator.SetBool("IsWalking", state.isWalking);
                        animator.SetBool("IsRunning", state.isRunning);
                        animator.SetBool("ToSitting", state.isSitting);
                    }
                }
            }
        }
    }

    private void SaveAnimationState()
    {
        // Debug.Log("ayup ONE!!!!!--------");
        foreach (GameObject npc in allNPCs)
        {
            // Debug.Log("ayup 2");
            if (npc.activeSelf)
            {
                // Debug.LogWarning(GetCurrentPhase());
                WaypointMover mover = npc.GetComponent<WaypointMover>();
                if (mover != null && mover.waypoints != null)
                {
                    Animator animator = npc.GetComponent<Animator>();
                    bool isWalking = animator != null && animator.GetBool("IsWalking");
                    bool isRunning = animator != null && animator.GetBool("IsRunning");
                    bool isSitting = animator != null && animator.GetBool("ToSitting");


                    var state = mover.waypointStorage.Pop();
                    state.updateAnimator(isWalking, isRunning, isSitting);
                    mover.waypointStorage.Push(state);

                    // Debug.LogWarning("Walking? " + state.isWalking);
                    // Debug.LogWarning("Running? " + state.isRunning);
                    // Debug.LogWarning("Sitting? " + state.isSitting);

                    // if(npc == physicianHostage) Debug.Log(npc + " saving waypoint: " + state.waypoints + "for phase " + GetCurrentPhase());
                }
            }
        }
    }

    private void ResetBackwards()
    {
        foreach (GameObject npc in allNPCs)
        {
            if (npc.activeSelf)
            {
                npc.transform.rotation = Quaternion.identity;
                WaypointMover mover = npc.GetComponent<WaypointMover>();
                if (mover != null && mover.waypoints != null && mover.waypointStorage.Count > 0)
                {
                    var state = mover.waypointStorage.Pop();
                    // Debug.Log("state waypoints: " + state.waypoints);
                    // Debug.Log("national waypoints: " + mover.waypoints);
                    mover.waypoints = state.waypoints;
                    // Debug.Log("child length state: " + state.activeChildLength);
                    // Debug.Log("child length current: " + mover.waypoints.ActiveChildLength);
                    mover.waypoints.ActiveChildLength = state.activeChildLength;
                    
                    mover.waypoints.isMovingForward = state.isMovingForward;
                    mover.waypoints.canLoop = state.canLoop;
                    // Debug.LogWarning("same old waypoint: " + mover.waypointStorage.Peek().sameOld + " for " + npc);
                    if(mover.waypointStorage.Peek().sameOld){
                        Transform LastWaypoint = mover.waypoints.transform.GetChild(mover.waypoints.transform.childCount - 1);
                        mover.currentWaypoint = LastWaypoint;
                        npc.transform.position = LastWaypoint.position;
                    }
                    else {
                        Transform firstWaypoint = mover.waypoints.transform.GetChild(0);
                        mover.currentWaypoint = firstWaypoint;
                        npc.transform.position = firstWaypoint.position;
                    }

                    // Restore animation states
                    Animator animator = npc.GetComponent<Animator>();
                    if (animator != null)
                    {
                        animator.SetBool("IsWalking", state.isWalking);
                        animator.SetBool("IsRunning", state.isRunning);
                        animator.SetBool("ToSitting", state.isSitting);
                    }
                }
            }
        }
    }

    private void MoveNPCsForPhase(GamePhase phase)
    {

        Debug.Log($"Moving NPCs for phase: {phase}");
        
        if (!reverting)
        {
            SaveWaypointState();
            UpdatePaths();
        }

        switch (phase)
        {
            case GamePhase.Phase1:
                // currentPhase = 1;
                ExecutePhase1();
                SaveAnimationState();
                break;
                
            case GamePhase.Phase2:
                // currentPhase = 2; 
                ExecutePhase2();
                break;
                
            case GamePhase.Phase3:
                // currentPhase = 3;
                if (currentPhaseCoroutine != null) {
                    StopCoroutine(currentPhaseCoroutine);
                    currentPhaseCoroutine = null;
                }
                ExecutePhase3();
                break;
                
            case GamePhase.Phase4:
                // currentPhase = 4;
                if (currentPhaseCoroutine != null) {
                    StopCoroutine(currentPhaseCoroutine);
                    currentPhaseCoroutine = null;
                }
                // ExecutePhase4();
                
                break;
            case GamePhase.Phase5:
                // currentPhase = 5;
                if (currentPhaseCoroutine != null) {
                    StopCoroutine(currentPhaseCoroutine);
                    currentPhaseCoroutine = null;
                }
                if (gammaKnifeObject != null) {
                    Debug.Log("gammaknifeobject SPAWNING!!!!");
                    gammaKnifeObject.SetActive(true);
                    gammaKnifeObject.transform.GetChild(0).gameObject.SetActive(true);
                    // gammaKnifeObject.transform.localScale = new Vector3(25f, 25f, 25f);
                }

                if(LLE == null || LLE.Length == 0) LLE = GameObject.FindGameObjectsWithTag("LawEnforcement");
                foreach(GameObject unit in LLE){
                    unit.transform.position = new Vector3(unit.transform.position.x, unit.transform.position.y+9000f, unit.transform.position.z);
                }
                // ExecutePhase5();

                break;
            case GamePhase.Phase6:
                // currentPhase = 6;
                // ExecutePhase6();
                
                if (FD == null || FD.Length == 0) FD = GameObject.FindGameObjectsWithTag("FireDepartment");
                foreach(GameObject unit in FD){
                    unit.transform.position = new Vector3(unit.transform.position.x, unit.transform.position.y+9000f, unit.transform.position.z);
                }
                break;
            case GamePhase.Phase7:
                // currentPhase = 7;
                SetEgressPhase();

                break;
            default:
                Debug.LogError("how did we get here");
                break;

        }

        // if (!reverting)
        // {
        //     SaveAnimationState();
        // }
        
    }
    

    public GamePhase GetCurrentPhase(){
        return phaseList.Current.Phase;
    }

    private void UpdatePaths()
    {
        foreach (GameObject npc in allNPCs)
        {
            if (!npc.activeSelf) continue;

            WaypointMover mover = npc.GetComponent<WaypointMover>();
            if (mover == null || mover.paths == null || mover.waypoints == null)
            {
                Debug.LogWarning($"NPC {npc.name} has missing WaypointMover or path references.");
                resetAnimator(npc);
                continue;
            }
            if(mover.waypoints.ActiveChildLength < 2) continue;

            GamePhase currentPhase = GetCurrentPhase();

            Transform pathsTransform = mover.paths.transform;
            for (int i = mover.pathidx; i < pathsTransform.childCount; i++)
            {
                Transform pathTransform = pathsTransform.GetChild(i);
                Waypoints waypoints = pathTransform.GetComponent<Waypoints>();
                if(waypoints == null){
                    // Debug.LogWarning("Waypoints null, so " + npc + " wont update their paths");
                    continue;
                }else if (waypoints.getActivity() == currentPhase){
                    mover.waypoints = waypoints;
                    mover.currentWaypoint = waypoints.GetNextWaypoint(waypoints.transform.GetChild(0));
                    mover.pathidx = i;

                    Animator animator = mover.GetComponent<Animator>();
                    if (animator != null)
                    {
                        animator.SetBool("IsRunning", true);
                        mover.moveSpeed = 5;
                    }
                    break;
                } 
            }
        }
    }

    private void resetAnimator(GameObject npc){
        // Reset physicianHostage animator from bugging out
        Animator animator = npc.GetComponent<Animator>();
        if(animator != null) {
            animator.Rebind();  // This completely resets the animator state machine
            animator.Update(0f); // This forces an immediate update
            
            // THEN set all animation parameters explicitly to known states
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsRunning", false);
            animator.SetBool("ToSitting", false);
        }
        
        // THEN set position and rotation (order matters)
        npc.transform.rotation = Quaternion.identity;
        if(animator != null) animator.SetBool("IsThreatPresent", false);
    }
    
}