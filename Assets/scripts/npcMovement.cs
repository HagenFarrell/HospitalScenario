using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class npcMovement : MonoBehaviour
{
   Camera mainCamera;
   private float stoppingRadius = 10f; // Radius Where NPCs can stop at

   // Store references to all active NPCs
   private Dictionary<GameObject, NavMeshAgent> npcAgents = new Dictionary<GameObject, NavMeshAgent>();
   private Dictionary<GameObject, Animator> npcAnimators = new Dictionary<GameObject, Animator>();
   private Dictionary<GameObject, bool> npcDestinationStatus = new Dictionary<GameObject, bool>();

    private AIMover[] aimovers;

   void Start()
   {
       if (mainCamera == null)
       {
           mainCamera = Camera.main;
       }
       aimovers = GetComponentsInChildren<AIMover>();
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

    public void moveNpc(GameObject[] npcs)
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            foreach (AIMover mover in aimovers)
            {
                mover.target.position = hit.point;
                Debug.Log(mover.target.position);
                StartCoroutine(mover.UpdatePath());
            }
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
                foreach(GameObject npc in npcs)
                {
                    if (!npcAgents.ContainsKey(npc))
                    {
                        npcAgents[npc] = npc.GetComponent<NavMeshAgent>();
                        npcAnimators[npc] = npc.GetComponent<Animator>();
                    }

                    NavMeshAgent agent = npcAgents[npc];
                    Animator animator = npcAnimators[npc];
                    
                    if (agent != null && animator != null)
                    {
                        Vector3 destination;
                        if(npc == centerNPC)
                        {
                            // Center NPC goes to click point
                            destination = navMeshHit.position;
                        }
                        else
                        {
                            // Others circle around based on their position relative to center
                            Vector3 dirFromCenter = (npc.transform.position - centerNPC.transform.position).normalized;
                            float distanceFromCenter = stoppingRadius * 
                                (1f + Vector3.Distance(npc.transform.position, centerNPC.transform.position) * 0.2f); // Outer NPCs spread more
                            destination = navMeshHit.position + dirFromCenter * distanceFromCenter;
                        }

                        if (NavMesh.SamplePosition(destination, out NavMeshHit finalHit, stoppingRadius, NavMesh.AllAreas))
                        {
                            npcDestinationStatus[npc] = false;
                            animator.SetBool("IsWalking", true);
                            
                            foreach(AIMover mover in aimovers)
                            {
                                mover.UpdatePath();
                            }
                            //agent.SetDestination(finalHit.position);
                        }
                    }
                }
            }
        }
    }
}