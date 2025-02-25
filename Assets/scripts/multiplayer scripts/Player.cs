using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

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
    
    [SyncVar(hook = nameof(OnRoleChanged))]
    [SerializeField] private Roles playerRole;
    private void OnRoleChanged(Roles oldRole, Roles newRole)
    {
        Debug.Log($"Role changed from {oldRole} to {newRole}");
    }

    private npcMovement npcs;
    private PhaseManager phaseManager;

    private GameObject[] moveableChars; // Array of gameobjects that this player is allowed to interact with
    private List<GameObject> selectedChars = new List<GameObject>();

    [SerializeField] private Camera playerCamera; // Assign the camera in the Inspector

    public static Player LocalPlayerInstance {get; private set; }

    [Client]
    void Start()
    {
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
    }

    private void InitializeSceneObjects()
    {
        // Find npcMovement in the scene
        npcs = FindObjectOfType<npcMovement>();
        if (npcs == null)
        {
            Debug.LogError("npcMovement not found in the scene!");
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
        playerCamera.transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void HandleMovement()
    {
        // Get input for movement
        float moveX = Input.GetAxis("Horizontal"); // Strafe left/right
        float moveZ = Input.GetAxis("Vertical");   // Move forward/backward

        // Create movement vector relative to the camera's facing direction
        moveDirection = (playerCamera.transform.right * moveX + playerCamera.transform.forward * moveZ).normalized;
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
        if (Input.GetMouseButtonDown(0) && playerRole != Roles.None)
        {
            Debug.Log("Left-click detected.");

            Camera mainCamera = playerCamera;
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 2f); // Draws a debug ray in Scene View

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                GameObject hitObj = hit.collider.gameObject;
                Debug.Log($"Raycast hit: {hitObj.name} (Tag: {hitObj.tag}, Layer: {LayerMask.LayerToName(hitObj.layer)})");

                if (hitObj.tag == playerRole.ToString() || (playerRole == Roles.Instructor && hitObj.tag != "Untagged"))
                {
                    Debug.Log($"NPC {hitObj.name} selected!");

                    if (hitObj.transform.childCount > 2)
                    {
                        GameObject moveToolRing = hitObj.transform.GetChild(2).gameObject;
                        moveToolRing.SetActive(true);
                    }
                    else
                    {
                        Debug.LogWarning($"{hitObj.name} does not have a child at index 2.");
                    }

                    selectedChars.Add(hitObj);
                    return;
                }
                else
                {
                    Debug.LogWarning("NPC does not match role requirements.");
                }
            }
            else
            {
                Debug.LogWarning("Raycast did not hit any object.");
            }
        }
    }




    private void HandlePhaseManagement()
    {
        if (phaseManager == null) return;

        if (Input.GetKeyDown(KeyCode.Alpha0) && playerRole == Roles.Instructor) // Next phase
        {
            phaseManager.NextPhase();
        }

        if (Input.GetKeyDown(KeyCode.Alpha9) && playerRole == Roles.Instructor) // Previous phase
        {
            phaseManager.PreviousPhase();
        }

        if (Input.GetKeyDown(KeyCode.U)) // Undo last action
        {
            UndoLastAction();
        }
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
            npcs.AddRange(Law);

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

    private void UndoLastAction()
    {
        if (phaseManager == null) return;

        foreach (var charObj in selectedChars)
        {
            Vector3 lastPosition = phaseManager.UndoAction(playerRole.ToString());
            if (lastPosition != Vector3.zero)
            {
                Debug.Log($"Undo action for {charObj.name}. Moving to {lastPosition}");
                charObj.transform.position = lastPosition;
            }
        }
    }
    
    [Command]
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
}