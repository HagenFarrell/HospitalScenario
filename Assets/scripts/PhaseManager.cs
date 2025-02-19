using UnityEngine;
using System.Collections.Generic;
using PhaseLink;
using System;
using System.Collections;

public class PhaseManager : MonoBehaviour
{
    private PhaseLinkedList phaseList;
    private PhaseMovementHelper npcMove;
    private Stack<Vector3> NPCpositionHistory = new Stack<Vector3>();
    private Dictionary<GamePhase, Dictionary<string, Stack<Vector3>>> roleActionHistory; // Role-specific undo stacks per phase
    private Dictionary<GamePhase, int> roleTurnIndex; // Tracks turn indices for roles in each phase
    private bool isUndoing = false;
    
    private Coroutine currentPhaseCoroutine;

    private void Start()
    {
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

    private void StartPhase()
    {
        Debug.Log($"Entering Phase: {phaseList.Current.Phase}");
        MoveNPCsForPhase(phaseList.Current.Phase);
    }

    public void NextPhase()
    {
        // Stop any ongoing coroutines from the current phase
        if (currentPhaseCoroutine != null)
        {
            StopCoroutine(currentPhaseCoroutine);
            currentPhaseCoroutine = null;
        }

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
        // Stop any ongoing coroutines
        if (currentPhaseCoroutine != null)
        {
            StopCoroutine(currentPhaseCoroutine);
            currentPhaseCoroutine = null;
        }

        if (phaseList.MovePrevious())
        {
            Debug.Log("Moving to previous phase.");
            // Undo the movement: Reset the position using the role action history
            UndoAllActionsInCurrentPhase();
            
            // Set flag to prevent a follow-up movement
            isUndoing = true;

            StartPhase();
        }
        else
        {
            Debug.Log("Already at the first phase!");
        }
    }

    private void UndoAllActionsInCurrentPhase()
    {
        GamePhase currentPhase = phaseList.Current.Phase;
        
        if (roleActionHistory.ContainsKey(currentPhase))
        {
            foreach (var rolePair in roleActionHistory[currentPhase])
            {
                string roleName = rolePair.Key;
                Stack<Vector3> positions = rolePair.Value;
                
                if (positions.Count > 0)
                {
                    // Find the NPC by name
                    GameObject npc = GameObject.Find(roleName);
                    if (npc != null)
                    {
                        Vector3 originalPosition = positions.Pop();
                        npc.transform.position = originalPosition;
                        
                        // Reset animator if available
                        Animator animator = npc.GetComponent<Animator>();
                        if (animator != null)
                        {
                            animator.SetBool("IsWalking", false);
                        }
                        
                        // Reset AIMover state
                        AIMover mover = npc.GetComponent<AIMover>();
                        if (mover != null)
                        {
                            mover.SetTargetPosition(originalPosition);
                        }
                    }
                }
            }
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
        
        npcMove = FindObjectOfType<PhaseMovementHelper>();
        
        // // Stop any previous movement
        // if (npcMove != null && prevPhase != GamePhase.None)
        // {
        //     npcMove.StopRandomMovement(prevPhase);
        // }
        
        switch (phase)
        {
            case GamePhase.Phase1:
                // Start random movement for civilians
                if (currentPhaseCoroutine != null) {
                    StopCoroutine(currentPhaseCoroutine);
                    currentPhaseCoroutine = null;
                }
                currentPhaseCoroutine = StartCoroutine(npcMove.MoveCiviliansRandomly(GetCurrentPhase()));
                break;
            case GamePhase.Phase2:
                if (currentPhaseCoroutine != null) {
                    StopCoroutine(currentPhaseCoroutine);
                    currentPhaseCoroutine = null;
                }
                currentPhaseCoroutine = StartCoroutine(npcMove.MoveToEdgeAndDespawn());
                break;
            // Add cases for other phases as needed
        }
        
    }

    public GamePhase GetCurrentPhase(){
        return phaseList.Current.Phase;
    }
}