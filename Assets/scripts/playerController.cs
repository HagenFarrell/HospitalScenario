using System.Collections.Generic;
using UnityEngine;

public class playerController : MonoBehaviour
{
    public float moveSpeed = 10f; // Horizontal movement speed
    public float verticalSpeed = 5f; // Vertical movement speed
    public float smoothingSpeed = 0.1f; // Determines how smooth the movement is

    private Vector3 moveDirection = Vector3.zero;
    private Vector3 currentVelocity = Vector3.zero; // Used for SmoothDamp
    public enum Roles
    {
        None,
        LawEnforcement,
        FireDepartment,
        OnSiteSecurity,
        RadiationSaftey,
        Dispatch,
        Spectator,
        Instructor,
    }

    [SerializeField] private Roles playerRole;
    // public GameObject cameras;
    public npcMovement npcs;
    public PhaseManager phaseManager;
    private cameraSwitch cameraswitch;

    private GameObject[] moveableChars; // Array of gameobjects that this player is allowed to interact with
    private List<GameObject> selectedChars = new List<GameObject>();
    private int currentCharIndex = 0;

    private void Start()
    {
        // cameraswitch = cameras.GetComponent<cameraSwitch>();
        Debug.Log($"This script is attached to: {gameObject.name}");
    }

    void Update()
    {
        HandleRoleSelection(); // Use keybinds to assign roles

        if (playerRole == Roles.Instructor)
        {
            HandleInstructorControls();
        }
        else
        {
            HandlePlayerControls();
        }

        // if (Input.GetKeyDown(KeyCode.Alpha1)) cameraswitch.SwitchCamera(0);
        // if (Input.GetKeyDown(KeyCode.Alpha2)) cameraswitch.SwitchCamera(1);
        // if (Input.GetKeyDown(KeyCode.Alpha3)) cameraswitch.SwitchCamera(2);
        // if (Input.GetKeyDown(KeyCode.Alpha4)) cameraswitch.SwitchCamera(3);

        if (Input.GetKeyDown(KeyCode.U)) // Undo last action
        {
            UndoLastAction();
        }
    }
    
    private void HandleRoleSelection()
    {
        if (Input.GetKeyDown(KeyCode.L)) // Law Enforcement
        {
            SetRole(Roles.LawEnforcement, "LawEnforcement");
        }
        if (Input.GetKeyDown(KeyCode.F)) // Fire Department
        {
            SetRole(Roles.FireDepartment, "FireDepartment");
        }
        if (Input.GetKeyDown(KeyCode.I)) // Instructor
        {
            SetRole(Roles.Instructor, "Instructor");
        }
    }

    private void SetRole(Roles role, string roleTag)
    {
        if(role != playerRole){
            playerRole = role;
            moveableChars = GetNpcs(roleTag);
            Debug.Log($"Role set to: {role}. Movable characters: {moveableChars.Length}");
        }
    }

    private void HandleInstructorControls()
    {
        if (Input.GetMouseButtonDown(0))
        {
            SelectCharacter();
        }

        if (Input.GetMouseButtonDown(1))
        {
            DeselectCharacter();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            MoveSelectedCharacters();
        }

        if (Input.GetKeyDown(KeyCode.Alpha0)) // Next phase
        {
            phaseManager.NextPhase();
        }

        if (Input.GetKeyDown(KeyCode.Alpha9)) // Previous phase
        {
            phaseManager.PreviousPhase();
        }
    }

    private void HandlePlayerControls()
    {
        if (Input.GetMouseButtonDown(0))
        {
            SelectCharacter();
        }

        if (Input.GetMouseButtonDown(1))
        {
            DeselectCharacter();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            MoveSelectedCharacters();
        }
    }

    private void SelectCharacter()
    {
        Camera mainCamera = Camera.main;
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            GameObject hitObj = hit.collider.gameObject;
            if (hitObj.tag == playerRole.ToString() || playerRole == Roles.Instructor)
            {
                Debug.Log($"Selected character: {hitObj.name}");
                GameObject moveToolRing = hitObj.transform.GetChild(2).gameObject;
                moveToolRing.SetActive(true);
                selectedChars.Add(hitObj);
            }
            else
            {
                Debug.Log($"Cannot interact with {hitObj.name}. Wrong role or invalid selection.");
            }
        }
    }

    private void DeselectCharacter()
    {
        Camera mainCamera = Camera.main;
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            GameObject hitObj = hit.collider.gameObject;
            if (hitObj.tag == playerRole.ToString() || playerRole == Roles.Instructor)
            {
                Debug.Log($"Deselected character: {hitObj.name}");
                GameObject moveToolRing = hitObj.transform.GetChild(2).gameObject;
                moveToolRing.SetActive(false);
                selectedChars.Remove(hitObj);
            }
        }
    }

    private void MoveSelectedCharacters()
    {
        npcs.refreshCamera();
        npcs.moveNpc(selectedChars.ToArray());
        foreach (var charObj in selectedChars)
        {
            phaseManager.LogAction(playerRole.ToString(), charObj.transform.position);
        }
    }

    private void UndoLastAction()
    {
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

    private GameObject[] GetNpcs(string role)
    {
        if (role != "Instructor")
        {
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
}