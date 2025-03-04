using UnityEngine;

public class cameraSwitch : MonoBehaviour
{
    private Camera[] cameras; // Array to hold the cameras
    private int activeCameraIndex = 0; // Index of the currently active camera
    private npcMovement npcMovement;

    public GameObject Radeye; // Reference to the original Radeye tool in the scene (not a prefab)
    public Vector3[] radeyePositions;
    public Vector3[] radeyeRotations;
    public GameObject radeyeCircleTool; // The object that follows the mouse
    

    void Start()
    {
        // Get all camera components from child objects
        cameras = GetComponentsInChildren<Camera>();

        // Disable all cameras except the first one
        for (int i = 0; i < cameras.Length; i++)
        {
            cameras[i].enabled = (i == activeCameraIndex);
        }

        Collider circleCollider = radeyeCircleTool.GetComponent<Collider>();
        if (circleCollider != null)
        {
            Collider[] allColliders = FindObjectsOfType<Collider>();
            foreach (Collider col in allColliders)
            {
                if (col != circleCollider)
                {
                    Physics.IgnoreCollision(circleCollider, col);
                }
            }
        }
        MoveRadeyeToActiveCamera(); // Move the original Radeye tool to the initial camera

    }

    void Update()
    {
        // Switch cameras based on key press
        // if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchCamera(0); // Press "1" for camera 1
        // if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchCamera(1); // Press "2" for camera 2
        // if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchCamera(2); // Press "3" for camera 3

        if (Radeye.activeInHierarchy && radeyeCircleTool != null)
        {
            //Debug.Log("Calling MoveCircleToMousePosition");
            MoveCircleToMousePosition();
        }
        else
        {
            //Debug.LogWarning("Radeye is not active or radeyeCircleTool is null");
        }
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
            //Debug.Log($"Switched to {cameras[activeCameraIndex].name}");
            //Debug.Log($"Radeye World Position: {Radeye.transform.position}");
            //Debug.Log($"Radeye Local Position: {Radeye.transform.localPosition}");
            //Debug.Log($"Radeye World Rotation: {Radeye.transform.rotation.eulerAngles}");
            //Debug.Log($"Radeye Local Rotation: {Radeye.transform.localRotation.eulerAngles}");
            //Debug.Log($"Radeye Parent: {Radeye.transform.parent.name}");
        }
        else
        {
            Debug.LogWarning("Radeye tool or active camera is null!");
        }
    }

    private void MoveCircleToMousePosition()
    {
        // Get the current active camera
        Camera activeCam = cameras[activeCameraIndex];

        // Get the mouse position in screen space
        Vector3 mousePosition = Input.mousePosition;

        // Convert the mouse position to a ray
        Ray ray = activeCam.ScreenPointToRay(mousePosition);
        RaycastHit hit;

        // LayerMask to exclude radeyeCircleTool (IgnoreRaycast layer)
        int layerMask = ~LayerMask.GetMask("IgnoreRaycast"); // Exclude the IgnoreRaycast layer

        // Perform the raycast with the layer mask
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask))
        {
            // Move the circle to the hit point
            radeyeCircleTool.transform.position = hit.point;
        }
        else
        {
            // Default position in front of the camera
            radeyeCircleTool.transform.position = ray.origin + ray.direction * 10f;
        }
    }


}