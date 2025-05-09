using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class playerController : MonoBehaviour
{
    [Header("Mobile Controls")]
    public CustomJoystick mobileJoystick;
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
    public npcMovement npcs;

    private GameObject[] moveableChars; //Array of gameobjects that this player is allowed to interact with
    private List<GameObject> selectedChars = new List<GameObject>();
    public PhaseManager phaseManager;

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && playerRole != Roles.None)
        {
            Camera mainCamera = Camera.main;

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                GameObject hitObj = hit.collider.gameObject;
                if (hitObj.tag == playerRole.ToString() || (playerRole == Roles.Instructor && hitObj.tag != "Untagged"))
                {
                    GameObject moveToolRing = hitObj.transform.GetChild(2).gameObject;
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
            Camera mainCamera = Camera.main;

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

        if (Input.GetKeyDown(KeyCode.Alpha0) && playerRole == Roles.Instructor) // Next phase
        {
            phaseManager.NextPhase();
        }

        if (Input.GetKeyDown(KeyCode.Alpha9) && playerRole == Roles.Instructor) // Previous phase
        {
            phaseManager.PreviousPhase();
        }




    }

    void FixedUpdate()
    {
        // Calculate the target velocity based on moveDirection
        Vector3 targetVelocity = transform.TransformDirection(moveDirection) * moveSpeed;
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
        Debug.Log("Button");
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
        }

        moveableChars = GetNpcs(npcRole);

        //Hide UI
        GameObject buttonUI = button.gameObject.transform.parent.gameObject;
        buttonUI.gameObject.SetActive(false);

    }

    public Roles getPlayerRole(){
        return playerRole;
    }

}
