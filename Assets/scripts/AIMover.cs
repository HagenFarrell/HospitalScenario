using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIMover : MonoBehaviour
{
    public Transform target;
    public float speed = 5f;

    private Pathfinder pathfinder;
    private List<Vector3> path;
    private int currentWaypoint = 0;

    void Start()
    {
        pathfinder = FindObjectOfType<Pathfinder>();
        if(pathfinder != null ) { StartCoroutine(UpdatePath()); }
       
        
    }

    public IEnumerator UpdatePath()
    {
        while (true)
        {
            if (target != null)
            {
                path = pathfinder.FindPath(transform.position, target.position);
                if (path != null && path.Count > 0)
                {
                    currentWaypoint = 0;
                }
            }
            yield return new WaitForSeconds(0.5f); // Update path every 0.5 seconds
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
        }
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