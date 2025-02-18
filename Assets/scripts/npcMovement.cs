using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public static class GameObjectExtensions
{
    public static T GetOrAddComponent<T>(this GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        if (component == null)
        {
            component = go.AddComponent<T>();
        }
        return component;
    }
}
public class npcMovement : MonoBehaviour
{
    [SerializeField] private float randomMovementRadius = 20f;
    [SerializeField] private float randomMovementInterval = 5f;
    private Camera mainCamera;

    [SerializeField] private float stoppingRadius = 2f;
    [SerializeField] private float rowSpacing = 4f;
    [SerializeField] private float colSpacing = 2f;
    [SerializeField] private float formationUpdateInterval = 10f;
    
    //private bool isMovingSequence = false;

    // Store references to all active NPCs
    private Dictionary<GameObject, AIMover> npcAgents = new Dictionary<GameObject, AIMover>();
    private Dictionary<GameObject, Animator> npcAnimators = new Dictionary<GameObject, Animator>();
    private Dictionary<GameObject, bool> npcDestinationStatus = new Dictionary<GameObject, bool>();
    private Coroutine formationUpdateCoroutine;

    private PhaseManager phaseManager;
    public DynamicNavMesh dynamicNavMesh;

    void Start()
    {
        mainCamera = Camera.main;
        phaseManager = FindObjectOfType<PhaseManager>();
    }

    public void refreshCamera()
    {
        mainCamera = Camera.main;
    }

    // Rewrote this function to implement the steering solution analogous to StarCraft II's design.
    public void moveFormation(GameObject[] npcs)
    {
        // You know what this does ;)
        if (npcs.Length == 0) return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // We need to create a commander, this way we can have the group offset from the commanders position.
            AIMover commanderAI = npcs[0].GetComponent<AIMover>();

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

            foreach (GameObject npc in npcs)
            {
                // Grab the AIMover and Animator components for each NPC.
                AIMover mover = npc.GetComponent<AIMover>();
                Animator animator = npc.GetComponent<Animator>();

                if (mover != null)
                {
                    npcAgents[npc] = mover;
                    npcDestinationStatus[npc] = false;

                    if (animator != null)
                    {
                        npcAnimators[npc] = animator;
                    }
                }
            }

            commanderAI.SetTargetPosition(hit.point);
            StartCoroutine(commanderAI.UpdatePath());

            // If a formation corutine is already running, halt.
            if (formationUpdateCoroutine != null)
            {
                StopCoroutine(formationUpdateCoroutine);
            }

            formationUpdateCoroutine = StartCoroutine(updateFormationPositions(npcs));
        }
    }

    private IEnumerator updateFormationPositions(GameObject[] npcs)
    {
        while (!allNpcsAtDestination())
        {
            AIMover commanderAI = npcs[0].GetComponent<AIMover>();

            if (commanderAI == null) yield break;

            Vector3 commanderPosition = commanderAI.transform.position;
            Vector3 commanderForward = commanderAI.transform.forward;

            // Update all NPCs including the commander
            for (int i = 0; i < npcs.Length; i++)
            {
                GameObject npc = npcs[i];
                AIMover agent = npc.GetComponent<AIMover>();

                if (agent != null)
                {
                    // Skip commander.
                    if (i == 0) continue;

                    else
                    {
                        Vector3 formationSlot = ComputeTriangleSlot(
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
                            if (!agent.isAtDestination)
                            {
                                StartCoroutine(agent.UpdatePath());
                            }
                        }
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
        {
            if (!status) return false;
        }

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
    Vector3 ComputeTriangleSlot(int index, Vector3 apex, Vector3 forward, float rowSpacing, float colSpacing)
    {
        int row = 0;
        int count = 0;

        while (true)
        {
            int rowCount = row + 1;
            if (index < count + rowCount)
            {
                int col = index - count;

                // The position for this row, from the apex of the formation backwards.
                Vector3 rowPosition = apex - forward * (rowSpacing * row);

                Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

                // Centering the row horizontally.
                // In order to grab the center of the rowCount NPC we have to do (rowCount - 1)/2.0f <-- middle position.
                float centerOffset = (rowCount - 1) / 2.0f;
                float horizontalOffset = (col - centerOffset) * colSpacing;

                return rowPosition + right * horizontalOffset;
            }

            count += row + 1;
            row++;
        }
    }

    public IEnumerator MoveCiviliansRandomly(GamePhase currentPhase)
    {
        Debug.Log("Starting MoveCiviliansRandomly coroutine");
        
        // Find all civilians
        GameObject[] civilians = GameObject.FindGameObjectsWithTag("Civilians");
        
        if (civilians.Length == 0)
        {
            Debug.LogError("No civilians found with tag 'Civilian'");
            yield break;
        }
        
        Debug.Log($"Found {civilians.Length} civilians");
        
        // Main movement loop
        float timer = 0;
        while (true)
        {
            // Move each civilian to a random position
            foreach (GameObject civilian in civilians)
            {
                if (civilian == null) continue;
                
                AIMover mover = civilian.GetOrAddComponent<AIMover>();
                
                // Only move NPCs that have reached their destination or don't have one yet
                if (mover.isAtDestination)
                {
                    // Generate random direction within a circle
                    Vector2 randomDirection2D = Random.insideUnitCircle * randomMovementRadius;
                    Vector3 randomDirection = new Vector3(randomDirection2D.x, 0, randomDirection2D.y);
                    Vector3 targetPosition = civilian.transform.position + randomDirection;
                    
                    // Ensure the target is on valid ground
                    if (Physics.Raycast(targetPosition + Vector3.up * 20f, Vector3.down, out RaycastHit hit, 40f))
                    {
                        targetPosition = hit.point;
                        
                        // Verify the position is within the navmesh
                        GridNode node = dynamicNavMesh.GetNodeFromWorldPoint(targetPosition);
                        if (node != null && node.IsWalkable)
                        {
                            Debug.Log($"Moving {civilian.name} to {targetPosition}");
                            
                            // Save starting position for the phase (only once per phase)
                            if (phaseManager != null && timer < 0.1f)
                            {
                                phaseManager.LogAction(civilian.name, civilian.transform.position);
                            }
                            
                            // Set up movement
                            mover.SetTargetPosition(targetPosition);
                            StartCoroutine(mover.UpdatePath());
                            
                            // Activate animation if available
                            Animator animator = civilian.GetComponent<Animator>();
                            if (animator != null)
                            {
                                animator.SetBool("IsWalking", true);
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"Invalid navmesh position for {civilian.name} at {targetPosition}");
                        }
                    }
                }
            }
            
            // Wait before the next round of movements
            timer += randomMovementInterval;
            yield return new WaitForSeconds(randomMovementInterval);
        }
    }
}