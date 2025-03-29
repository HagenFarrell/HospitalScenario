using UnityEngine;
using System.Collections.Generic;
using PhaseLink;
using System.Collections;
using Mirror;

public class PhaseManager : NetworkBehaviour
{
    private PhaseLinkedList phaseList;
    private Player playerRole;
    
    // radiaoctive object
    public GameObject gammaKnifeObject;
    
    // References to different NPC groups
    private GameObject[] villainsInside;
    private GameObject[] villainsOutside;
    [SerializeField] private GameObject PlayerUnitsObject;
    [SerializeField] private GameObject VillainLeagueObject;
    [SerializeField] private GameObject CiviliansObject;
    private List<GameObject> playerUnits;
    private GameObject[] FD;
    private GameObject[] LLE;
    private GameObject[] LLEVehicles;
    private GameObject[] FDVehicles;
    private List<GameObject> allCivilians;
    private GameObject[] newCivilians;
    private GameObject[] newHostages;
    private GameObject[] newMedicals;
    private GameObject physicianHostage;
    private List<GameObject> allVillains;
    private List<GameObject> allNPCs;
    // other
    private float runSpeed;
    private bool reverting;
    // egress
    public delegate void EgressSelectedHandler(int egressPhase);
    public static event EgressSelectedHandler OnEgressSelected;
    private int egress;


    //new netcdoe
    public static PhaseManager Instance {get; private set; }
    [SyncVar(hook = nameof(OnPhaseChanged))]
    private GamePhase syncedPhase;
    private bool isMirrorInitialization = false;
    //[SyncVar(hook = nameof(OnBubbleStateChanged))]
    private bool isBubbleActive;
    
    private void Awake()
    {
        // This runs before Mirror's processing
        isMirrorInitialization = true;
        gameObject.SetActive(true); // Force active
        Instance = this;
    }
    
    private void Start()
    {
        gameObject.SetActive(true);
        Debug.Log("PhaseManager START");
        //gameObject.SetActive(true);
        //debug for checking why phasehandling starts turned off
        Debug.Log($"PhaseManager isServer: {isServer}, isClient: {isClient}, hasAuthority: {hasAuthority}");

        phaseList = new PhaseLinkedList();
        // Define the phases
        foreach (GamePhase phase in System.Enum.GetValues(typeof(GamePhase)))
            phaseList.AddPhase(phase);
        phaseList.SetCurrentToHead();
        
        runSpeed = 5f;
        OnEgressSelected += ExecuteEgressPhase;
        FindKeyNPCs();
        HidePlayers();

        //netcode
        if (isServer)
        {
            SetPhase(GamePhase.Phase1); //instructor sets 1st phase, triggers sync
        }

        if(gammaKnifeObject == null) gammaKnifeObject = getRadSource();
        if (gammaKnifeObject != null) {
            gammaKnifeObject.SetActive(false);
        }
        //runSpeed = 5f;

        /*
        OnEgressSelected += ExecuteEgressPhase;
        FindKeyNPCs();
        HidePlayers();
        StartPhase();
        */
    }

    private void Update()
    {
        
        // Debug.Log($"PhaseManager Awake - Active: {gameObject.activeSelf}, NetId: {GetComponent<NetworkIdentity>().netId}");
        Instance = this;
        if (phaseList == null || phaseList.Current == null)
        {
            Debug.LogError("phaseList or phaseList.Current is null!");
            return;
        }

        if (phaseList.Current.Phase == GamePhase.Phase7)
        {
            SetEgressPhase();
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            // Debug.LogWarning("Resetting the current phase doesn't really work right now...");
            ResetCurrent();
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
        playerRole = FindObjectOfType<Player>();
        if(playerRole == null){
            Debug.LogError("playerRole null");
        }
        if (playerRole.getPlayerRole() == Player.Roles.Instructor)
        {
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

        if (OnEgressSelected != null)
        {
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
        // Initialize lists
        allVillains = new List<GameObject>();
        playerUnits = new List<GameObject>();
        allCivilians = new List<GameObject>();
        allNPCs = new List<GameObject>();

        // Find villains - use serialized reference first
        if (VillainLeagueObject != null)
        {
            foreach (Transform child in VillainLeagueObject.transform)
            {
                if (child.CompareTag("Villains") || child.CompareTag("OutsideVillains"))
                {
                    allVillains.Add(child.gameObject);
                }
            }
        }
        else
        {
            // Fall back to tag search
            villainsInside = GameObject.FindGameObjectsWithTag("Villains");
            villainsOutside = GameObject.FindGameObjectsWithTag("OutsideVillains");
            allVillains.AddRange(villainsInside);
            allVillains.AddRange(villainsOutside);
        }

        // Find player units - use serialized reference first
        if (PlayerUnitsObject != null)
        {
            foreach (Transform child in PlayerUnitsObject.transform)
            {
                if (child.CompareTag("FireDepartment") || child.CompareTag("LawEnforcement"))
                {
                    playerUnits.Add(child.gameObject);
                }
            }
            
            List<GameObject> fdList = new List<GameObject>();
            foreach (GameObject unit in playerUnits)
            {
                if (unit.CompareTag("FireDepartment"))
                {
                    fdList.Add(unit);
                }
            }
            FD = fdList.ToArray();
            
            List<GameObject> lleList = new List<GameObject>();
            foreach (GameObject unit in playerUnits)
            {
                if (unit.CompareTag("LawEnforcement"))
                {
                    lleList.Add(unit);
                }
            }
            LLE = lleList.ToArray();

            LLEVehicles = GameObject.FindGameObjectsWithTag("LawEnforcementVehicle");
            FDVehicles = GameObject.FindGameObjectsWithTag("FireDepartmentVehicle");
        }
        else
        {
            // Fall back to tag search
            FD = GameObject.FindGameObjectsWithTag("FireDepartment");
            LLE = GameObject.FindGameObjectsWithTag("LawEnforcement");
            playerUnits.AddRange(FD);
            playerUnits.AddRange(LLE);
        }

        // Find civilians - use serialized reference first
        if (CiviliansObject != null)
        {
            foreach (Transform child in CiviliansObject.transform)
            {
                if (child.CompareTag("PhysicianHostage") || 
                    child.CompareTag("Hostages") || 
                    child.CompareTag("Civilians") || 
                    child.CompareTag("Medicals"))
                {
                    allCivilians.Add(child.gameObject);
                    
                    // Special handling for physician hostage
                    if (child.CompareTag("PhysicianHostage"))
                    {
                        physicianHostage = child.gameObject;
                    }
                }
            }
            
            List<GameObject> civiliansList = new List<GameObject>();
            List<GameObject> hostagesList = new List<GameObject>();
            List<GameObject> medicalsList = new List<GameObject>();
            
            foreach (GameObject civilian in allCivilians)
            {
                if (civilian.CompareTag("Civilians"))
                {
                    civiliansList.Add(civilian);
                }
                else if (civilian.CompareTag("Hostages"))
                {
                    hostagesList.Add(civilian);
                }
                else if (civilian.CompareTag("Medicals"))
                {
                    medicalsList.Add(civilian);
                }
            }
            
            newCivilians = civiliansList.ToArray();
            newHostages = hostagesList.ToArray();
            newMedicals = medicalsList.ToArray();
        }
        else
        {
            // Fall back to tag search
            GameObject[] physicianHostages = GameObject.FindGameObjectsWithTag("PhysicianHostage");
            physicianHostage = physicianHostages.Length > 0 ? physicianHostages[0] : null;
            
            newCivilians = GameObject.FindGameObjectsWithTag("Civilians");
            newHostages = GameObject.FindGameObjectsWithTag("Hostages");
            newMedicals = GameObject.FindGameObjectsWithTag("Medicals");

            if (physicianHostage != null) allCivilians.Add(physicianHostage);
            if (newHostages != null) allCivilians.AddRange(newHostages);
            if (newCivilians != null) allCivilians.AddRange(newCivilians);
            if (newMedicals != null) allCivilians.AddRange(newMedicals);
        }

        allNPCs.AddRange(allVillains);
        allNPCs.AddRange(allCivilians);

        if (allVillains.Count == 0) Debug.LogWarning("No villains found!");
        if (playerUnits.Count == 0) Debug.LogWarning("No player units found!");
        if (allCivilians.Count == 0) Debug.LogWarning("No civilians found!");
        if (physicianHostage == null) Debug.LogWarning("Physician hostage not found!");
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
            civilian.SetActive(false);
        }
        
        // Find and disable any remaining medicals
        foreach (GameObject medical in newMedicals)
        {
            medical.SetActive(false);
        }
    }
    public void NextPhase()
    {
        reverting = false;

        if (phaseList.MoveNext())
        {
            // Debug.Log("Moving to next phase.");
            ResetForward();
            StartPhase();
        }
        else
        {
            Debug.LogWarning("Already at the last phase!");
        }
    }
    public void PreviousPhase()
    {
        reverting = true;

        if (phaseList.MovePrevious())
        {
            if(OnEgressSelected == null) OnEgressSelected += ExecuteEgressPhase;
            // Debug.Log("Moving to previous phase.");
            ResetBackwards();
            StartPhase();
        }
        else
        {
            Debug.LogWarning("Already at the first phase!");
        }
    }
    private GameObject getRadSource(){
        return allVillains[0].transform.GetChild(4).gameObject;
    }
    private void HidePlayers(){
        if(playerUnits == null || playerUnits.Count == 0){
            Debug.LogError("Player units null, attempting to locate");
            GameObject temp = GameObject.Find("Player Units");
        }
        foreach(GameObject unit in playerUnits){
            unit.transform.position = new Vector3(unit.transform.position.x, unit.transform.position.y-9000f, unit.transform.position.z);
        }
        HideAllVehicles();
    }
    private void HideLLE(){
        if(LLE == null || LLE.Length == 0){
            LLE = GameObject.FindGameObjectsWithTag("LawEnforcement");
        }
        foreach(GameObject unit in LLE){
            unit.transform.position = new Vector3(unit.transform.position.x, unit.transform.position.y-9000f, unit.transform.position.z);
        }
        if(LLEVehicles == null || LLEVehicles.Length == 0){
            LLEVehicles = GameObject.FindGameObjectsWithTag("LawEnforcementVehicle");
        }
        foreach(GameObject vehicle in LLEVehicles){
            vehicle.transform.position = new Vector3(vehicle.transform.position.x, vehicle.transform.position.y-9000f, vehicle.transform.position.z);
        }
    }
    private void HideFD(){
        if(FD == null || FD.Length == 0){
            FD = GameObject.FindGameObjectsWithTag("FireDepartment");
        }
        foreach(GameObject unit in FD){
            unit.transform.position = new Vector3(unit.transform.position.x, unit.transform.position.y-9000f, unit.transform.position.z);
        }
        if(FDVehicles == null || FDVehicles.Length == 0){
        FDVehicles = GameObject.FindGameObjectsWithTag("FireDepartmentVehicle");
        }
        foreach(GameObject vehicle in FDVehicles){
            vehicle.transform.position = new Vector3(vehicle.transform.position.x, vehicle.transform.position.y-9000f, vehicle.transform.position.z);
        }
    }

    private void HideAllVehicles(){
        // Hide LLE vehicles
        if(LLEVehicles == null || LLEVehicles.Length == 0){
            LLEVehicles = GameObject.FindGameObjectsWithTag("LawEnforcementVehicle");
        }
        foreach(GameObject vehicle in LLEVehicles){
            vehicle.transform.position = new Vector3(vehicle.transform.position.x, vehicle.transform.position.y-9000f, vehicle.transform.position.z);
        }
        
        // Hide FD vehicles
        if(FDVehicles == null || FDVehicles.Length == 0){
            FDVehicles = GameObject.FindGameObjectsWithTag("FireDepartmentVehicle");
        }
        foreach(GameObject vehicle in FDVehicles){
            vehicle.transform.position = new Vector3(vehicle.transform.position.x, vehicle.transform.position.y-9000f, vehicle.transform.position.z);
        }
    }
    private void HandleStartCivs()
    {
        foreach (GameObject civilian in allCivilians)
        {
            civilian.SetActive(true);
            // Set animation state to walking
            Animator animator = civilian.GetComponent<Animator>();
            
            // Enable the WaypointMover component
            WaypointMover mover = civilian.GetComponent<WaypointMover>();

            if (mover != null)
            {
                if (mover.waypoints != null && mover.waypoints.transform.childCount > 0)
                {
                    mover.currentWaypoint = mover.waypoints.GetNextWaypoint(null);
                    mover.enabled = true;
                    mover.despawnAtLastWaypoint = false;
                    civilian.transform.position = mover.currentWaypoint.transform.position;
                    mover.waypoints.ResetToPhase1Settings();
                    mover.pathidx = 0;
                    resetAnimator(civilian);
                }
                if(mover.waypoints.waypointsActiveInPhase1 == 1){
                    animator.SetBool("IsWalking", false);
                    animator.SetBool("IsRunning", false);
                    animator.SetBool("ToSitting", true);
                } else {
                    if (animator != null)
                    {
                        animator.SetBool("IsWalking", true);
                        animator.SetBool("IsRunning", false);
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
    private void InitializeDiscColors(){
        GameObject disc = physicianHostage.transform.GetChild(2).gameObject;
        Renderer discRenderer = disc.GetComponent<Renderer>();
        foreach (GameObject npc in allNPCs)
        {
            // Change disk color to green
            disc = npc.transform.GetChild(2).gameObject;

            discRenderer = disc.GetComponent<Renderer>();
            discRenderer.material.color = Color.green;
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
        // toggles gun
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
        OnEgressSelected -= ExecuteEgressPhase; 
        SaveWaypointState();

        Debug.Log($"Egress phase {selectedEgress} selected!");
        egress = selectedEgress;

        foreach (GameObject npc in allNPCs)
        {
            if (!npc.activeSelf || (npc.CompareTag("Civilians") || npc.CompareTag("Medicals"))) continue;

            WaypointMover mover = npc.GetComponent<WaypointMover>();
            if(mover.paths == null) mover.paths = mover.waypoints.transform.parent.gameObject;
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
                if(GetCurrentPhase() != GamePhase.Phase1) 
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
    private void ResetCurrent()
    {
        if (phaseList.Current == null) return;

        foreach(GameObject npc in allNPCs)
        {
            if (npc.activeSelf)
            {
                npc.transform.rotation = Quaternion.identity;
                WaypointMover mover = npc.GetComponent<WaypointMover>();
                if (mover != null && mover.waypoints != null && phaseList.Current.State.ContainsKey(npc))
                {
                    var state = phaseList.Current.State[npc];
                    
                    if((GetCurrentPhase() != GamePhase.Phase2 && GetCurrentPhase() != GamePhase.Phase4) && state.sameOld)
                    {
                        Transform LastWaypoint = mover.waypoints.transform.GetChild(mover.waypoints.transform.childCount - 1);
                        mover.currentWaypoint = LastWaypoint;
                        npc.transform.position = LastWaypoint.position;
                    }
                    else 
                    {
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

    private void ResetBackwards()
    {
        if (phaseList.Current == null) return;

        foreach (GameObject npc in allNPCs)
        {
            if (npc.activeSelf)
            {
                npc.transform.rotation = Quaternion.identity;
                WaypointMover mover = npc.GetComponent<WaypointMover>();
                if (mover != null && mover.waypoints != null && phaseList.Current.State.ContainsKey(npc))
                {
                    var state = phaseList.Current.State[npc];
                    mover.waypoints = state.waypoints;
                    mover.waypoints.ActiveChildLength = state.activeChildLength;
                    mover.waypoints.isMovingForward = state.isMovingForward;
                    mover.waypoints.canLoop = state.canLoop;
                    // Debug.Log("sameold: " + state.sameOld + "  for phase " + GetCurrentPhase() + " for npc: " + npc);
                    
                    if(state.sameOld)
                    {
                        Transform LastWaypoint = mover.waypoints.transform.GetChild(mover.waypoints.transform.childCount - 1);
                        if (LastWaypoint != null)
                        {
                            mover.currentWaypoint = LastWaypoint;
                            npc.transform.position = LastWaypoint.position;
                        }
                        else
                        {
                            Debug.LogError($"Invalid LastWaypoint for {npc.name}!");
                        }
                    }
                    else 
                    {
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

    private void ResetForward()
    {
        GamePhase phase = GetCurrentPhase();
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

                            // Reset crouching for hostages
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
        if (phaseList.Current == null) return;

        foreach (GameObject npc in allNPCs)
        {
            if (npc.activeSelf)
            {
                WaypointMover mover = npc.GetComponent<WaypointMover>();
                if (mover != null && mover.waypoints != null)
                {
                    Animator animator = npc.GetComponent<Animator>();
                    bool isWalking = animator?.GetBool("IsWalking") ?? false;
                    bool isRunning = animator?.GetBool("IsRunning") ?? false;
                    bool isSitting = animator?.GetBool("ToSitting") ?? false;

                    // Check if waypoints are the same as previous phase
                    bool sameOld = false;
                    if (phaseList.Current.Previous != null && 
                        phaseList.Current.Previous.State.TryGetValue(npc, out var prevState))
                    {
                        // Compare both the waypoints object and path for more reliable comparison
                        sameOld = mover.waypoints == prevState.waypoints
                            || mover.waypoints.GetInstanceID() == prevState.waypoints.GetInstanceID();
                    }
                    if(GetCurrentPhase() == GamePhase.Phase5) sameOld = true; // bandaid

                    var state = new WaypointState(
                        mover.waypoints,
                        mover.waypoints.ActiveChildLength,
                        mover.waypoints.isMovingForward,
                        mover.waypoints.canLoop,
                        isWalking,
                        isRunning,
                        isSitting,
                        sameOld
                    );

                    // Update or add the state
                    if (phaseList.Current.State.ContainsKey(npc))
                        phaseList.Current.State[npc] = state;
                    else
                        phaseList.Current.State.Add(npc, state);
                }
            }
        }
    }
    private void SaveAnimationState()
    {
        if (phaseList.Current == null) return;

        foreach (GameObject npc in allNPCs)
        {
            if (npc.activeSelf)
            {
                WaypointMover mover = npc.GetComponent<WaypointMover>();
                if (mover != null && mover.waypoints != null && phaseList.Current.State.ContainsKey(npc))
                {
                    Animator animator = npc.GetComponent<Animator>();
                    bool isWalking = animator != null && animator.GetBool("IsWalking");
                    bool isRunning = animator != null && animator.GetBool("IsRunning");
                    bool isSitting = animator != null && animator.GetBool("ToSitting");

                    var state = phaseList.Current.State[npc];
                    state.updateAnimator(isWalking, isRunning, isSitting);
                    phaseList.Current.State[npc] = state;
                }
            }
        }
    }
    private void ToggleGammaKnife(){
        if (gammaKnifeObject != null) {
            gammaKnifeObject.SetActive(!gammaKnifeObject.activeSelf);
            gammaKnifeObject.transform.GetChild(0).gameObject.SetActive(gammaKnifeObject.activeSelf);
        }
    }
    private void SpawnLLE(){
        if(LLE == null || LLE.Length == 0) LLE = GameObject.FindGameObjectsWithTag("LawEnforcement");
        foreach(GameObject unit in LLE){
            unit.transform.position = new Vector3(unit.transform.position.x, unit.transform.position.y+9000f, unit.transform.position.z);
        }
        if(LLEVehicles == null || LLEVehicles.Length == 0) LLEVehicles = GameObject.FindGameObjectsWithTag("LawEnforcementVehicle");
        foreach(GameObject vehicle in LLEVehicles){
            vehicle.transform.position = new Vector3(vehicle.transform.position.x, vehicle.transform.position.y+9000f, vehicle.transform.position.z);
        }
    }
    private void SpawnFD(){
        if (FD == null || FD.Length == 0) FD = GameObject.FindGameObjectsWithTag("FireDepartment");
        foreach(GameObject unit in FD){
            unit.transform.position = new Vector3(unit.transform.position.x, unit.transform.position.y+9000f, unit.transform.position.z);
        }
         if(FDVehicles == null || FDVehicles.Length == 0) FDVehicles = GameObject.FindGameObjectsWithTag("FireDepartmentVehicle");
        foreach(GameObject vehicle in FDVehicles){
            vehicle.transform.position = new Vector3(vehicle.transform.position.x, vehicle.transform.position.y+9000f, vehicle.transform.position.z);
        }
    }
    private void MoveNPCsForPhase(GamePhase phase){
        Debug.Log($"Moving NPCs for phase: {phase}");
        
        if (!reverting){
            SaveWaypointState();
        }
        UpdatePaths();

        switch (phase)
        {
            case GamePhase.Phase1:
                HandleStartCivs();
                SaveAnimationState();
                InitializeDiscColors();
                if(allVillains[0].transform.GetChild(1).gameObject.activeSelf) ToggleGun();
                break;
                
            case GamePhase.Phase2:
                Alarming();
                Phase2CivPaths();
                DespawnOnEscape();
                UpdateHostageDiscs();
                UpdateVillainDiscs(Color.red);
                if(!allVillains[0].transform.GetChild(1).gameObject.activeSelf) ToggleGun();
                break;
                
            case GamePhase.Phase3:
                resetAnimator(physicianHostage);
                Debug.Log("The adversaries have taken " + physicianHostage + " hostage!");
                break;
                
            case GamePhase.Phase4:
                if(reverting) {
                    HideLLE();
                    ToggleGammaKnife();
                }
                break;
            case GamePhase.Phase5:
                if(!reverting) {
                    SpawnLLE();
                    ToggleGammaKnife();
                }
                else HideFD();

                break;
            case GamePhase.Phase6:
                if(!reverting) SpawnFD();
                
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
            if (!npc.activeSelf || (npc.CompareTag("Civilians") || npc.CompareTag("Medicals"))) {
                // Debug.Log(npc + " not chosen-------------");
                continue;
            }

            WaypointMover mover = npc.GetComponent<WaypointMover>();
            if(mover.paths == null) mover.paths = mover.waypoints.transform.parent.gameObject;
            if (mover == null || mover.paths == null || mover.waypoints == null)
            {
                Debug.LogWarning($"NPC {npc.name} has missing WaypointMover or path references.");
                resetAnimator(npc);
                //
                continue;
            }
            if(mover.waypoints.ActiveChildLength < 2 && GetCurrentPhase() == GamePhase.Phase1) {
                // Debug.Log(npc + " active child length 1 or less, sitting?");
                continue;
            }

            GamePhase currentPhase = GetCurrentPhase();

            Transform pathsTransform = mover.paths.transform;
            for (int i = 0; i < pathsTransform.childCount; i++)
            {
                Transform pathTransform = pathsTransform.GetChild(i);
                Waypoints waypoints = pathTransform.GetComponent<Waypoints>();
                if(waypoints == null || waypoints.transform.childCount == 0){
                    continue;
                }else if (waypoints.getActivity() == currentPhase){
                    // Debug.Log(npc + " !---! " + waypoints + " active in current phase: " + waypoints.getActivity());
                    mover.waypoints = waypoints;
                    mover.currentWaypoint = waypoints.GetNextWaypoint(waypoints.transform.GetChild(0));
                    mover.pathidx = i;
                    if(currentPhase != GamePhase.Phase1) 
                        mover.waypoints.enableAll();

                    Animator animator = mover.GetComponent<Animator>();
                    if (animator != null)
                    {
                        animator.SetBool("IsRunning", true);
                        mover.moveSpeed = runSpeed;
                    }
                    break;
                }
                // else Debug.Log(npc + " !---! " + waypoints + " not active in current phase: " + waypoints.getActivity());
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
            animator.SetBool("IsThreatPresent", false);
        }
        
        // THEN set position and rotation (order matters)
        npc.transform.rotation = Quaternion.identity;
    }
    

    private void OnPhaseChanged(GamePhase oldPhase, GamePhase newPhase)
    {
        Debug.Log($"Phase changed from {oldPhase} to {newPhase}");

        phaseList.SetCurrentTo(newPhase);//locally updating
        StartPhase();//every client now runs startPhase together including host
    }

    [Command(requiresAuthority = false)]
    public void CmdNextPhase()
    {
        if (!isServer) return;

        if (phaseList.MoveNext())
        {
            ResetForward();
            SetPhase(phaseList.Current.Phase); //triggers OnPhaseChanged on all clients
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdPreviousPhase()
    {
        if (!isServer) return;

        if (phaseList.MovePrevious())
        {
            SetPhase(phaseList.Current.Phase); //same
        }
    }

    [Server]
    public void SetPhase(GamePhase newPhase)
    {
        syncedPhase = newPhase; //triggers hook on clients
    }

    private void OnEnable()
    {
        if (NetworkServer.active || NetworkClient.active)
        {
            Debug.Log("PhaseHandling re-enabled by Mirror");
            // Reinitialize components if needed
            if (phaseList == null){
                phaseList = new PhaseLinkedList();
                // Define the phases
                foreach (GamePhase phase in System.Enum.GetValues(typeof(GamePhase)))
                    phaseList.AddPhase(phase);
                phaseList.SetCurrentToHead();
            }
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdToggleBubble()
    {
        GameObject bubble = gammaKnifeObject.transform.GetChild(0).gameObject;
        bool newState = !bubble.activeSelf;
        bubble.SetActive(newState);
        RpcToggleBubble(newState);
    }

    [ClientRpc]
    public void RpcToggleBubble(bool state)
    {
        GameObject bubble = gammaKnifeObject.transform.GetChild(0).gameObject;
        bubble.SetActive(state);
    }

    private void OnDisable()
    {
        if (isMirrorInitialization || NetworkServer.active || NetworkClient.active)
        {
            Debug.Log("PhaseHandling disabled by Mirror (expected during network setup)");
            isMirrorInitialization = false;
            return;
        }
        Debug.LogError("PhaseHandling disabled unexpectedly!");
    }

}