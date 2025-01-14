using UnityEngine;
using System.Collections.Generic;
using PhaseLink;

public class PhaseManager : MonoBehaviour
{
    private PhaseLinkedList phaseList;
    public GameObject phaseTesterObject; // Assign in Inspector

    private Stack<Vector3> positionHistory = new Stack<Vector3>(); // Stack to store previous positions
    private bool isUndoing = false; // Flag to prevent re-executing phase movement when going backward

    private void Start()
    {
        phaseList = new PhaseLinkedList();

        // Define the phases
        phaseList.AddPhase(GamePhase.Phase1);
        phaseList.AddPhase(GamePhase.Phase2);
        phaseList.AddPhase(GamePhase.Phase3);
        phaseList.AddPhase(GamePhase.Phase4);
        phaseList.AddPhase(GamePhase.Phase5);
        phaseList.AddPhase(GamePhase.Phase6);
        phaseList.AddPhase(GamePhase.Phase7);

        // Set current phase
        phaseList.SetCurrentToHead();
        StartPhase();
    }

    private void Update()
    {
        // Move to the next phase when "1" is pressed
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            NextPhase();
        }

        // Move to the previous phase when "2" is pressed
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            PreviousPhase();
        }
    }

    private void StartPhase()
    {
        Debug.Log($"Entering Phase: {phaseList.Current.Phase}");

        // Avoid moving when undoing a phase
        if (!isUndoing)
        {
            // Push the current position to the stack before moving
            positionHistory.Push(phaseTesterObject.transform.position);

            MoveNPCsForPhase(phaseList.Current.Phase);
        }

        // Reset the undo flag
        isUndoing = false;
    }

    public void NextPhase()
    {
        if (phaseList.MoveNext())
        {
            StartPhase();
        }
        else
        {
            Debug.Log("Already at the last phase!");
        }
    }

    public void PreviousPhase()
    {
        if (phaseList.MovePrevious())
        {
            // Undo the movement: Reset the position
            if (positionHistory.Count > 0)
            {
                phaseTesterObject.transform.position = positionHistory.Pop();
            }
            else
            {
                Debug.Log("No previous positions to undo!");
            }

            // Set flag to prevent a follow-up movement
            isUndoing = true;

            StartPhase();
        }
        else
        {
            Debug.Log("Already at the first phase!");
        }
    }

    private void MoveNPCsForPhase(GamePhase phase)
    {
        Vector3 newPosition = phaseTesterObject.transform.position;

        switch (phase)
        {
            case GamePhase.Phase1:
                newPosition += Vector3.left; // Move left
                break;
            case GamePhase.Phase2:
                newPosition += Vector3.up; // Move up
                break;
            case GamePhase.Phase3:
                newPosition += Vector3.right; // Move right
                break;
            case GamePhase.Phase4:
                newPosition += Vector3.forward; // Move forward
                break;
            case GamePhase.Phase5:
                newPosition += Vector3.back; // Move back
                break;
            case GamePhase.Phase6:
                newPosition += Vector3.down; // Move down
                break;
            case GamePhase.Phase7:
                newPosition += Vector3.one; // Move diagonally
                break;
        }

        phaseTesterObject.transform.position = newPosition;
    }
}
