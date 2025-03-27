using System.Collections.Generic;
using UnityEngine;

public class cameraSwitch : MonoBehaviour
{
    private Camera[] cameras; // Array to hold the cameras
    private int activeCameraIndex = 0; // Index of the currently active camera
    private npcMovement npcMovement;

    public List<GameObject> DispatchCams;

    void Start()
    {
        // Get all camera components from child objects
        cameras = GetComponentsInChildren<Camera>();

        // Disable all cameras except the first one
        for (int i = 0; i < cameras.Length; i++)
        {
            cameras[i].enabled = (i == activeCameraIndex);
        }

    }

    void Update()
    {
        // Switch cameras based on key press
        // if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchCamera(0); // Press "1" for camera 1
        // if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchCamera(1); // Press "2" for camera 2
        // if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchCamera(2); // Press "3" for camera 3

    }

    public void SwitchCamera(int cameraIndex)
    {
        // Ensure the index is within bounds
        if (cameraIndex >= 0 && cameraIndex < cameras.Length)
        {
            // Remove MainCamera tag from the active camera
            cameras[activeCameraIndex].tag = "Untagged";

            // Disable the currently active camera
            cameras[activeCameraIndex].enabled = false;

            // Enable the selected camera
            cameras[cameraIndex].enabled = true;

            // Update the active camera index
            activeCameraIndex = cameraIndex;

            // Add MainCamera tag to the new active camera
            cameras[activeCameraIndex].tag = "MainCamera";

        }
    }


}