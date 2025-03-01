using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class playerController : MonoBehaviour
{
    public enum Roles
    {
        None,
        LawEnforcement,
        FireDepartment,
        OnSiteSecurity,
        RadiationSaftey,
        Dispatch,
        Spectator,
        Instructor
    }

    public float moveSpeed = 10f; // Horizontal movement speed
    public float verticalSpeed = 5f; // Vertical movement speed
    public float smoothingSpeed = 0.1f; // Determines how smooth the movement is

    [SerializeField] private Roles playerRole;
    public GameObject cameras;
    public npcMovement npcs;
    public PhaseManager phaseManager;

    private readonly Vector3 moveDirection = Vector3.zero;
    private readonly List<GameObject> selectedChars = new List<GameObject>();
    private cameraSwitch cameraswitch;
    private Vector3 currentVelocity = Vector3.zero; // Used for SmoothDamp

    private GameObject[] moveableChars; //Array of gameobjects that this player is allowed to interact with

    private void Start()
    {
        cameraswitch = cameras.GetComponent<cameraSwitch>();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && playerRole != Roles.None)
        {
            var mainCamera = Camera.main;

            var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit))
            {
                var hitObj = hit.collider.gameObject;
                if (hitObj.tag == playerRole.ToString() || (playerRole == Roles.Instructor && hitObj.tag != "Untagged"))
                {
                    var moveToolRing = hitObj.transform.GetChild(2).gameObject;
                    moveToolRing.SetActive(true);
                    selectedChars.Add(hitObj);
                    return;
                }
            }

            npcs.refreshCamera();
            npcs.moveFormation(selectedChars.ToArray());
        }

        if (Input.GetMouseButtonDown(1) && playerRole != Roles.None)
        {
            var mainCamera = Camera.main;

            var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit))
            {
                var hitObj = hit.collider.gameObject;
                if (hitObj.tag == playerRole.ToString() || (playerRole == Roles.Instructor && hitObj.tag != "Untagged"))
                {
                    var moveToolRing = hitObj.transform.GetChild(2).gameObject;
                    moveToolRing.SetActive(false);
                    selectedChars.Remove(hitObj);
                    return;
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha1)) cameraswitch.SwitchCamera(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) cameraswitch.SwitchCamera(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) cameraswitch.SwitchCamera(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) cameraswitch.SwitchCamera(3);

        if (Input.GetKeyDown(KeyCode.Alpha0) && playerRole == Roles.Instructor) // Next phase
            phaseManager.NextPhase();

        if (Input.GetKeyDown(KeyCode.Alpha9) && playerRole == Roles.Instructor) // Previous phase
            phaseManager.PreviousPhase();

        if (Input.GetKeyDown(KeyCode.U)) // Undo last action
            UndoLastAction();
    }

    private void FixedUpdate()
    {
        // Calculate the target velocity based on moveDirection
        var targetVelocity = transform.TransformDirection(moveDirection) * moveSpeed;
        targetVelocity.y = moveDirection.y * verticalSpeed;

        // Smoothly interpolate the current velocity towards the target velocity
        currentVelocity = Vector3.SmoothDamp(currentVelocity, targetVelocity, ref currentVelocity, smoothingSpeed);

        // Apply the smoothed velocity to the player's position
        transform.position += currentVelocity * Time.fixedDeltaTime;
    }

    private GameObject[] GetNpcs(string role)
    {
        if (role != "Instructor")
        {
            Debug.Log("Not instructor");
            return GameObject.FindGameObjectsWithTag(role);
        }

        var Fire = GameObject.FindGameObjectsWithTag("FireDepartment");
        var Law = GameObject.FindGameObjectsWithTag("LawEnforcement");

        var npcs = new List<GameObject>(Fire);
        npcs.AddRange(Law);

        return npcs.ToArray();
    }

    public void onButtonClick(Button button)
    {
        var npcRole = "";
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
        }

        moveableChars = GetNpcs(npcRole);

        //Hide UI
        var buttonUI = button.gameObject.transform.parent.gameObject;
        buttonUI.gameObject.SetActive(false);
    }

    private void UndoLastAction()
    {
        foreach (var charObj in selectedChars)
        {
            var lastPosition = phaseManager.UndoAction(playerRole.ToString());
            if (lastPosition != Vector3.zero)
            {
                Debug.Log($"Undo action for {charObj.name}. Moving to {lastPosition}");
                charObj.transform.position = lastPosition;
            }
        }
    }
}