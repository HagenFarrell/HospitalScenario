using System.Collections.Generic;
using UnityEngine;

public class PathSmoother
{
    public List<Vector3> SmoothPath(List<Vector3> path, int segments = 10)
    {
        List<Vector3> smoothedPath = new List<Vector3>();
        for (int i = 0; i < path.Count - 1; i++)
        {
            Vector3 p0 = (i == 0) ? path[i] : path[i - 1];
            Vector3 p1 = path[i];
            Vector3 p2 = path[i + 1];
            Vector3 p3 = (i == path.Count - 2) ? path[i + 1] : path[i + 2];

            for (int j = 0; j <= segments; j++)
            {
                float t = j / (float)segments;
                smoothedPath.Add(CalculateBezierPoint(t, p0, p1, p2, p3));
            }
        }
        return smoothedPath;
    }

    private Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float u = 1 - t;
        float uu = u * u;
        float uuu = uu * u;
        float tt = t * t;
        float ttt = tt * t;

        Vector3 point = uuu * p0 + 3 * uu * t * p1 + 3 * u * tt * p2 + ttt * p3;
        return point;
    }
}