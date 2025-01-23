using System.Collections.Generic;
using UnityEngine;

public class playerController : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float verticalSpeed = 5f;
    public float smoothingSpeed = 0.1f;

    private Vector3 moveDirection = Vector3.zero;
    private Vector3 currentVelocity = Vector3.zero;

    public enum Roles
    {
        None,
        LawEnforcement,
        FireDepartment,
        OnSiteSecurity,
        RadiationSafety,
        Dispatch,
        Spectator,
        Instructor
    }

    [SerializeField] private Roles playerRole;
    public GameObject cameras;
    public npcMovement npcs;
    private cameraSwitch cameraSwitch;

    private GameObject[] moveableChars;
    private int charTurnIndex = 0; // Tracks which character's turn it is
    private Stack<Vector3> charActionHistory = new Stack<Vector3>(); // Stores character actions for undoing

    private void Start()
    {
        cameraSwitch = cameras.GetComponent<cameraSwitch>();
    }

    private void Update()
    {
        if (playerRole == Roles.None || playerRole == Roles.Instructor)
            return;

        if (Input.GetKeyDown(KeyCode.U))
        {
            UndoLastAction();
        }

        if (Input.GetKeyDown(KeyCode.Space)) // Select next character in turn order
        {
            charTurnIndex = (charTurnIndex + 1) % moveableChars.Length;
            Debug.Log($"Selected {moveableChars[charTurnIndex].name}");
        }

        if (Input.GetMouseButtonDown(0)) // Perform action
        {
            PerformAction();
        }
    }

    private void PerformAction()
    {
        GameObject activeChar = moveableChars[charTurnIndex];

        Vector3 originalPosition = activeChar.transform.position;
        Vector3 newPosition = originalPosition + Vector3.forward; // Example: move forward
        activeChar.transform.position = newPosition;

        charActionHistory.Push(originalPosition); // Log action for undo
        Debug.Log($"{activeChar.name} moved to {newPosition}");
    }

    private void UndoLastAction()
    {
        if (charActionHistory.Count > 0)
        {
            Vector3 lastPosition = charActionHistory.Pop();
            moveableChars[charTurnIndex].transform.position = lastPosition;
            Debug.Log($"Undo: Moved {moveableChars[charTurnIndex].name} back to {lastPosition}");
        }
        else
        {
            Debug.Log("No actions to undo!");
        }
    }

    public void AssignRoleCharacters(GameObject[] characters)
    {
        moveableChars = characters;
        charTurnIndex = 0;
    }
}
