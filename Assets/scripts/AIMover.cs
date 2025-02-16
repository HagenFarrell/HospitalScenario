using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AIMover : MonoBehaviour
{
    public Transform target;
    public float speed = 5f;

    private Pathfinder pathfinder;
    private List<Vector3> path;
    private int currentWaypoint = 0;
    private Coroutine currentPathUpdateCoroutine;

    public bool isAtDestination;

    void Start()
    {
        pathfinder = FindObjectOfType<Pathfinder>();
        isAtDestination = true;
    }

    public IEnumerator UpdatePath()
    {
        // Stop any existing path update coroutine
        if (currentPathUpdateCoroutine != null)
        {
            StopCoroutine(currentPathUpdateCoroutine);
        }

        isAtDestination = false;
        
        while (!isAtDestination)  // Only run while we haven't reached destination
        {
            if (target != null)
            {
                path = pathfinder.FindPath(transform.position, target.position);
                if (path != null && path.Count > 0)
                {
                    currentWaypoint = 0;
                }
                else
                {
                    // No path found - might want to handle this case
                    Debug.LogWarning($"No path found for {gameObject.name}");
                }
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    void Update()
    {
        if (path == null || currentWaypoint >= path.Count) return;

        Vector3 direction = (path[currentWaypoint] - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;

        // Check distance in XZ plane (ignore Y-axis)
        Vector2 agentXZ = new Vector2(transform.position.x, transform.position.z);
        Vector2 waypointXZ = new Vector2(path[currentWaypoint].x, path[currentWaypoint].z);
        if (Vector2.Distance(agentXZ, waypointXZ) < 0.2f)
        {
            currentWaypoint++;

            if (currentWaypoint >= path.Count)
            {
                Debug.Log($"{gameObject.name} has reached the destination!");
                isAtDestination = true;
                path = null;  // Clear the path
            }
        }
    }

    // Optional: Method to explicitly stop movement
    public void StopMoving()
    {
        if (currentPathUpdateCoroutine != null)
        {
            StopCoroutine(currentPathUpdateCoroutine);
        }
        isAtDestination = true;
        path = null;
    }

    void OnDrawGizmos()
    {
        if (path != null)
        {
            Gizmos.color = Color.blue;
            foreach (Vector3 point in path)
            {
                Gizmos.DrawSphere(point, 0.2f);
            }
        }
    }
}