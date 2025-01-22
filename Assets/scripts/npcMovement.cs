using UnityEngine;
using UnityEngine.AI;

public class npcMovement : MonoBehaviour
{

    Camera mainCamera;
    private NavMeshAgent agent;

    void Start()
    {
        
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))  // Left mouse click
        {
           
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
            // Check if the hit point is on the NavMesh
            if (NavMesh.SamplePosition(hit.point, out NavMeshHit navMeshHit, 1.0f, NavMesh.AllAreas))
            {
                foreach(GameObject npc in npcs)
                {
                    agent = npc.GetComponent<NavMeshAgent>();
                    agent.SetDestination(navMeshHit.position);
                }
                
                
            }
        }
    }
}
