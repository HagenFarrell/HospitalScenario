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
    private int runSpeed;
    // private GameObject pathObject;
    // private bool sameOld;
    private int egress;
    private bool reverting;
    public delegate void EgressSelectedHandler(int egressPhase);
    public static event EgressSelectedHandler OnEgressSelected;

    // public int currentPhase;

    private void Start()
    {
        phaseList = new PhaseLinkedList();
        // Define the phases
        foreach (GamePhase phase in System.Enum.GetValues(typeof(GamePhase)))
            phaseList.AddPhase(phase);
        phaseList.SetCurrentToHead();

        allCivilians = new List<GameObject>();
        allVillains = new List<GameObject>();
        allNPCs = new List<GameObject>();

        if(gammaKnifeObject == null) gammaKnifeObject = getRadSource();
        if (gammaKnifeObject != null) {
            gammaKnifeObject.SetActive(false);
            // gammaKnifeObject.transform.GetChild(0).localScale = new Vector3(1.125f, 1.125f, 1.125f);
        }
        runSpeed = 5;

        
        OnEgressSelected += ExecuteEgressPhase;
        FindKeyNPCs();
        StartPhase();
    }

    private void Update()
    {
        if (phaseList == null || phaseList.Current == null)
        {
            Debug.LogError("phaseList or phaseList.Current is null!");
            return;
        }

        if (phaseList.Current.Phase == GamePhase.Phase7)
        {
            SetEgressPhase();
        }

        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            Debug.LogWarning("Resetting the current phase doesn't really work right now...");
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
    private void HidePlayers(){
        if(playerUnits == null || playerUnits.Count == 0){
            GameObject temp = GameObject.Find("Player Units");
        }
        foreach(GameObject unit in playerUnits){
            unit.transform.position = new Vector3(unit.transform.position.x, unit.transform.position.y-9000f, unit.transform.position.z);
        }
    }

    private void HandleStartCivs()
    {
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
    }

    private void Phase2CivPaths(){
        foreach(GameObject npc in allCivilians){
            WaypointMover mover = npc.GetComponent<WaypointMover>();
            if(mover == null) continue;

            mover.waypoints.isMovingForward = true;
            mover.waypoints.canLoop = false;
            mover.waypoints.enableAll();
        }
    }
    private void DespawnOnEscape(){
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
    }
    private void UpdateHostageDiscs(){
        // physicianHostage special - orange!
        GameObject disc = physicianHostage.transform.GetChild(2).gameObject;
        Renderer discRenderer = disc.GetComponent<Renderer>();
        discRenderer.material.color = new Color(1f, 0.5f, 0f);

        foreach (GameObject hostage in newHostages)
        {
            // Change disk color to yellow at last waypoint
            disc = hostage.transform.GetChild(2).gameObject;

            if (discRenderer == null && gameObject.CompareTag("PhysicianHostage")) continue;

            discRenderer = disc.GetComponent<Renderer>();
            discRenderer.material.color = Color.yellow;
        }
    }

    private void UpdateVillainDiscs(Color color){
        foreach(GameObject villain in allVillains) {
            GameObject disc = villain.transform.GetChild(2).gameObject;
            
            // Change the disc color to green
            Renderer discRenderer = disc.GetComponent<Renderer>();
            if (discRenderer != null) {
                discRenderer.material.color = color;
            }
        }
    }
    private void ToggleGun(){
        // Shows gun
        GameObject weapon = allVillains[0].transform.GetChild(1).gameObject;
        weapon.SetActive(!weapon.activeSelf);
    }

    private void Alarming(){
        // Receptionist hits duress alarm
        Debug.Log("Duress alarm activated. Dispatcher notified.");
        physicianHostage.transform.rotation = new Quaternion(0f, 0f, 0f, 1f);
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
                if(mover.waypoints.ActiveChildLength != mover.waypoints.transform.childCount-1) 
                    mover.waypoints.enableAll();

                Animator animator = mover.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.SetBool("IsRunning", true);
                    mover.moveSpeed = runSpeed;
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
                    if (mover.waypointStorage.Count == 0){
                        Debug.LogWarning($"No stored states for {npc.name}!");
                        continue;
                    }
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
    private void SpawnGammaKnife(){
        if (gammaKnifeObject != null) {
            gammaKnifeObject.SetActive(true);
            gammaKnifeObject.transform.GetChild(0).gameObject.SetActive(true);
        }
    }
    private void SpawnLLE(){
        if(LLE == null || LLE.Length == 0) LLE = GameObject.FindGameObjectsWithTag("LawEnforcement");
        foreach(GameObject unit in LLE){
            unit.transform.position = new Vector3(unit.transform.position.x, unit.transform.position.y+9000f, unit.transform.position.z);
        }
    }
    private void SpawnFD(){
        if (FD == null || FD.Length == 0) FD = GameObject.FindGameObjectsWithTag("FireDepartment");
        foreach(GameObject unit in FD){
            unit.transform.position = new Vector3(unit.transform.position.x, unit.transform.position.y+9000f, unit.transform.position.z);
        }
    }
    private void MoveNPCsForPhase(GamePhase phase)
    {

        Debug.Log($"Moving NPCs for phase: {phase}");
        
        if (!reverting)
        {
            SaveWaypointState();
        }
        UpdatePaths();

        switch (phase)
        {
            case GamePhase.Phase1:
                HandleStartCivs();
                SaveAnimationState();
                UpdateVillainDiscs(Color.green); // they're "blending in" with the other civilians
                if(allVillains[0].transform.GetChild(1).gameObject.activeSelf) ToggleGun();
                break;
                
            case GamePhase.Phase2:
                if(!allVillains[0].transform.GetChild(1).gameObject.activeSelf) ToggleGun();
                Alarming();
                Phase2CivPaths();
                DespawnOnEscape();
                UpdateHostageDiscs();
                UpdateVillainDiscs(Color.red);
                break;
                
            case GamePhase.Phase3:
                resetAnimator(physicianHostage);
                Debug.Log("The adversaries have taken " + physicianHostage + " hostage!");
                break;
                
            case GamePhase.Phase4:
                
                
                break;
            case GamePhase.Phase5:
                SpawnLLE();
                SpawnGammaKnife();

                break;
            case GamePhase.Phase6:
                SpawnFD();
                
                break;
            case GamePhase.Phase7:
                SetEgressPhase();

                break;
            default:
                Debug.LogError("how did we get here");
                break;

        }
        
    }
    

    public GamePhase GetCurrentPhase(){
        return phaseList.Current.Phase;
    }

    private void UpdatePaths()
    {
        foreach (GameObject npc in allNPCs)
        {
            if (!npc.activeSelf) {
                // Debug.Log(npc + "is inactive");
                continue;
            }

            WaypointMover mover = npc.GetComponent<WaypointMover>();
            if (mover == null || mover.paths == null || mover.waypoints == null)
            {
                Debug.LogWarning($"NPC {npc.name} has missing WaypointMover or path references.");
                resetAnimator(npc);
                continue;
            }
            if(mover.waypoints.ActiveChildLength < 2 && GetCurrentPhase() == GamePhase.Phase1) {
                // Debug.Log("not enough active kids in " + npc);
                continue;
            }

            GamePhase currentPhase = GetCurrentPhase();

            Transform pathsTransform = mover.paths.transform;
            // Debug.Log("updating path for " + npc);
            for (int i = mover.pathidx; i < pathsTransform.childCount; i++)
            {
                Transform pathTransform = pathsTransform.GetChild(i);
                Waypoints waypoints = pathTransform.GetComponent<Waypoints>();
                if(waypoints == null || waypoints.transform.childCount == 0){
                    // Debug.LogWarning("Waypoints null, so " + npc + " wont update their paths");
                    continue;
                }else if (waypoints.getActivity() == currentPhase){
                    mover.waypoints = waypoints;
                    mover.currentWaypoint = waypoints.GetNextWaypoint(waypoints.transform.GetChild(0));
                    mover.pathidx = i;
                    if(mover.waypoints.ActiveChildLength != mover.waypoints.transform.childCount-1) 
                        mover.waypoints.enableAll();

                    Animator animator = mover.GetComponent<Animator>();
                    if (animator != null)
                    {
                        animator.SetBool("IsRunning", true);
                        mover.moveSpeed = runSpeed;
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