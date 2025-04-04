﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using System.Linq;

public class Player : NetworkBehaviour
{

    private bool flag = true;
    public float moveSpeed = 10f; // Horizontal movement speed
    public float verticalSpeed = 5f; // Vertical movement speed
    public float mouseSensitivity = 100f; // Sensitivity for mouse look
    public float smoothingSpeed = 0.1f; // Determines how smooth the movement is

    private Vector3 moveDirection = Vector3.zero;
    private Vector3 currentVelocity = Vector3.zero; // Used for SmoothDamp
    private float yaw = 0f;
    private float pitch = 0f;
    private GameObject playerObject;
    private List<GameObject> DispatchCams;
    private AudioSource alarmNoise;
    [SyncVar] private Vector3 syncedPosition;
    private GameObject roof;

    private Vector3 lastSentPosition;
    private float lastMoveTime;
    private float ignoreServerUpdateDuration = 0.2f; // Ignore server update for 0.2s after local move

    [SerializeField] private GameObject radeyeToolPrefab; // Reference to the Radeye prefab
    private RadEyeTool radeyeToolInstance; // Holds the instantiated Radeye tool


    [SerializeField] private GameObject radeyeCircleTool;

    public enum Roles
    {
        None,
        LawEnforcement,
        FireDepartment,
        OnSiteSecurity,
        RadiationSafety,
        Dispatch,
        Spectator,
        Instructor,
    }

    public Roles getPlayerRole()
    {
        return playerRole;
    }

    [SyncVar(hook = nameof(OnRoleChanged))]
    [SerializeField] private Roles playerRole;
    private void OnRoleChanged(Roles oldRole, Roles newRole)
    {
        // Debug.Log($"Role changed from {oldRole} to {newRole}");
    }

    [SerializeField] private npcMovement npcs;
    [SerializeField] private PhaseManager phaseManager;

    private GameObject[] moveableChars; // Array of gameobjects that this player is allowed to interact with
    private List<GameObject> selectedChars = new List<GameObject>();

    [SerializeField] public Camera playerCamera; // Assign the camera in the Inspector

    public static Player LocalPlayerInstance { get; private set; }

    private uint nextRequestId = 1;
    private Dictionary<uint, MovementRequest> pendingMoves = new Dictionary<uint, MovementRequest>();

    // Struct for handling server requests with client prediction.
    private struct MovementRequest
    {
        public uint requestId;
        public uint[] npcNetIds;
        public Vector3 targetPosition;
        public float timestamp;

        // Contructor for the struct objects.
        public MovementRequest(uint requestId, uint[] npcNetIds, Vector3 targetPosition)
        {
            this.requestId = requestId;
            this.npcNetIds = npcNetIds;
            this.targetPosition = targetPosition;
            this.timestamp = Time.realtimeSinceStartup;
        }
    }

    private uint GetNextRequestId()
    {
        // Store the currentID
        uint currentId = nextRequestId;

        // Move to the next request number available.
        nextRequestId++;

        // Its possible the ID requests overflow although its less probable in our project.
        if (nextRequestId == 0)
            nextRequestId = 1;

        return currentId;
    }

    private bool isCursorConfined = true;

    void updateCursorState()
    {
        if (isCursorConfined)
        {
            Cursor.lockState = CursorLockMode.Confined;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
        }
    }

    [Client]
    void Awake()
    {
        updateCursorState();
    }

    [Client]
    void Start()
    {
        roof = GameObject.Find("Roof");
        alarmNoise = GameObject.Find("GuyWithTheSpeakers").GetComponent<AudioSource>();
        /*AudioListener audioListener = transform.GetChild(1).GetComponent<AudioListener>();

        // Disable the AudioListener on non-local players (if this is not the local player's camera)
        if (!isLocalPlayer)
        {
            audioListener.enabled = false;
        }
        else
        {
            audioListener.enabled = true;  // Ensure the local player's camera has the AudioListener
        }*/
        DispatchCams = GameObject.Find("Cameras").GetComponent<cameraSwitch>().DispatchCams;
        foreach (GameObject cam in DispatchCams)
        {
            cam.SetActive(false);
        }

        if (!isLocalPlayer)
        {
            // Debug.Log($"Player spawned at {transform.position}");

            // Disable camera for remote players
            if (playerCamera != null)
            {
                playerCamera.enabled = false;
            }
            return;
        }

        LocalPlayerInstance = this;
        // Ensure the cursor is visible and not locked
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Enable the local player's camera
        if (playerCamera != null)
        {
            playerCamera.enabled = true;
        }

        CmdSpawnRadEyeTool();


        // Find and initialize necessary objects
        InitializeSceneObjects();

        AssignButtonOnClick();

    }
    public override void OnStartClient()
    {
        base.OnStartClient();
        if (isLocalPlayer)
        {
            PhaseManager.Instance.RegisterPlayer(this);
            DriveVehicle.Instance.RegisterPlayer(this);
        }
    }

    private void AssignButtonOnClick()
    {
        // Find the buttons in the scene
        Button lawEnfButton = GameObject.Find("LawEnfButton")?.GetComponent<Button>();
        Button fireDeptButton = GameObject.Find("FireDeptButton")?.GetComponent<Button>();
        Button instructorButton = GameObject.Find("InstructorButton")?.GetComponent<Button>();
        Button dispatchButton = GameObject.Find("DispatchButton")?.GetComponent<Button>();
        Button spectatorButton = GameObject.Find("SpectatorButton")?.GetComponent<Button>();

        // Debug: Check if buttons are found
        if (lawEnfButton == null)
        {
            Debug.LogError("LawEnfButton not found!");
        }
        if (fireDeptButton == null)
        {
            Debug.LogError("FireDeptButton not found!");
        }
        if (instructorButton == null)
        {
            Debug.LogError("InstructorButton not found!");
        }

        // Assign the onClick event for each button
        if (lawEnfButton != null)
        {
            lawEnfButton.onClick.AddListener(() => LocalPlayerInstance.onButtonClick(lawEnfButton));
            // Debug.Log("LawEnfButton onClick assigned.");
        }

        if (fireDeptButton != null)
        {
            fireDeptButton.onClick.AddListener(() => LocalPlayerInstance.onButtonClick(fireDeptButton));
            // Debug.Log("FireDeptButton onClick assigned.");
        }

        if (instructorButton != null)
        {
            instructorButton.onClick.AddListener(() => LocalPlayerInstance.onButtonClick(instructorButton));
            // Debug.Log("InstructorButton onClick assigned.");
        }
        if (dispatchButton != null)
        {
            dispatchButton.onClick.AddListener(() => LocalPlayerInstance.onButtonClick(dispatchButton));
            // Debug.Log("dispatchButton onClick assigned.");
        }
        if (spectatorButton != null)
        {
            spectatorButton.onClick.AddListener(() => LocalPlayerInstance.onButtonClick(spectatorButton));
            // Debug.Log("spectatorButton onClick assigned.");
        }
    }

    private void InitializeSceneObjects()
    {
        // Find npcMovement in the scene
        npcs = FindObjectOfType<npcMovement>();
        if (npcs == null)
        {
            Debug.LogError("npcMovement not found in the scene!");
        }
        if (npcs != null)
        {
            npcs.SetCamera(playerCamera);
        }
        // Find PhaseManager in the scene
        phaseManager = FindObjectOfType<PhaseManager>();
        if (phaseManager == null)
        {
            Debug.LogError("PhaseManager not found in the scene!");
        }

        // Log success
        // Debug.Log("Scene objects initialized successfully.");
    }

    
    
    [Client]
    void Update()
    {

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            isCursorConfined = !isCursorConfined;
            updateCursorState();
        }
        
        if(!flag && Input.GetKey(KeyCode.UpArrow)) Cursor.lockState = CursorLockMode.Confined;
        
        if (!isLocalPlayer)
        {
            transform.position = new Vector3(transform.position.x, transform.position.y, transform.position.z); // Adjust height
        }
        else
        {
            // Handle mouse look
            if(playerRole != Roles.None)
                HandleMouseLook();

            // Handle movement
            HandleMovement();

            // Handle NPC selection and interaction
            HandleNPCInteraction();

            // Handle phase management (for Instructor role)
            HandlePhaseManagement();
        }

    }

    private void HandleMouseLook()
    {
        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Rotate the camera horizontally (yaw)
        yaw += mouseX;
        transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

        // Rotate the camera vertically (pitch)
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -90f, 90f); // Prevent flipping
        playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        //playerCamera.transform.parent.localRotation = Quaternion.Euler(0f, yaw, 0f);
    }

    private void HandleMovement()
    {
        // Get input for movement
        float moveX = Input.GetAxis("Horizontal"); // Strafe left/right
        float moveZ = Input.GetAxis("Vertical");   // Move forward/backward
        float moveY = Input.GetAxis("YAxis");

        // Create movement vector relative to the camera's facing direction
        moveDirection = (transform.right * moveX + transform.forward * moveZ + transform.up * moveY).normalized;
        Vector3 movement = moveDirection * moveSpeed * Time.deltaTime;


        // Apply movement locally
        //If no collision is detected OR if the role is instructor 
        if (!Physics.SphereCast(transform.position, 2f, moveDirection, out RaycastHit hit, movement.magnitude) || (hit.collider != null && hit.collider.gameObject.CompareTag("ParkingLot")) || playerRole == Roles.Instructor)
        {
                transform.position += movement; 
        }

        if (isLocalPlayer && movement.magnitude > 0.01f)
        {
            lastMoveTime = Time.time;

            if (Vector3.Distance(transform.position, lastSentPosition) > 0.05f)
            {
                lastSentPosition = transform.position;
                CmdMove(transform.position);
            }
        }

    }

    [Command]
    private void CmdMove(Vector3 newPosition)
    {
        // Server-side movement logic
        transform.position = newPosition;

        // Sync position with all clients except the local player
        RpcMove(newPosition);
    }

    [ClientRpc]
    private void RpcMove(Vector3 newPosition)
    {
        // Only update the position if this is not the local player
        if (!isLocalPlayer)
        {
            transform.position = Vector3.Lerp(transform.position, newPosition, Time.deltaTime * 10f);
        }
        else
        {
            // Skip server correction if we moved very recently (to prevent snapping)
            if (Time.time - lastMoveTime > ignoreServerUpdateDuration)
            {
                transform.position = Vector3.Lerp(transform.position, newPosition, 0.5f);
            }
        }
    }

    private void HandleNPCInteraction()
    {
        if(radeyeToolInstance != null && radeyeToolInstance.IsActive())
        {
            //// Debug.Log("NPC movement is disabled while radeye tool is active");
            return;
        }
        
        if (Input.GetMouseButtonDown(0) && playerRole != Roles.None)
        {
            // Debug.Log("Left-click detected.");

            Camera mainCamera = playerCamera;
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            //Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 2f); // Draws a debug ray in Scene View

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                GameObject hitObj = hit.collider.gameObject;
                // Debug.Log($"Raycast hit: {hitObj.name} (Tag: {hitObj.tag})");

                if (hitObj.tag == playerRole.ToString() || (playerRole == Roles.Instructor && hitObj.tag != "Untagged"))
                {
                    // Debug.Log($"NPC {hitObj.name} selected!");
                    GameObject moveToolRing = hitObj.transform.GetChild(2).gameObject;
                    moveToolRing.SetActive(true);
                    selectedChars.Add(hitObj);
                    // Debug.Log($"Added {hitObj.name} to selectedChars");
                }
                if (selectedChars.Count > 0)
                {
                    Vector3 targetPosition = hit.point;
                    uint[] npcNetIds = new uint[selectedChars.Count];

                    for (int i = 0; i < selectedChars.Count; i++)
                    {
                        NetworkIdentity identity = selectedChars[i].GetComponent<NetworkIdentity>();
                        if (identity != null)
                        {
                            npcNetIds[i] = identity.netId;
                        }
                    }
                    // Before we move, we send a request to the server. (timestamped)
                    //uint requestId = GetNextRequestId();

                    //pendingMoves[requestId] = new MovementRequest(requestId, npcNetIds, targetPosition);

                    // Move locally before sending request to server.
                    //moveNPClocally(selectedChars.ToArray(), targetPosition);

                    CmdMoveNPCs(npcNetIds, targetPosition);
                }
                else
                {
                    Debug.LogWarning("No NPCs selected, skipping moveFormation().");
                }
            }
            else
            {
                Debug.LogWarning("Raycast did not hit any object.");
            }
        }
        if (Input.GetMouseButtonDown(1) && playerRole != Roles.None)
        {
            Camera mainCamera = playerCamera;
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                GameObject hitObj = hit.collider.gameObject;
                if (hitObj.tag == playerRole.ToString() || (playerRole == Roles.Instructor && hitObj.tag != "Untagged"))
                {
                    GameObject moveToolRing = hitObj.transform.GetChild(2).gameObject;
                    moveToolRing.SetActive(false);
                    selectedChars.Remove(hitObj);
                    return;
                }
            }
        }
    }

    private void HandlePhaseManagement()
    {
        if (phaseManager == null || !isLocalPlayer || playerRole != Roles.Instructor) return;

        if (Input.GetKeyDown(KeyCode.Alpha0) && (playerRole == Roles.Instructor)) // Next phase
        {
            phaseManager.CmdNextPhase();
            //phaseManager.NextPhase();
            if (phaseManager.GetCurrentPhase() == GamePhase.Phase2) CmdSoundAlarm();
        }

        if (Input.GetKeyDown(KeyCode.Alpha9) && playerRole == Roles.Instructor) // Previous phase
        {
           phaseManager.CmdPreviousPhase();
            // phaseManager.PreviousPhase();
            if (phaseManager.GetCurrentPhase() == GamePhase.Phase2) CmdSoundAlarm();
        }

        if (Input.GetKeyDown(KeyCode.T) && playerRole == Roles.Instructor) // toggle big dome
        {
            phaseManager.CmdToggleBubble();
            //GameObject bubble = phaseManager.gammaKnifeObject.transform.GetChild(0).gameObject;
            //bubble.SetActive(!bubble.activeSelf);
             if (isLocalPlayer)
            {
                GameObject bubble = phaseManager.gammaKnifeObject.transform.GetChild(0).gameObject;
                bubble.SetActive(!bubble.activeSelf);
            }
        }

        if(Input.GetKeyDown(KeyCode.Y) && playerRole == Roles.Instructor) //toggle roof
        {
            roof.SetActive(!roof.activeInHierarchy);
        }
    }

    [Command]
    private void CmdSoundAlarm()
    {
        RpcPlayAlarm();
    }

    [ClientRpc]
    void RpcPlayAlarm()
    {
        alarmNoise.Play(); 
    }
    private GameObject[] GetNpcs(string role)
    {
        if (role != "Instructor")
        {
            // Debug.Log("Not instructor");
            return GameObject.FindGameObjectsWithTag(role);
        }
        else
        {
            GameObject[] Fire = GameObject.FindGameObjectsWithTag("FireDepartment");
            GameObject[] Law = GameObject.FindGameObjectsWithTag("LawEnforcement");

            List<GameObject> npcs = new List<GameObject>(Fire);
            if (npcs == null) Debug.LogError("npcs null");
            npcs.AddRange(Law);

            // // ensure player characters are disabled
            // foreach(GameObject npc in npcs){
            //     npc.SetActive(false);
            // }

            return npcs.ToArray();
        }
    }

    public void onButtonClick(Button button)
    {
        if (!isLocalPlayer)
        {
            Debug.LogError("onButtonClick called on a non-local player!");
            return;
        }

        // Debug: Check if the button is null
        if (button == null)
        {
            Debug.LogError("Button is null!");
            return;
        }

        // Debug: Check if the button's name is valid
        if (string.IsNullOrEmpty(button.name))
        {
            Debug.LogError("Button name is null or empty!");
            return;
        }

        // Debug: Check if the button's parent is null
        if (button.gameObject.transform.parent == null)
        {
            Debug.LogError("Button has no parent!");
            return;
        }

        string npcRole = "";
        switch (button.name)
        {
            case "LawEnfButton":
                npcRole = "LawEnforcement";
                playerRole = Roles.LawEnforcement;
                break;
            case "FireDeptButton":
                npcRole = "FireDepartment";
                playerRole = Roles.FireDepartment;
                break;
            case "InstructorButton":
                npcRole = "Instructor";
                playerRole = Roles.Instructor;
                break;
            case "DispatchButton":
                npcRole = "Dispatch";
                playerRole = Roles.Dispatch;
                ChangeDispatchView();
                break;
            case "SpectatorButton":
                npcRole = "Spectator";
                playerRole = Roles.Spectator;
                break;
            default:
                Debug.LogWarning($"Unknown button clicked: {button.name}");
                return;
        }

        CmdSetRole(playerRole);
        moveableChars = GetNpcs(npcRole);

        // Hide UI
        GameObject buttonUI = button.gameObject.transform.parent.gameObject;
        if (buttonUI != null)
        {
            buttonUI.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogError("Button UI is null!");
        }
    }

    private void ChangeDispatchView()
    {
        List<GameObject> LLEcams = GameObject.FindGameObjectsWithTag("LLE/FD Cams").ToList();
        GameObject Maincam = GameObject.FindGameObjectWithTag("MainCamera");
        Maincam.SetActive(false);
        transform.GetChild(0).gameObject.SetActive(false);
        foreach (GameObject cam in LLEcams)
        {
            cam.SetActive(false);
        }



        foreach (GameObject cam in DispatchCams)
        {
            cam.GetComponent<Camera>().enabled = true;
            cam.SetActive(true);
        }
    }

    [Command(requiresAuthority = true)]
    private void CmdSetRole(Roles role)
    {
        // Set the role on the server
        playerRole = role;

        // Sync the role with all clients
        RpcSetRole(role);
    }

    [ClientRpc]
    private void RpcSetRole(Roles role)
    {
        // Set the role on all clients
        playerRole = role;
    }

    private void MoveCircleToMousePosition()
    {
        // Get the current active camera
        Camera activeCam = playerCamera;

        // Get the mouse position in screen space
        Vector3 mousePosition = Input.mousePosition;

        // Convert the mouse position to a ray
        Ray ray = activeCam.ScreenPointToRay(mousePosition);
        RaycastHit hit;

        // LayerMask to exclude radeyeCircleTool (IgnoreRaycast layer)
        int layerMask = ~LayerMask.GetMask("IgnoreRaycast"); // Exclude the IgnoreRaycast layer

        // Perform the raycast with the layer mask
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask))
        {
            // Move the circle to the hit point
            radeyeCircleTool.transform.position = hit.point;
        }
        else
        {
            // Default position in front of the camera
            radeyeCircleTool.transform.position = ray.origin + ray.direction * 10f;
        }
    }


    [ClientRpc]
    private void RpcExecuteMovement(uint[] npcNetIds, Vector3[] targetPositions)
    {
        for (int i = 0; i < npcNetIds.Length; i++)
        {
            if (NetworkIdentity.spawned.TryGetValue(npcNetIds[i], out NetworkIdentity npcIdentity))
            {
                GameObject npc = npcIdentity.gameObject;
                AIMover mover = npcIdentity.GetComponent<AIMover>();
                if (mover != null)
                {
                    //// Debug.Log($"RpcExecuteMovement: NPC {npcIdentity.name} moving to {targetPositions[i]}");
                    mover.SetTargetPosition(targetPositions[i]);
                }
                
                if (npc.CompareTag("LawEnforcement"))
                {
                    // Set armed unit status on the server
                    mover.SetArmedUnit(true);
                }
            }
        }
    }
    

    [Command]
    private void CmdMoveNPCs(uint[] npcNetIds, Vector3 targetPosition)
    {
        if (npcNetIds == null || npcNetIds.Length == 0)
        {
            Debug.LogError("CmdMoveNPCs: No NPCs received for movement.");
            return;
        }

        //// Debug.Log($"Server: Moving {npcNetIds.Length} NPCs to {targetPosition}");

        List<GameObject> npcObjects = new List<GameObject>();

        foreach (uint netId in npcNetIds)
        {
            if (NetworkIdentity.spawned.TryGetValue(netId, out NetworkIdentity identity))
            {
                npcObjects.Add(identity.gameObject);
            }
            else
            {
                Debug.LogWarning($"CmdMoveNPCs: NetworkIdentity with netId {netId} not found on server.");
            }
        }

        if (npcObjects.Count > 0)
        {
            // Debug.Log($"Server: Moving {npcObjects.Count} NPCs");

            // Instead of looking for npcMovement on the NPC, find it on the parent object
            npcMovement movementScript = FindObjectOfType<npcMovement>();
            if (movementScript != null)
            {
                Vector3[] npcPositions = new Vector3[npcObjects.Count];  //pass formation instead of single targetPosition
                for (int i = 0; i < npcObjects.Count; i++)
                {
                    npcPositions[i] = movementScript.ComputeTriangleSlot(i, targetPosition, Vector3.forward, 1.0f, 1.0f);
                }

                // move each NPC to assigned position
                for (int i = 0; i < npcObjects.Count; i++)
                {
                    AIMover mover = npcObjects[i].GetComponent<AIMover>();
                    
                    if (npcObjects[i].CompareTag("LawEnforcement"))
                    {
                        // Set armed unit status on the server
                        mover.SetArmedUnit(true);
                    }
                    
                    if (mover != null)
                    {
                        // Debug.Log($"Setting NPC {npcObjects[i].name} to move to: {npcPositions[i]}");
                        Vector3 adjustedPosition = npcPositions[i] + new Vector3(Random.Range(-0.5f, 0.5f), 0, Random.Range(-0.5f, 0.5f));
                        mover.SetTargetPosition(adjustedPosition);
                        // Debug.Log($"NPC {npcObjects[i].name} final movement position: {adjustedPosition}");

                    }
                }
                RpcExecuteMovement(npcNetIds, npcPositions);
            }
            else
            {
                Debug.LogError("npcMovement script is missing on the NPC parent object!");
            }
        }
    }

    [Command]
    private void CmdSpawnRadEyeTool()
    {
        if (radeyeToolPrefab == null)
        {
            Debug.LogError("RadEyeTool prefab is not assigned in Player!");
            return;
        }

        GameObject toolInstance = Instantiate(radeyeToolPrefab, transform.position, Quaternion.identity);
        NetworkServer.Spawn(toolInstance, connectionToClient);

        // Pass this player's netId to the client to attach the tool correctly
        TargetAttachRadEyeTool(connectionToClient, toolInstance, netId);
    }



    [TargetRpc]
    private void TargetAttachRadEyeTool(NetworkConnection target, GameObject toolInstance, uint ownerNetId)
    {
        radeyeToolInstance = toolInstance.GetComponent<RadEyeTool>();
        if (radeyeToolInstance == null)
        {
            Debug.LogError("Failed to find RadEyeTool script on spawned object!");
            return;
        }

        radeyeToolInstance.AssignPlayer(ownerNetId); // <-- important!

        // Attach the tool to this player
        Vector3 adjustedPosition = transform.position + new Vector3(1.0f, -0.5f, 0.5f);
        radeyeToolInstance.transform.SetParent(transform, true);
        radeyeToolInstance.transform.position = adjustedPosition;
        radeyeToolInstance.transform.localRotation = Quaternion.Euler(-30, 180, 0);

        // Debug.Log("RadEyeTool successfully attached to local player.");
    }

    public List<GameObject> GetSelectedChars()
    {
        return selectedChars;
    }
}