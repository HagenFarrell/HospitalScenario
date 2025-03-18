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
    private List<GameObject> allCivilians;
    private GameObject[] newCivilians;
    private GameObject[] newHostages;
    private GameObject[] newMedicals;
    private GameObject physicianHostage;
    private List<GameObject> allVillains;
    private List<GameObject> allNPCs;
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
        physicianHostage.transform.rotation = new Quaternion(0f, 0f, 0f, 1f);
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

    private GameObject getRadSource(){
        GameObject[] temp = GameObject.FindGameObjectsWithTag("RadiationSource");
        if(temp.Length > 0) return temp[0];
        else{
            Debug.LogWarning("gamma source null???");
            return null;
        }
    }

    // Phase 1 with new waypoint paths
    private void ExecutePhase1()
    {
        if(playerUnits == null || playerUnits.Count == 0){
            GameObject temp = GameObject.Find("Player Units");
            temp.transform.position = new Vector3(temp.transform.position.x, temp.transform.position.y-9000f, temp.transform.position.z);
        }
        else foreach(GameObject unit in playerUnits){
            Debug.Log("moving down " + unit);
            unit.transform.position = new Vector3(unit.transform.position.x, unit.transform.position.y-9000f, unit.transform.position.z);
        }
        
        currentPhase = 1;
        Debug.Log("Executing Phase 1: NPCs begin waypoint movement");
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
                    if (animator != null)
                    {
                        animator.SetBool("IsWalking", true);
                        animator.SetBool("IsRunning", false); // this is fine right
                    }
                }
            }
        }

        // Sets allVillains disc to green to make then try to blend in
        foreach(GameObject villain in allVillains) {
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

    }

    private void ExecutePhase3(){
        Debug.Log($"The allVillains have taken {physicianHostage.name} hostage!");
        Animator animator = physicianHostage.GetComponent<Animator>();
        if(animator != null) animator.SetBool("IsThreatPresent", false);
    }

    private void ExecutePhase4(){
        WaypointMover mover = allVillains[0].GetComponent<WaypointMover>();
        mover.waypoints = GameObject.Find("Waypoints15")?.GetComponent<Waypoints>();
        mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0)); //this is how we get first waypoint externally

        Animator animator = mover.GetComponent<Animator>();
        // Change animation to walking
        if (animator != null)
        {
            animator.SetBool("IsRunning", true);
            mover.moveSpeed = 5;
        }

        mover = allVillains[1].GetComponent<WaypointMover>();
        mover.waypoints = GameObject.Find("Waypoints14")?.GetComponent<Waypoints>();
        mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));

        Animator animator1 = mover.GetComponent<Animator>();
        // Change animation to walking
        if (animator1 != null)
        {
            animator1.SetBool("IsRunning", true);
            mover.moveSpeed = 5;
        }

        mover = allVillains[3].GetComponent<WaypointMover>();
        mover.waypoints = GameObject.Find("Waypoints16")?.GetComponent<Waypoints>();
        mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));

        Animator animator2 = mover.GetComponent<Animator>();
        // Change animation to walking
        if (animator2 != null)
        {
            animator2.SetBool("IsRunning", true);
            mover.moveSpeed = 5;
        }

        mover = allVillains[4].GetComponent<WaypointMover>();
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


        
        // Two allVillains take the physician hostage to the gamma knife room
        if (physicianHostage != null && villainsInside != null && villainsInside.Length >= 2) {
            Debug.Log($"Two allVillains are taking {physicianHostage.name} to the gamma knife room");
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
        if(LLE == null || LLE.Length == 0) LLE = GameObject.FindGameObjectsWithTag("LawEnforcement");
        foreach(GameObject unit in LLE){
            unit.transform.position = new Vector3(unit.transform.position.x, unit.transform.position.y+9000f, unit.transform.position.z);
        }


        WaypointMover mover = allVillains[2].GetComponent<WaypointMover>();
        mover.waypoints = GameObject.Find("Waypoints13")?.GetComponent<Waypoints>();
        mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));

        Animator animator1 = mover.GetComponent<Animator>();
        // Change animation
        if (animator1 != null)
        {
            animator1.SetBool("IsRunning", true);
            mover.moveSpeed = 5;
        }

        mover = allVillains[3].GetComponent<WaypointMover>();
        mover.waypoints = GameObject.Find("Waypoints24")?.GetComponent<Waypoints>();
        mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));

        Animator animator2 = mover.GetComponent<Animator>();
        // Change animation 
        if (animator2 != null)
        {
            animator2.SetBool("IsRunning", true);
            mover.moveSpeed = 5;
        }

        mover = allVillains[4].GetComponent<WaypointMover>();
        mover.waypoints = GameObject.Find("Waypoints25")?.GetComponent<Waypoints>();
        mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));

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

        if (gammaKnifeObject != null) {
            Debug.Log("gammaknifeobject spawning");
            gammaKnifeObject.SetActive(true);
            gammaKnifeObject.transform.GetChild(0).gameObject.SetActive(true);
            // gammaKnifeObject.transform.localScale = new Vector3(25f, 25f, 25f);
        }

    }

    private void ExecutePhase6(){
        if (FD == null || FD.Length == 0) FD = GameObject.FindGameObjectsWithTag("FireDepartment");
        foreach(GameObject unit in FD){
            unit.transform.position = new Vector3(unit.transform.position.x, unit.transform.position.y+9000f, unit.transform.position.z);
        }

        WaypointMover mover = allVillains[0].GetComponent<WaypointMover>();
        mover.waypoints = GameObject.Find("Waypoints26")?.GetComponent<Waypoints>();
        mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0)); //this is how we get first waypoint externally

        Animator animator = mover.GetComponent<Animator>();
        // Change animation to walking
        if (animator != null)
        {
            animator.SetBool("IsRunning", true);
            mover.moveSpeed = 5;
        }

        mover = allVillains[1].GetComponent<WaypointMover>();
        mover.waypoints = GameObject.Find("Waypoints27")?.GetComponent<Waypoints>();
        mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));

        Animator animator1 = mover.GetComponent<Animator>();
        // Change animation to walking
        if (animator1 != null)
        {
            animator1.SetBool("IsRunning", true);
            mover.moveSpeed = 5;
        }

        mover = allVillains[2].GetComponent<WaypointMover>();
        mover.waypoints = GameObject.Find("Waypoints28")?.GetComponent<Waypoints>();
        mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));

        animator1 = mover.GetComponent<Animator>();
        // Change animation to walking
        if (animator1 != null)
        {
            animator1.SetBool("IsRunning", true);
            mover.moveSpeed = 5;
        }

        mover = allVillains[3].GetComponent<WaypointMover>();
        mover.waypoints = GameObject.Find("Waypoints29")?.GetComponent<Waypoints>();
        mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));

        Animator animator2 = mover.GetComponent<Animator>();
        // Change animation to walking
        if (animator2 != null)
        {
            animator2.SetBool("IsRunning", true);
            mover.moveSpeed = 5;
        }

        mover = allVillains[4].GetComponent<WaypointMover>();
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
                
                WaypointMover mover = allVillains[0].GetComponent<WaypointMover>();
                mover.waypoints = GameObject.Find("Waypoints34")?.GetComponent<Waypoints>();
                mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0)); //this is how we get first waypoint externally

                Animator animator = mover.GetComponent<Animator>();
                // Change animation to walking
                if (animator != null)
                {
                    animator.SetBool("IsRunning", true);
                    mover.moveSpeed = 5;
                }

                mover = allVillains[1].GetComponent<WaypointMover>();
                mover.waypoints = GameObject.Find("Waypoints35")?.GetComponent<Waypoints>();
                mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));

                Animator animator1 = mover.GetComponent<Animator>();
                // Change animation to walking
                if (animator1 != null)
                {
                    animator1.SetBool("IsRunning", true);
                    mover.moveSpeed = 5;
                }

                mover = allVillains[2].GetComponent<WaypointMover>();
                mover.waypoints = GameObject.Find("Waypoints36")?.GetComponent<Waypoints>();
                mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));

                animator1 = mover.GetComponent<Animator>();
                // Change animation to walking
                if (animator1 != null)
                {
                    animator1.SetBool("IsRunning", true);
                    mover.moveSpeed = 5;
                }

                mover = allVillains[3].GetComponent<WaypointMover>();
                mover.waypoints = GameObject.Find("Waypoints37")?.GetComponent<Waypoints>();
                mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));

                Animator animator2 = mover.GetComponent<Animator>();
                // Change animation to walking
                if (animator2 != null)
                {
                    animator2.SetBool("IsRunning", true);
                    mover.moveSpeed = 5;
                }

                mover = allVillains[4].GetComponent<WaypointMover>();
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
                
                mover = allVillains[0].GetComponent<WaypointMover>();
                mover.waypoints = GameObject.Find("Waypoints42")?.GetComponent<Waypoints>();
                mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0)); //this is how we get first waypoint externally

                animator = mover.GetComponent<Animator>();
                // Change animation to walking
                if (animator != null)
                {
                    animator.SetBool("IsRunning", true);
                    mover.moveSpeed = 5;
                }

                mover = allVillains[1].GetComponent<WaypointMover>();
                mover.waypoints = GameObject.Find("Waypoints43")?.GetComponent<Waypoints>();
                mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));

                animator1 = mover.GetComponent<Animator>();
                // Change animation to walking
                if (animator1 != null)
                {
                    animator1.SetBool("IsRunning", true);
                    mover.moveSpeed = 5;
                }

                mover = allVillains[2].GetComponent<WaypointMover>();
                mover.waypoints = GameObject.Find("Waypoints44")?.GetComponent<Waypoints>();
                mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));

                animator1 = mover.GetComponent<Animator>();
                // Change animation to walking
                if (animator1 != null)
                {
                    animator1.SetBool("IsRunning", true);
                    mover.moveSpeed = 5;
                }

                mover = allVillains[3].GetComponent<WaypointMover>();
                mover.waypoints = GameObject.Find("Waypoints45")?.GetComponent<Waypoints>();
                mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));

                animator2 = mover.GetComponent<Animator>();
                // Change animation to walking
                if (animator2 != null)
                {
                    animator2.SetBool("IsRunning", true);
                    mover.moveSpeed = 5;
                }

                mover = allVillains[4].GetComponent<WaypointMover>();
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

                mover = allVillains[0].GetComponent<WaypointMover>();
                mover.waypoints = GameObject.Find("Waypoints50")?.GetComponent<Waypoints>();
                mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0)); //this is how we get first waypoint externally

                animator = mover.GetComponent<Animator>();
                // Change animation to walking
                if (animator != null)
                {
                    animator.SetBool("IsRunning", true);
                    mover.moveSpeed = 5;
                }

                mover = allVillains[1].GetComponent<WaypointMover>();
                mover.waypoints = GameObject.Find("Waypoints51")?.GetComponent<Waypoints>();
                mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));

                animator1 = mover.GetComponent<Animator>();
                // Change animation to walking
                if (animator1 != null)
                {
                    animator1.SetBool("IsRunning", true);
                    mover.moveSpeed = 5;
                }

                mover = allVillains[2].GetComponent<WaypointMover>();
                mover.waypoints = GameObject.Find("Waypoints52")?.GetComponent<Waypoints>();
                mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));

                animator1 = mover.GetComponent<Animator>();
                // Change animation to walking
                if (animator1 != null)
                {
                    animator1.SetBool("IsRunning", true);
                    mover.moveSpeed = 5;
                }

                mover = allVillains[3].GetComponent<WaypointMover>();
                mover.waypoints = GameObject.Find("Waypoints53")?.GetComponent<Waypoints>();
                mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));

                animator2 = mover.GetComponent<Animator>();
                // Change animation to walking
                if (animator2 != null)
                {
                    animator2.SetBool("IsRunning", true);
                    mover.moveSpeed = 5;
                }

                mover = allVillains[4].GetComponent<WaypointMover>();
                mover.waypoints = GameObject.Find("Waypoints54")?.GetComponent<Waypoints>();
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
                mover.waypoints = GameObject.Find("Waypoints55")?.GetComponent<Waypoints>();
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
                mover.waypoints = GameObject.Find("Waypoints56")?.GetComponent<Waypoints>();
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
                mover.waypoints = GameObject.Find("Waypoints57")?.GetComponent<Waypoints>();
                mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));

                break;
            case 4: // v
                // Phase Egress 4: Adversaries move to the rear exit
                Debug.Log("Phase Egress: " + egress);
                
                mover = allVillains[0].GetComponent<WaypointMover>();
                mover.waypoints = GameObject.Find("Waypoints58")?.GetComponent<Waypoints>();
                mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0)); //this is how we get first waypoint externally

                animator = mover.GetComponent<Animator>();
                // Change animation to walking
                if (animator != null)
                {
                    animator.SetBool("IsRunning", true);
                    mover.moveSpeed = 5;
                }

                mover = allVillains[1].GetComponent<WaypointMover>();
                mover.waypoints = GameObject.Find("Waypoints59")?.GetComponent<Waypoints>();
                mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));

                animator1 = mover.GetComponent<Animator>();
                // Change animation to walking
                if (animator1 != null)
                {
                    animator1.SetBool("IsRunning", true);
                    mover.moveSpeed = 5;
                }

                mover = allVillains[2].GetComponent<WaypointMover>();
                mover.waypoints = GameObject.Find("Waypoints60")?.GetComponent<Waypoints>();
                mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));

                animator1 = mover.GetComponent<Animator>();
                // Change animation to walking
                if (animator1 != null)
                {
                    animator1.SetBool("IsRunning", true);
                    mover.moveSpeed = 5;
                }

                mover = allVillains[3].GetComponent<WaypointMover>();
                mover.waypoints = GameObject.Find("Waypoints61")?.GetComponent<Waypoints>();
                mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));

                animator2 = mover.GetComponent<Animator>();
                // Change animation to walking
                if (animator2 != null)
                {
                    animator2.SetBool("IsRunning", true);
                    mover.moveSpeed = 5;
                }

                mover = allVillains[4].GetComponent<WaypointMover>();
                mover.waypoints = GameObject.Find("Waypoints62")?.GetComponent<Waypoints>();
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
                mover.waypoints = GameObject.Find("Waypoints63")?.GetComponent<Waypoints>();
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
                mover.waypoints = GameObject.Find("Waypoints64")?.GetComponent<Waypoints>();
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
                mover.waypoints = GameObject.Find("Waypoints65")?.GetComponent<Waypoints>();
                mover.currentWaypoint = mover.waypoints.GetNextWaypoint(mover.waypoints.transform.GetChild(0));

                break;
            default:
                Debug.LogWarning("Invalid egress phase!");
                break;
        }
    }

    private void MoveNPCsForPhase(GamePhase phase)
    {
        Debug.Log($"Moving NPCs for phase: {phase}");
        if (phase != GamePhase.Phase1 && phase != GamePhase.Phase2)
        {
            foreach (GameObject npc in allNPCs)
            {
                npc.transform.rotation = new Quaternion(0f, 0f, 0f, 1f);
                WaypointMover mover = npc.GetComponent<WaypointMover>();
                if (mover != null && mover.waypoints != null)
                {
                    Transform lastWaypoint = mover.waypoints.transform.GetChild(mover.waypoints.transform.childCount - 1);
                    
                    // Only teleport if not already at the last waypoint
                    if (mover.currentWaypoint != lastWaypoint)
                    {
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
                        }
                    }
                }
            }
        }
        
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
                // Two allVillains take the physician hostage to the gamma knife room
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
        
    }
    

    public GamePhase GetCurrentPhase(){
        return phaseList.Current.Phase;
    }
    
}