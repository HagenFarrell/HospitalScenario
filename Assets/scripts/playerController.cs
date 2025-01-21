using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 10f; // Movement speed
    public float verticalSpeed = 5f; // Vertical movement speed
    private Vector3 moveDirection = Vector3.zero;

    public RoleManager.Roles PlayerRole { get; private set; } // Player's role

    private bool isControlEnabled = false; // Can the player control their character?

    public void EnableControl(bool enable)
    {
        isControlEnabled = enable;
    }

    private void Update()
    {
        if (!isControlEnabled) return;

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        moveDirection = new Vector3(horizontal, 0, vertical).normalized;

        if (Input.GetKey(KeyCode.Space))
        {
            moveDirection.y = 1f;
        }
        if (Input.GetMouseButtonDown(1) && playerRole != Roles.None)
        {
            moveDirection.y = -1f;
        }
        if (Input.GetKeyDown(KeyCode.Alpha1)) cameraswitch.SwitchCamera(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) cameraswitch.SwitchCamera(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) cameraswitch.SwitchCamera(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) cameraswitch.SwitchCamera(3);

        if (Input.GetKeyDown(KeyCode.Alpha0) && playerRole == Roles.Instructor) // Next phase
        {
            moveDirection.y = 0f;
        }

        if (Input.GetKeyDown(KeyCode.Alpha3)) // Undo action
        {
            UndoLastAction();
        }

        if (Input.GetKeyDown(KeyCode.Alpha4)) // End turn
        {
            EndTurn();
        }
    }

    private void FixedUpdate()
    {
        if (!isControlEnabled) return;

        Vector3 targetVelocity = transform.TransformDirection(moveDirection) * moveSpeed;
        targetVelocity.y = moveDirection.y * verticalSpeed;

        transform.position += targetVelocity * Time.fixedDeltaTime;
    }

    private void UndoLastAction()
    {
        Debug.Log($"{PlayerRole}: Undo last action!");
        // This will be managed by RoleManager (future expansion)
    }

    private void EndTurn()
    {
        Debug.Log($"{PlayerRole}: End turn!");
        FindObjectOfType<RoleManager>().EndRoleTurn(this);
    }
}
