using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using System.Linq;

public class Player : NetworkBehaviour
{
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

    [SerializeField] private GameObject radeyeToolPrefab;
    private RadEyeTool radeyeToolInstance;


    

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

    public Roles getPlayerRole(){
        return playerRole;
    }
    
    [SyncVar(hook = nameof(OnRoleChanged))]
    [SerializeField] private Roles playerRole;
    private void OnRoleChanged(Roles oldRole, Roles newRole)
    {
        Debug.Log($"Role changed from {oldRole} to {newRole}");
    }

    [SerializeField] private npcMovement npcs;
    [SerializeField] private PhaseManager phaseManager;

    private GameObject[] moveableChars; // Array of gameobjects that this player is allowed to interact with
    private List<GameObject> selectedChars = new List<GameObject>();

    [SerializeField] private Camera playerCamera; // Assign the camera in the Inspector

    public static Player LocalPlayerInstance {get; private set; }

    [Client]
    void Start()
    {
        alarmNoise = GetComponent<AudioSource>();
        // Try to find an AudioListener in this object or its children
        AudioListener audioListener = GetComponentInChildren<AudioListener>();

        if (audioListener == null)
        {
            Debug.LogWarning("No AudioListener found in Player. Skipping AudioListener assignment.");
        }
        else
        {
            Debug.Log("AudioListener found!");

            // Disable the AudioListener on non-local players
            if (!isLocalPlayer)
            {
                audioListener.enabled = false;
            }
            else
            {
                audioListener.enabled = true;  // Ensure the local player's camera has the AudioListener
            }
        }

    Debug.Log($"Player initialized. Current child count: {transform.childCount}");

        DispatchCams = GameObject.Find("Cameras").GetComponent<cameraSwitch>().DispatchCams;
        foreach(GameObject cam in DispatchCams)
        {
            cam.SetActive(false);
        }

        if (!isLocalPlayer)
        {
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


    private void AssignButtonOnClick()
    {
        // Find the buttons in the scene
        Button lawEnfButton = GameObject.Find("LawEnfButton")?.GetComponent<Button>();
        Button fireDeptButton = GameObject.Find("FireDeptButton")?.GetComponent<Button>();
        Button instructorButton = GameObject.Find("InstructorButton")?.GetComponent<Button>();
        Button dispatchButton = GameObject.Find("DispatchButton")?.GetComponent<Button>();

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
            Debug.Log("LawEnfButton onClick assigned.");
        }

        if (fireDeptButton != null)
        {
            fireDeptButton.onClick.AddListener(() => LocalPlayerInstance.onButtonClick(fireDeptButton));
            Debug.Log("FireDeptButton onClick assigned.");
        }

        if (instructorButton != null)
        {
            instructorButton.onClick.AddListener(() => LocalPlayerInstance.onButtonClick(instructorButton));
            Debug.Log("InstructorButton onClick assigned.");
        }
        if (dispatchButton != null)
        {
            dispatchButton.onClick.AddListener(() => LocalPlayerInstance.onButtonClick(dispatchButton));
            Debug.Log("dispatchButton onClick assigned.");
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
        if( npcs != null)
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
        Debug.Log("Scene objects initialized successfully.");
    }

    [Client]
    void Update()
    {
        if (!isLocalPlayer) return;

        // Handle mouse look
        HandleMouseLook();

        // Handle movement
        HandleMovement();

        // Handle NPC selection and interaction
        HandleNPCInteraction();

        // Handle phase management (for Instructor role)
        HandlePhaseManagement();

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
        transform.position += movement;

        // Send movement request to server
        CmdMove(transform.position);
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
            transform.position = newPosition;
        }
    }

    private void HandleNPCInteraction()
    {
        if(radeyeToolInstance != null && radeyeToolInstance.IsActive())
        {
            // Debug.Log("NPC movement is disabled while Radeye tool is active");
            return;
        }
        
        if (Input.GetMouseButtonDown(0) && playerRole != Roles.None)
        {
            Debug.Log("Left-click detected.");

            Camera mainCamera = playerCamera;
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            //Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 2f); // Draws a debug ray in Scene View

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                GameObject hitObj = hit.collider.gameObject;
                Debug.Log($"Raycast hit: {hitObj.name} (Tag: {hitObj.tag})");

                if (hitObj.tag == playerRole.ToString() || (playerRole == Roles.Instructor && hitObj.tag != "Untagged"))
                {
                    Debug.Log($"NPC {hitObj.name} selected!");
                    GameObject moveToolRing = hitObj.transform.GetChild(2).gameObject;
                    moveToolRing.SetActive(true);
                    selectedChars.Add(hitObj);
                    Debug.Log($"Added {hitObj.name} to selectedChars");

                    /*npcs.moveFormation(selectedChars.ToArray());

                    Vector3[] npcPositions = new Vector3[selectedChars.Count];
                    for(int i = 0; i<selectedChars.Count;i++)
                    {
                        npcPositions[i] = selectedChars[i].transform.position;
                    }
                    CmdMoveNPCs(npcPositions);
                    return;*/ 
                }
                if(selectedChars.Count > 0)
                {
                    Debug.Log("Calling CmdMoveNPCs with selected npcs");
                    //npcs.moveFormation(selectedChars.ToArray());
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
            if(Physics.Raycast(ray, out RaycastHit hit))
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
        if (phaseManager == null) return;

        if (Input.GetKeyDown(KeyCode.Alpha0) && playerRole == Roles.Instructor) // Next phase
        {
            phaseManager.NextPhase();
            if (phaseManager.currentPhase == 2) SoundAlarm();
        }

        if (Input.GetKeyDown(KeyCode.Alpha9) && playerRole == Roles.Instructor) // Previous phase
        {
            phaseManager.PreviousPhase();
            if (phaseManager.currentPhase == 2) SoundAlarm();
        }

        if (Input.GetKeyDown(KeyCode.U)) // Undo last action
        {
            UndoLastAction();
        }

        
    }

    private void SoundAlarm()
    {
        if(playerRole == Roles.Dispatch || playerRole == Roles.Instructor)
            alarmNoise.Play();
    }
    private GameObject[] GetNpcs(string role)
    {
        if (role != "Instructor")
        {
            Debug.Log("Not instructor");
            return GameObject.FindGameObjectsWithTag(role);
        }
        else
        {
            GameObject[] Fire = GameObject.FindGameObjectsWithTag("FireDepartment");
            GameObject[] Law = GameObject.FindGameObjectsWithTag("LawEnforcement");

            List<GameObject> npcs = new List<GameObject>(Fire);
            if(npcs == null) Debug.LogError("npcs null");
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
        transform.GetChild(1).gameObject.SetActive(false);
        foreach(GameObject cam in LLEcams)
        {
            cam.SetActive(false);
        }

        

        foreach(GameObject cam in DispatchCams)
        {
            cam.GetComponent<Camera>().enabled = true;
            cam.SetActive(true);
        }
    }
    private void UndoLastAction()
    {
        // if (phaseManager == null) return;

        // foreach (var charObj in selectedChars)
        // {
        //     Vector3 lastPosition = phaseManager.UndoAction(playerRole.ToString());
        //     if (lastPosition != Vector3.zero)
        //     {
        //         Debug.Log($"Undo action for {charObj.name}. Moving to {lastPosition}");
        //         charObj.transform.position = lastPosition;
        //     }
        // }
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


    [Command]
    private void CmdMoveNPCs(uint[] npcNetIds, Vector3 targetPosition)
    {
        if (npcNetIds == null || npcNetIds.Length == 0)
        {
            Debug.LogError("CmdMoveNPCs: No NPCs received for movement.");
            return;
        }

        Debug.Log($"Server: Moving {npcNetIds.Length} NPCs to {targetPosition}");

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
            Debug.Log($"Server: Moving {npcObjects.Count} NPCs");

            // Instead of looking for npcMovement on the NPC, find it on the parent object
            npcMovement movementScript = FindObjectOfType<npcMovement>();
            if (movementScript != null)
            {
                movementScript.moveFormation(npcObjects.ToArray());
            }
            else
            {
                Debug.LogError("npcMovement script is missing on the NPC parent object!");
            }

            // Send the update to clients
            RpcMoveNPCs(npcNetIds, targetPosition);
        }
    }

    [ClientRpc]
    private void RpcMoveNPCs(uint[] npcNetIds, Vector3 targetPosition)
    {
        if (npcNetIds == null || npcNetIds.Length == 0)
        {
            Debug.LogError("RpcMoveNPCs: No NPCs received for movement.");
            return;
        }

        Debug.Log("Client: Updating NPCs positions");

        List<GameObject> npcObjects = new List<GameObject>();

        foreach (uint netId in npcNetIds)
        {
            if (NetworkIdentity.spawned.TryGetValue(netId, out NetworkIdentity identity))
            {
                npcObjects.Add(identity.gameObject);
            }
            else
            {
                Debug.LogWarning($"RpcMoveNPCs: NetworkIdentity with netId {netId} not found on client.");
            }
        }

        if (npcObjects.Count > 0)
        {
            Debug.Log($"Client: Moving {npcObjects.Count} NPCs");

            // Find the parent object with npcMovement
            npcMovement movementScript = FindObjectOfType<npcMovement>();
            if (movementScript != null)
            {
                movementScript.moveFormation(npcObjects.ToArray());
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
        RpcAttachRadEyeTool(toolInstance);
    }


    [ClientRpc]
    private void RpcAttachRadEyeTool(GameObject toolInstance)
    {
        if (!isLocalPlayer) return;

        radeyeToolInstance = toolInstance.GetComponent<RadEyeTool>();
        if (radeyeToolInstance == null)
        {
            Debug.LogError("Failed to find RadEyeTool script on spawned object!");
            return;
        }

        // Store world position before parenting
        Vector3 adjustedPosition = transform.position + new Vector3(1.0f, -0.5f, 0.5f);

        // Attach the tool to the player safely
        radeyeToolInstance.transform.SetParent(transform, true); 

        // Set world position after parenting to avoid reset
        radeyeToolInstance.transform.position = adjustedPosition;

        // Adjust the rotation to face a specific direction
        radeyeToolInstance.transform.localRotation = Quaternion.Euler(-30, 180, 0); // Adjust rotation as needed

        Debug.Log("RadEyeTool successfully attached to player with adjusted position and rotation.");
    }



}