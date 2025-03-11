using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GamePhase;

public class npcMovement : MonoBehaviour
{
    [SerializeField] private float stoppingRadius = 2f;
    [SerializeField] private float rowSpacing = 2f;
    [SerializeField] private float colSpacing = 2f;
    [SerializeField] private float formationUpdateInterval = 0.10f;

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
    Debug.Log($"NPCs length = {npcs.Length}");
    if (npcs.Length == 0) return;

    var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
    if (Physics.Raycast(ray, out var hit))
    {
        // Get the click position - this is where the formation will be centered
        Vector3 clickPosition = hit.point;
        
        // Set formation direction - typically toward the camera or based on the terrain
        Vector3 formationDirection = Vector3.forward; // Default direction
        
        // Try to use a more natural direction if available (e.g., from current position to click)
        if (npcs.Length > 0 && npcs[0] != null)
        {
            Vector3 directionToClick = (clickPosition - npcs[0].transform.position).normalized;
            // Use horizontal direction only
            directionToClick.y = 0;
            if (directionToClick.magnitude > 0.1f)
            {
                formationDirection = directionToClick;
            }
        }
        
        // Clear tracking dictionaries
        npcAgents.Clear();
        npcDestinationStatus.Clear();
        npcAnimators.Clear();

        // Register all NPCs first
        foreach (var npc in npcs)
        {
            var mover = npc.GetComponent<AIMover>();
            var animator = npc.GetComponent<Animator>();

            if (mover != null)
            {
                npcAgents[npc] = mover;
                npcDestinationStatus[npc] = false;
                if (animator != null) npcAnimators[npc] = animator;
            }
        }
        
        // Calculate all final destinations based on the click position
        for (var i = 0; i < npcs.Length; i++)
        {
            var npc = npcs[i];
            var agent = npcAgents[npc];

            if (agent == null) continue;
            
            Vector3 finalPosition;
            
            if (i == 0)
            {
                // Commander goes to the click position
                finalPosition = clickPosition;
            }
            else
            {
                // Calculate formation position relative to click position
                finalPosition = ComputeTriangleSlot(
                    i,
                    clickPosition,       // Use click position, not commander position
                    formationDirection,  // Use calculated direction
                    rowSpacing,
                    colSpacing
                );
            }

            // Set the destination and calculate path immediately
            agent.SetTargetPosition(finalPosition);
            StartCoroutine(agent.UpdatePath(agent));
        }

        // Optionally start an update coroutine for ongoing adjustments
        if (formationUpdateCoroutine != null) StopCoroutine(formationUpdateCoroutine);
        formationUpdateCoroutine = StartCoroutine(updateFormationPositions(npcs, clickPosition, formationDirection));
    }
}

private IEnumerator updateFormationPositions(GameObject[] npcs, Vector3 formationCenter, Vector3 formationDirection)
{
    // Small initial delay
    yield return new WaitForSeconds(0.1f);
    
    while (!allNpcsAtDestination())
    {
        // Check if any units need their paths refreshed
        for (var i = 0; i < npcs.Length; i++)
        {
            var npc = npcs[i];
            var agent = npcAgents[npc];

            if (agent != null && !agent.isAtDestination)
            {
                // Get the unit's current target
                Vector3 currentTarget = agent.target.position;
                
                // Only recalculate if needed (unit might be stuck or had issues)
                if (Vector3.Distance(npc.transform.position, currentTarget) > 5f && 
                    agent.currentVelocity.magnitude < 0.1f)
                {
                    // Unit seems stuck - calculate a new path
                    Vector3 finalPosition;
                    
                    if (i == 0)
                    {
                        // Commander
                        finalPosition = formationCenter;
                    }
                    else
                    {
                        // Calculate formation position relative to formation center
                        finalPosition = ComputeTriangleSlot(
                            i,
                            formationCenter,
                            formationDirection,
                            rowSpacing,
                            colSpacing
                        );
                    }
                    
                    agent.SetTargetPosition(finalPosition);
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