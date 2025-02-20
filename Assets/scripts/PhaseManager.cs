using UnityEngine;
using System.Collections.Generic;
using PhaseLink;
using System.Collections;

public class PhaseManager : MonoBehaviour
{
    private PhaseLinkedList phaseList;
    private PhaseMovementHelper npcMove;
    private Dictionary<string, Vector3> initialPositions = new Dictionary<string, Vector3>(); // Store initial positions of NPCs
    private Coroutine currentPhaseCoroutine;

    private void Start()
    {
        phaseList = new PhaseLinkedList();

        // Define the phases
        foreach (GamePhase phase in System.Enum.GetValues(typeof(GamePhase)))
        {
            phaseList.AddPhase(phase);
        }

        // Store initial positions of all NPCs
        StoreInitialPositions();

        phaseList.SetCurrentToHead();
        StartPhase();
    }

    private void StoreInitialPositions()
    {
        // Store civilians' initial positions
        GameObject[] civilians = GameObject.FindGameObjectsWithTag("Civilians");
        foreach (GameObject civilian in civilians)
        {
            initialPositions[civilian.name] = civilian.transform.position;
        }

        // Store medicals' initial positions
        GameObject[] medicals = GameObject.FindGameObjectsWithTag("Medicals");
        foreach (GameObject medical in medicals)
        {
            initialPositions[medical.name] = medical.transform.position;
        }

        // Store hostages' initial positions
        GameObject[] hostages = GameObject.FindGameObjectsWithTag("Hostages");
        foreach (GameObject hostage in hostages)
        {
            initialPositions[hostage.name] = hostage.transform.position;
        }

        Debug.Log($"Stored initial positions for {initialPositions.Count} NPCs");
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
            StartPhase();
        }
        else
        {
            Debug.Log("Already at the first phase!");
        }
    }

    private void ResetNPCsToInitialPositions()
    {
        Debug.Log("Resetting NPCs to initial positions for Phase 1");
        
        // Get references to NPCs by tag - whether active or inactive
        List<string> npcTags = new List<string> { "Civilians", "Medicals", "Hostages" };
        
        foreach (string tag in npcTags)
        {
            // Find active NPCs with this tag
            GameObject[] activeNPCs = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject npc in activeNPCs)
            {
                ResetNPC(npc);
            }
            
            // Also check for inactive NPCs that might need to be reactivated
            Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
            foreach (Transform transform in allTransforms)
            {
                if (transform.gameObject.CompareTag(tag) && !transform.gameObject.activeInHierarchy)
                {
                    GameObject inactiveNPC = transform.gameObject;
                    if (initialPositions.ContainsKey(inactiveNPC.name))
                    {
                        // Re-enable and reset the NPC
                        inactiveNPC.SetActive(true);
                        ResetNPC(inactiveNPC);
                    }
                }
            }
        }
    }
    
    private void ResetNPC(GameObject npc)
    {
        if (initialPositions.ContainsKey(npc.name))
        {
            // Reset position to initial
            npc.transform.position = initialPositions[npc.name];
            
            // Reset animator if available
            Animator animator = npc.GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetBool("IsWalking", false);
            }
            
            // Reset and enable AIMover
            AIMover mover = npc.GetComponent<AIMover>();
            if (mover != null)
            {
                mover.enabled = true;
                mover.SetTargetPosition(initialPositions[npc.name]);
                mover.StopAllMovement(); // Stop any ongoing movement
            }
        }
    }

    private void MoveNPCsForPhase(GamePhase phase)
    {
        Debug.Log($"Moving NPCs for phase: {phase}");
        
        npcMove = FindObjectOfType<PhaseMovementHelper>();
        
        switch (phase)
        {
            case GamePhase.Phase1:
                // If coming back to Phase1, reset NPCs to initial positions
                if (currentPhaseCoroutine != null) {
                    StopCoroutine(currentPhaseCoroutine);
                    currentPhaseCoroutine = null;
                }
                
                // Reset NPCs to their initial positions when returning to Phase 1
                ResetNPCsToInitialPositions();
                
                // Start random movement for civilians
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