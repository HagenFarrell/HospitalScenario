using UnityEngine;

public class cameraSwitch : MonoBehaviour
{
    private Camera[] cameras; // Array to hold the cameras
    private int activeCameraIndex = 0; // Index of the currently active camera
    private npcMovement npcMovement;

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

    }

    public void SwitchCamera(int cameraIndex)
    {
        //Ensure the index is within bounds
        if (cameraIndex >= 0 && cameraIndex < cameras.Length)
        {
            //Remove mainCamera tag on activecamera
            cameras[activeCameraIndex].tag = "Untagged";

            //Disable the currently active camera
            cameras[activeCameraIndex].enabled = false;

            //Enable the selected camera
            cameras[cameraIndex].enabled = true;

            //Update the active camera index
            activeCameraIndex = cameraIndex;

            //Add mainCamera tag on new camera
            cameras[activeCameraIndex].tag = "MainCamera";

            

            //Debug.Log($"Switched to camera {cameraIndex + 1}");
        }

    }
}
