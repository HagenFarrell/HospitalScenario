using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DistanceTracker : MonoBehaviour
{
    public GameObject MRIMachine; // Assign the MRIMachine GameObject in the Inspector

    void Update()
    {
        if (MRIMachine != null)
        {
            // Calculate the distance between this object and the MRIMachine
            float distance = Vector3.Distance(transform.position, MRIMachine.transform.position);

            // Print the actual distance to the console
            Debug.Log("Distance from MRIMachine: " + distance);
        }
        else
        {
            Debug.LogError("MRIMachine is not assigned!");
        }
    }
}
