using System.Data.Common;
using UnityEngine;
using UnityEngine.UI;

public class playerController : MonoBehaviour
{
    public float moveSpeed = 10f; // Horizontal movement speed
    public float verticalSpeed = 5f; // Vertical movement speed
    public float smoothingSpeed = 0.1f; // Determines how smooth the movement is

    private Vector3 moveDirection = Vector3.zero;
    private Vector3 currentVelocity = Vector3.zero; // Used for SmoothDamp
    public enum Roles
    {
        LawEnforcement,
        FireDepartment,
        OnSiteSecurity,
        RadiationSaftey,
        Dispatch,
        Spectator,
        Instructor,
    }

    [SerializeField] private Roles playerRole;

    private GameObject[] moveableChars; //Array of gameobjects that this player is allowed to interact with

    void Update()
    {
        // Get WASD or arrow key input for horizontal and vertical movement
        float horizontal = Input.GetAxis("Horizontal"); // A/D or Left/Right Arrows
        float vertical = Input.GetAxis("Vertical"); // W/S or Up/Down Arrows

        // Calculate movement direction
        moveDirection = new Vector3(horizontal, 0, vertical).normalized;

        // Check for upward or downward movement with Space/Shift
        if (Input.GetKey(KeyCode.Space))
        {
            moveDirection.y = 1f; // Move upward
        }
        else if (Input.GetKey(KeyCode.LeftShift))
        {
            moveDirection.y = -1f; // Move downward
        }
        else
        {
            moveDirection.y = 0f; // No vertical movement
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


    //Helper function to get array of npcs that match the player's role
    private GameObject[] GetNpcs(string role)
    {

        if(role == "" )return null;
        GameObject[] npcs = GameObject.FindGameObjectsWithTag(role);

        return npcs;
    }


    //Button evevent runs reguardless of if script is enabled or not
    public void onButtonClick(Button button)
    {
        string npcRole = "";
        switch (button.name) {

            case "LawEnfButton":
                npcRole = "LawEnforcement";
                playerRole = Roles.LawEnforcement;
                break;
            case "FireDeptButton":
                npcRole = "FireDepartment";
                playerRole = Roles.FireDepartment;
                break;
            case "InstructorButton":
                //No npcRole because instructor is an exception to the rule and can move all npcs
                playerRole = Roles.Instructor;
                break;
        }

        moveableChars = GetNpcs(npcRole);

        for(int i = 0; i < moveableChars.Length; i++)
        {
            if (moveableChars[i] != null)
            {
                Debug.Log(moveableChars[i].name + ": " + moveableChars[i].tag);
            }
        }

        //Hide UI
        GameObject buttonUI = button.gameObject.transform.parent.gameObject;
        buttonUI.gameObject.SetActive(false);

        //Activate Camera
        //TODO: Enable Camera Movement only for instructor (Waiting for a more specific implementation of camera position)
        playerController playerController = transform.GetComponent<playerController>();
        playerController.enabled = true;
        GameObject Camera = transform.Find("Camera").gameObject;
        CameraLook CameraLook = Camera.GetComponent<CameraLook>();
        CameraLook.enabled = true;
    }
}
