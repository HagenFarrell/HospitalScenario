using UnityEngine;
using System.Collections.Generic;
using PhaseLink;

public class PhaseManager : MonoBehaviour
{
    private PhaseLinkedList phaseList;
    public GameObject phaseTesterObject;
    private Stack<Vector3> NPCpositionHistory = new Stack<Vector3>();
    private Dictionary<GamePhase, Dictionary<string, Stack<Vector3>>> roleActionHistory; // Role-specific undo stacks per phase
    private Dictionary<GamePhase, int> roleTurnIndex; // Tracks turn indices for roles in each phase
    private bool isUndoing = false;

    private void Start()
    {
        if (phaseTesterObject == null)
        {
            phaseTesterObject = GameObject.Find("Robber");
        }

        phaseList = new PhaseLinkedList();
        roleActionHistory = new Dictionary<GamePhase, Dictionary<string, Stack<Vector3>>>();
        roleTurnIndex = new Dictionary<GamePhase, int>();

        // Define the phases
        foreach (GamePhase phase in System.Enum.GetValues(typeof(GamePhase)))
        {
            phaseList.AddPhase(phase);
            roleActionHistory[phase] = new Dictionary<string, Stack<Vector3>>();
            roleTurnIndex[phase] = 0; // Initialize turn index
        }

        phaseList.SetCurrentToHead();
        StartPhase();
    }

    private void Update()
    {
        // if (Input.GetKeyDown(KeyCode.Alpha0)) // Next phase
        // {
        //     NextPhase();
        // }

        // if (Input.GetKeyDown(KeyCode.Alpha9)) // Previous phase
        // {
        //     PreviousPhase();
        // }
    }

    private void StartPhase()
    {
        Debug.Log($"Entering Phase: {phaseList.Current.Phase}");

        if (!isUndoing)
        {
            NPCpositionHistory.Push(phaseTesterObject.transform.position);
            MoveNPCsForPhase(phaseList.Current.Phase);
        }

        isUndoing = false;
    }

    public void NextPhase()
    {
        if (phaseList.MoveNext())
        {
            Debug.Log("Moving to next phase.");
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
            Debug.Log("Moving to previous phase.");
            // Undo the movement: Reset the position
            if (NPCpositionHistory.Count > 0)
            {
                phaseTesterObject.transform.position = NPCpositionHistory.Pop();
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

    public void LogAction(string role, Vector3 position)
    {
        GamePhase currentPhase = phaseList.Current.Phase;

        if (!roleActionHistory[currentPhase].ContainsKey(role))
        {
            roleActionHistory[currentPhase][role] = new Stack<Vector3>();
        }

        roleActionHistory[currentPhase][role].Push(position);
    }

    public Vector3 UndoAction(string role)
    {
        GamePhase currentPhase = phaseList.Current.Phase;

        if (roleActionHistory[currentPhase].ContainsKey(role) && roleActionHistory[currentPhase][role].Count > 0)
        {
            return roleActionHistory[currentPhase][role].Pop();
        }
        else
        {
            Debug.Log($"{role} has no actions to undo!");
            return Vector3.zero; // Default no undo position
        }
    }

    private void MoveNPCsForPhase(GamePhase phase)
    {
        Debug.Log($"Moving NPCs for phase: {phase}");
        // Static movement for testing
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