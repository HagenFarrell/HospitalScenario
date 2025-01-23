using UnityEngine;
using System.Collections.Generic;
using PhaseLink;

public class PhaseManager : MonoBehaviour
{
    private PhaseLinkedList phaseList;
    public GameObject phaseTesterObject; // Assign in Inspector

    // Each phase stores stacks for roles and their actions
    private Dictionary<GamePhase, Dictionary<string, Stack<Vector3>>> roleActionStacks;

    private bool isUndoing = false; // prevent re-executing phase movement when undoing

    private void Start()
    {
        phaseList = new PhaseLinkedList();
        roleActionStacks = new Dictionary<GamePhase, Dictionary<string, Stack<Vector3>>>();

        // Define the phases and initialize their stacks
        foreach (GamePhase phase in System.Enum.GetValues(typeof(GamePhase)))
        {
            phaseList.AddPhase(phase);
            roleActionStacks[phase] = new Dictionary<string, Stack<Vector3>>();
        }

        // Set current phase
        phaseList.SetCurrentToHead();
        StartPhase();
    }

    private void StartPhase()
    {
        Debug.Log($"Entering Phase: {phaseList.Current.Phase}");

        if (!isUndoing)
        {
            MoveNPCsForPhase(phaseList.Current.Phase);
        }

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
            isUndoing = true;
            StartPhase();
        }
        else
        {
            Debug.Log("Already at the first phase!");
        }
    }

    public void RecordAction(GamePhase phase, string role, Vector3 position)
    {
        if (!roleActionStacks[phase].ContainsKey(role))
        {
            roleActionStacks[phase][role] = new Stack<Vector3>();
        }

        roleActionStacks[phase][role].Push(position);
    }

    public Vector3 UndoAction(GamePhase phase, string role)
    {
        if (roleActionStacks[phase].ContainsKey(role) && roleActionStacks[phase][role].Count > 0)
        {
            return roleActionStacks[phase][role].Pop();
        }

        Debug.LogWarning($"No actions to undo for role {role} in phase {phase}");
        return Vector3.zero;
    }

    private void MoveNPCsForPhase(GamePhase phase)
    {
        // Logic to handle NPC movements for the phase
        Debug.Log($"Moving NPCs for {phase}");
    }

    public GamePhase CurrentPhase
    {
        get
        {
            return phaseList.Current.Phase;
        }
    }

}
