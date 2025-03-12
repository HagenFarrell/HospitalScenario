using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DistanceTracker : MonoBehaviour
{
    public GameObject GammaKnife; // Assign the GammaKnife GameObject in the Inspector

    void Update()
    {
        if (GammaKnife != null)
        {
            // Calculate the distance between this object and the GammaKnife
            float distance = Vector3.Distance(transform.position, GammaKnife.transform.position);

            // Print the actual distance to the console
           // Debug.Log("Distance from GammaKnife: " + distance);
        }
        else
        {
            Debug.LogError("GammaKnife is not assigned!");
        }
    }
}
