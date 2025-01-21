using UnityEngine;
using UnityEngine.AI;

public class npcMovement : MonoBehaviour
{

    Camera mainCamera;
    private NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        // Basic NavMesh setup
        if (agent != null)
        {
            agent.stoppingDistance = 0.1f;
        }
    }

    void Update()
{
    if (Input.GetMouseButtonDown(0))  // Left mouse click
    {
        moveNpc(null);
    }
}

    void refreshCamera()
    {
        mainCamera = Camera.main;
    }

    void moveNpc(GameObject[] npcs)
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Check if the hit point is on the NavMesh
            if (NavMesh.SamplePosition(hit.point, out NavMeshHit navMeshHit, 1.0f, NavMesh.AllAreas))
            {
                agent.SetDestination(navMeshHit.position);
            }
        }
    }
}
