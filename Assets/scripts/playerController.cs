using System.Collections.Generic;
using UnityEditorInternal;
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
    public GameObject cameras;
    public npcMovement npcs;
    private cameraSwitch cameraswitch;

    private GameObject[] moveableChars; //Array of gameobjects that this player is allowed to interact with
    private List<GameObject> selectedChars = new List<GameObject>();

    private void Start()
    {
        cameraswitch = cameras.GetComponent<cameraSwitch>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && playerRole != Roles.None)
        {
            Camera mainCamera = Camera.main;
            
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                GameObject hitObj = hit.collider.gameObject;
                if (hitObj.tag == "FireDepartment")
                {
                    GameObject moveToolRing = hitObj.transform.GetChild(2).gameObject;
                    moveToolRing.SetActive(true);
                    selectedChars.Add(hitObj);
                    return;
                }
                
            }

            npcs.refreshCamera();
            npcs.moveNpc(selectedChars.ToArray());
        }
        if (Input.GetKeyDown(KeyCode.Alpha1)) cameraswitch.SwitchCamera(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) cameraswitch.SwitchCamera(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) cameraswitch.SwitchCamera(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) cameraswitch.SwitchCamera(3);

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
        GameObject[] npcs = GameObject.FindGameObjectsWithTag(role);

        return npcs;
    }
    
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
        }

        moveableChars = GetNpcs(npcRole);

        //Hide UI
        GameObject buttonUI = button.gameObject.transform.parent.gameObject;
        buttonUI.gameObject.SetActive(false);

    } 
}
