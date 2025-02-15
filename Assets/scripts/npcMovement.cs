using System.Collections.Generic;
using UnityEngine;

public class npcMovement : MonoBehaviour
{
    Camera mainCamera;
    private float stoppingRadius = 2f; // Radius Where NPCs can stop at

    private float rowSpacing = 2f;
    private float colSpacing = 1.5f;

    private float timeDelayBetweenNPCs = 0.5f; // Time between each npc movements
    private bool isMovingSequence = false; // Tracks if movement is in progress

    // Store references to all active NPCs
    private Dictionary<GameObject, AIMover> npcAgents = new Dictionary<GameObject, AIMover>();
    private Dictionary<GameObject, Animator> npcAnimators = new Dictionary<GameObject, Animator>();
    private Dictionary<GameObject, bool> npcDestinationStatus = new Dictionary<GameObject, bool>();

    private AIMover[] aimovers;
    public DynamicNavMesh dynamicNavMesh;

    void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        aimovers = gameObject.GetComponentsInChildren<AIMover>();
    }

    void Update()
    {

        // Update animations based on movement
        foreach (var npc in npcAgents.Keys)
        {

            if (!npcDestinationStatus[npc])
            {
                AIMover agent = npcAgents[npc];
                Animator animator = npcAnimators[npc];

                // Check if reached destination
                if (agent.isAtDestination)
                {
                    npcDestinationStatus[npc] = true;
                    animator.SetBool("IsWalking", false);
                }
            }
        }
    }

    public void refreshCamera()
    {
        mainCamera = Camera.main;
    }

    // Rewrote this function to implement the steering solution analogous to StarCraft II's design.
    public void moveNpc(GameObject[] npcs)
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // You know what this does ;)
            if (npcs.Length == 0) return;

            Vector3 formationApex = hit.point;

            // Need this for the "flock" heading, which is typically a boids algorithm term.
            Vector3 forwardDirection = Vector3.forward;

            // We will now loop through each NPC and compute its placement in the triangle formation.
            for (int i = 0; i < npcs.Length; ++i)
            {
                GameObject npc = npcs[i];


                // Get cached AImover object.
                AIMover agent = npc.GetComponent<AIMover>();
                if (agent == null) continue;

                // Calculate the formation slot for the current NPC.
                Vector3 formationSlot = ComputeTriangleSlot(i, formationApex, forwardDirection, rowSpacing, colSpacing);

                // Just updates the agents position relative to the other agents (NPCs)
                agent.transform.position = formationSlot;

                // Trigger the pathfinding subroutine.
                StartCoroutine(agent.UpdatePath());
            }
        }
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
}