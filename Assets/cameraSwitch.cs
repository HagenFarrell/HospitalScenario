using UnityEngine;

public class cameraSwitch : MonoBehaviour
{
    private Camera[] cameras; // Array to hold the cameras
    private int activeCameraIndex = 0; // Index of the currently active camera

    void Start()
    {
        // Get all camera components from child objects
        cameras = GetComponentsInChildren<Camera>();

        //Disable all cameras except the first one
        for (int i = 0; i < cameras.Length; i++)
        {
            cameras[i].enabled = (i == activeCameraIndex);
        }
    }

    void Update()
    { 
        // Switch to the corre sponding camera based on key press
        if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchCamera(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchCamera(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchCamera(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SwitchCamera(3);
    }

    void SwitchCamera(int cameraIndex)
    {
        // Ensure the index is within bounds
        if (cameraIndex >= 0 && cameraIndex < cameras.Length)
        {
            // Disable the currently active camera
            cameras[activeCameraIndex].enabled = false;

            // Enable the selected camera
            cameras[cameraIndex].enabled = true;

            // Update the active camera index
            activeCameraIndex = cameraIndex;

            Debug.Log($"Switched to camera {cameraIndex + 1}");
        }
        else
        {
            Debug.LogWarning("Camera index out of bounds!");
        }
    }
}
