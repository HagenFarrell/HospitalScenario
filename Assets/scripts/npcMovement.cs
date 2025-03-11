using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GamePhase;

public class npcMovement : MonoBehaviour
{
    [SerializeField] private float stoppingRadius = 2f;
    [SerializeField] private float rowSpacing = 2f;
    [SerializeField] private float colSpacing = 2f;
    [SerializeField] private float formationUpdateInterval = 0.25f;

    public DynamicNavMesh dynamicNavMesh;

    //private bool isMovingSequence = false;

    // Store references to all active NPCs
    private readonly Dictionary<GameObject, AIMover> npcAgents = new Dictionary<GameObject, AIMover>();
    private readonly Dictionary<GameObject, Animator> npcAnimators = new Dictionary<GameObject, Animator>();
    private readonly Dictionary<GameObject, bool> npcDestinationStatus = new Dictionary<GameObject, bool>();
    private Coroutine formationUpdateCoroutine;
    private Camera mainCamera;

    private void Start()
    {
        //mainCamera = Camera.main;
    }

    public void refreshCamera()
    {
        mainCamera = Camera.main;
    }

    // Rewrote this function to implement the steering solution analogous to StarCraft II's design.
    public void moveFormation(GameObject[] npcs)
    {
        // You know what this does ;)
        Debug.Log($"NPCs length = {npcs.Length}");
        if (npcs.Length == 0) return;

        var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out var hit))
        {
            // We need to create a commander, this way we can have the group offset from the commanders position.
            var commanderAI = npcs[0].GetComponent<AIMover>();

            // Early exit to prevent crashing.
            if (commanderAI == null)
            {
                Debug.LogError("Commander NPC does not have an AIMover component!");
                return;
            }

            // Clear tracking directories.
            npcAgents.Clear();
            npcDestinationStatus.Clear();
            npcAnimators.Clear();

            foreach (var npc in npcs)
            {
                // Grab the AIMover and Animator components for each NPC.
                var mover = npc.GetComponent<AIMover>();
                var animator = npc.GetComponent<Animator>();

                if (mover != null)
                {
                    npcAgents[npc] = mover;
                    npcDestinationStatus[npc] = false;

                    if (animator != null) npcAnimators[npc] = animator;
                }
            }

            commanderAI.SetTargetPosition(hit.point);
            StartCoroutine(commanderAI.UpdatePath(commanderAI));
            
            var commanderPosition = commanderAI.transform.position;
            var commanderForward = commanderAI.transform.forward;

            for (var i = 1; i < npcs.Length; i++) // Start from 1 to skip commander
            {
                var npc = npcs[i];
                var agent = npcAgents[npc];

                if (agent != null)
                {
                    // Calculate formation position
                    var formationSlot = ComputeTriangleSlot(
                        i,
                        commanderPosition,
                        commanderForward,
                        rowSpacing,
                        colSpacing
                    );

                    // Set target and start path update immediately
                    agent.SetTargetPosition(formationSlot);
                    StartCoroutine(agent.UpdatePath(agent)); // Pass the agent itself as parameter
                }
            }

            // If a formation corutine is already running, halt.
            if (formationUpdateCoroutine != null) StopCoroutine(formationUpdateCoroutine);

            formationUpdateCoroutine = StartCoroutine(updateFormationPositions(npcs));
        }
    }

    private IEnumerator updateFormationPositions(GameObject[] npcs)
    {
        while (!allNpcsAtDestination())
        {
            var commanderAI = npcs[0].GetComponent<AIMover>();

            if (commanderAI == null) yield break;

            var commanderPosition = commanderAI.transform.position;
            var commanderForward = commanderAI.transform.forward;

            // Update all NPCs including the commander
            for (var i = 0; i < npcs.Length; i++)
            {
                var npc = npcs[i];
                var agent = npc.GetComponent<AIMover>();

                if (agent != null)
                {
                    // Skip commander.
                    if (i == 0) continue;

                    var formationSlot = ComputeTriangleSlot(
                        i,
                        commanderPosition,
                        commanderForward,
                        rowSpacing,
                        colSpacing
                    );

                    // Only update if the NPC needs to move
                    if (Vector3.Distance(npc.transform.position, formationSlot) > 0.5f)
                    {
                        agent.SetTargetPosition(formationSlot);
                        if (!agent.isAtDestination) StartCoroutine(agent.UpdatePath(commanderAI));
                    }
                }
            }

            yield return new WaitForSeconds(formationUpdateInterval);
        }
    }

    private bool allNpcsAtDestination()
    {
        // If any npcs are not at the final destination, return false; otherwise true.
        foreach (var status in npcDestinationStatus.Values)
            if (!status)
                return false;

        return true;
    }

    /* Function that computes the slot for NPCs in wedge formation.
     * Function information:
     * index: NPCs index (0-based) in the current group.
     * apex: the formations front guard or target position.
     * forward: the curent direction the formation is facing.
     * rowSpacing: distance between sucessive rows.
     * colSpacing: horizontal spacing between each NPC in a row.
     */
    private Vector3 ComputeTriangleSlot(int index, Vector3 apex, Vector3 forward, float rowSpacing, float colSpacing)
    {
        var row = 0;
        var count = 0;

        while (true)
        {
            var rowCount = row + 1;
            if (index < count + rowCount)
            {
                var col = index - count;

                // The position for this row, from the apex of the formation backwards.
                var rowPosition = apex - forward * (rowSpacing * row);

                var right = Vector3.Cross(Vector3.up, forward).normalized;

                // Centering the row horizontally.
                // In order to grab the center of the rowCount NPC we have to do (rowCount - 1)/2.0f <-- middle position.
                var centerOffset = (rowCount - 1) / 2.0f;
                var horizontalOffset = (col - centerOffset) * colSpacing;

                return rowPosition + right * horizontalOffset;
            }

            count += row + 1;
            row++;
        }
    }

    public void SetCamera(Camera playerCamera)
    {
        mainCamera = playerCamera;
        Debug.Log("npcMovement: Camera assigned dynamically: "+mainCamera.name);
    }
}