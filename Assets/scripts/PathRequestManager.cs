// Create a new PathRequestManager.cs file
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PathRequestManager : MonoBehaviour
{
    private static PathRequestManager instance;
    private Pathfinder pathfinder;
    private Queue<PathRequest> pathRequestQueue = new Queue<PathRequest>();
    private PathRequest currentRequest;
    private bool isProcessingPath;

    struct PathRequest
    {
        public Vector3 pathStart;
        public Vector3 pathEnd;
        public Action<List<Vector3>> callback;

        public PathRequest(Vector3 start, Vector3 end, Action<List<Vector3>> callback)
        {
            pathStart = start;
            pathEnd = end;
            this.callback = callback;
        }
    }

    void Awake()
    {
        instance = this;
        pathfinder = GetComponent<Pathfinder>();
    }

    public static void RequestPath(Vector3 pathStart, Vector3 pathEnd, Action<List<Vector3>> callback)
    {
        PathRequest newRequest = new PathRequest(pathStart, pathEnd, callback);
        instance.pathRequestQueue.Enqueue(newRequest);
        instance.TryProcessNext();
    }

    void TryProcessNext()
    {
        if (!isProcessingPath && pathRequestQueue.Count > 0)
        {
            currentRequest = pathRequestQueue.Dequeue();
            isProcessingPath = true;
            StartCoroutine(ProcessPath());
        }
    }

    IEnumerator ProcessPath()
    {
        // Add small delay to distribute CPU load
        yield return new WaitForEndOfFrame();
        
        List<Vector3> newPath = pathfinder.FindPath(currentRequest.pathStart, currentRequest.pathEnd);
        currentRequest.callback(newPath);
        
        isProcessingPath = false;
        TryProcessNext();
    }
}