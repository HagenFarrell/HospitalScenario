using UnityEngine;
using System.Collections.Generic;
using PhaseLink;
using System;
using System.Collections;


public class PhaseManager : MonoBehaviour
{
    
    private PhaseLinkedList phaseList;
    private npcMovement npcMove;
    private Stack<Vector3> NPCpositionHistory = new Stack<Vector3>();
    private Dictionary<GamePhase, Dictionary<string, Stack<Vector3>>> roleActionHistory; // Role-specific undo stacks per phase
    private Dictionary<GamePhase, int> roleTurnIndex; // Tracks turn indices for roles in each phase
    private bool isUndoing = false;

    private void Start()
    {
        // if (phaseTesterObject == null)
        // {
        //     phaseTesterObject = GameObject.Find("Robber");
        // }

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
            // NPCpositionHistory.Push(phaseTesterObject.transform.position);
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
                // phaseTesterObject.transform.position = NPCpositionHistory.Pop();
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
        
        GameObject[] npcs = GameObject.FindGameObjectsWithTag("Civilians");
        npcMove = FindObjectOfType<npcMovement>();

        switch (phase)
        {
            case GamePhase.Phase1:
                StartCoroutine(npcMove.MoveNPCsRandomly(npcs, phase));
                break;
            case GamePhase.Phase2:
                npcMove.MoveNPCsOnRails(npcs);
                break;
            // Add cases for other phases as needed
        }
    }

//     private IEnumerator MoveNPCsRandomly(GameObject[] npcs)
//     {
//         npcMove = FindObjectOfType<npcMovement>();
//         while(phaseList.Current.Phase == GamePhase.Phase1)
//         {
//             foreach (GameObject npc in npcs)
//             {
//                 // Only give new destination if NPC has reached its current one
//                 if (npcAgents.ContainsKey(npc) && npcDestinationStatus[npc])
//                 {
//                     Vector3 randomDirection = new Vector3(UnityEngine.Random.Range(-5f, 5f), 0, UnityEngine.Random.Range(-5f, 5f));
//                     Vector3 targetPosition = npc.transform.position + randomDirection;

//                     if (Physics.Raycast(targetPosition + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f))
//                     {
//                         targetPosition = hit.point;
//                     }

//                     MoveTo(targetPosition, npc);
//                 }
//             }
//             yield return new WaitForSeconds(UnityEngine.Random.Range(1, 3));
//         }
//     }

//     private void MoveNPCsOnRails(GameObject[] npcs)
//     {
//         Vector3[] destinations = new Vector3[]
//         {
//             new Vector3(51.6f, 0.2f, 47.8f),
//             new Vector3(60.0f, 0.2f, 40.0f),
//             new Vector3(45.0f, 0.2f, 55.0f),
//             // Add more positions as needed
//         };

//         for (int i = 0; i < npcs.Length; i++)
//         {
//             Vector3 targetPosition = destinations[i % destinations.Length];
            
//             if (Physics.Raycast(targetPosition + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f))
//             {
//                 targetPosition = hit.point;
//             }

//             MoveTo(targetPosition, npcs[i]);
//         }
//     }


}