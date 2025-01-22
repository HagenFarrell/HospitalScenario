using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class npcMovement : MonoBehaviour
{
    Camera mainCamera;
    private float stoppingRadius = 2f; // Radius Where NPCs can stop at

    private float timeDelayBetweenNPCs = 0.5f; // Time between each npc movements
    private bool isMovingSequence = false; // Tracks if movement is in progress

    // Store references to all active NPCs
    private Dictionary<GameObject, NavMeshAgent> npcAgents = new Dictionary<GameObject, NavMeshAgent>();
    private Dictionary<GameObject, Animator> npcAnimators = new Dictionary<GameObject, Animator>();
    private Dictionary<GameObject, bool> npcDestinationStatus = new Dictionary<GameObject, bool>();

    void Start()
    {
       if (mainCamera == null)
       {
           mainCamera = Camera.main;
       }
    }

    void Update()
    {
        // Update animations based on movement
        foreach(var npc in npcAgents.Keys)
        {
            if (!npcDestinationStatus[npc])
            {
                NavMeshAgent agent = npcAgents[npc];
                Animator animator = npcAnimators[npc];

                animator.SetBool("IsWalking", agent.velocity.magnitude > 0.1f);

                // Check if reached destination
                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
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

    /* Takes in group of NPCs we want to move
       Takes in The positon where the player clicked
       Takes in the center most NPC of the group */
    private IEnumerator MoveNPCsOneAtATime(GameObject[] npcs, Vector3 ClickPosition, GameObject centerNPC)
    {
        foreach(GameObject npc in npcs) // Loop though each NPC
        {
            // Check if this NPC's components have been stored
            if (!npcAgents.ContainsKey(npc))
            {
                // If not, get and store the NavMeshAgent and Animator components
                npcAgents[npc] = npc.GetComponent<NavMeshAgent>();
                npcAnimators[npc] = npc.GetComponent<Animator>();
            }

            // Get the stored component for this NPC
            NavMeshAgent agent = npcAgents[npc];
            Animator animator = npcAnimators[npc];

             // Check if we have valid components
            if (agent != null && animator != null)
            {
                Vector3 destination;
                if(npc == centerNPC)
                {
                    // Center NPC goes to click point
                    destination = ClickPosition;
                }
                else
                {
                    // Others circle around based on their position relative to center
                    Vector3 dirFromCenter = (npc.transform.position - centerNPC.transform.position).normalized;
                    float distanceFromCenter = stoppingRadius * 
                        (1f + Vector3.Distance(npc.transform.position, centerNPC.transform.position) * 0.2f);
                    destination = ClickPosition + dirFromCenter * distanceFromCenter;
                }

                // Sample the NavMesh to find a valid position
                if (NavMesh.SamplePosition(destination, out NavMeshHit finalHit, stoppingRadius, NavMesh.AllAreas))
                {
                    npcDestinationStatus[npc] = false;
                    animator.SetBool("IsWalking", true);
                    agent.SetDestination(finalHit.position);
                }
            }

            yield return new WaitForSeconds(timeDelayBetweenNPCs); // Wait before moving the next NPC
            
        }

        isMovingSequence = false; // Ends Movement
    }

    public void moveNpc(GameObject[] npcs)
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (NavMesh.SamplePosition(hit.point, out NavMeshHit navMeshHit, 1.0f, NavMesh.AllAreas))
            {
                // Find center NPC (closest to group's center)
                Vector3 groupCenter = Vector3.zero;
                foreach(GameObject npc in npcs)
                {
                    groupCenter += npc.transform.position;
                }
                groupCenter /= npcs.Length;

                // Find NPC closest to center
                GameObject centerNPC = npcs[0];
                float closestDist = float.MaxValue;
                foreach(GameObject npc in npcs)
                {
                    float dist = Vector3.Distance(npc.transform.position, groupCenter);
                    if(dist < closestDist)
                    {
                        closestDist = dist;
                        centerNPC = npc;
                    }
                }
                // Move NPCs
                StartCoroutine(MoveNPCsOneAtATime(npcs, navMeshHit.position, centerNPC));
            }
        }
    }
}