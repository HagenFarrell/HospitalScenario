using UnityEngine;

public class cameraSwitch : MonoBehaviour
{
    private Camera[] cameras; // Array to hold the cameras
    private int activeCameraIndex = 0; // Index of the currently active camera
    private npcMovement npcMovement;

    public GameObject Radeye; // Reference to the original Radeye tool in the scene (not a prefab)
    public Vector3[] radeyePositions;
    public Vector3[] radeyeRotations;
    void Start()
    {
        // Get all camera components from child objects
        cameras = GetComponentsInChildren<Camera>();

        // Disable all cameras except the first one
        for (int i = 0; i < cameras.Length; i++)
        {
            cameras[i].enabled = (i == activeCameraIndex);
        }

        MoveRadeyeToActiveCamera(); // Move the original Radeye tool to the initial camera
    }

    void Update()
    {
        // Switch cameras based on key press
        if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchCamera(0); // Press "1" for camera 1
        if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchCamera(1); // Press "2" for camera 2
        if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchCamera(2); // Press "3" for camera 3
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

            // Move the Radeye tool to the new active camera
            MoveRadeyeToActiveCamera();
        }
    }

    private void MoveRadeyeToActiveCamera()
    {
        if (Radeye != null && cameras[activeCameraIndex] != null)
        {
            // Temporarily unparent the Radeye tool
            Radeye.transform.SetParent(null);

            // Calculate the new position and rotation
            Vector3 targetPosition = cameras[activeCameraIndex].transform.TransformPoint(radeyePositions[activeCameraIndex]);
            Quaternion targetRotation = cameras[activeCameraIndex].transform.rotation * Quaternion.Euler(radeyeRotations[activeCameraIndex]);

            // Apply the new position and rotation
            Radeye.transform.position = targetPosition;
            Radeye.transform.rotation = targetRotation;

            // Reparent the Radeye tool to the active camera
            Radeye.transform.SetParent(cameras[activeCameraIndex].transform);

            // Debugging output
            Debug.Log($"Switched to {cameras[activeCameraIndex].name}");
            Debug.Log($"Radeye World Position: {Radeye.transform.position}");
            Debug.Log($"Radeye Local Position: {Radeye.transform.localPosition}");
            Debug.Log($"Radeye World Rotation: {Radeye.transform.rotation.eulerAngles}");
            Debug.Log($"Radeye Local Rotation: {Radeye.transform.localRotation.eulerAngles}");
            Debug.Log($"Radeye Parent: {Radeye.transform.parent.name}");
        }
        else
        {
            Debug.LogWarning("Radeye tool or active camera is null!");
        }
    }



}