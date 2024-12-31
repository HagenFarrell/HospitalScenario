using UnityEngine;

public class CameraLook : MonoBehaviour
{
    public float mouseSensitivity = 100f; // Adjust this to control sensitivity
    public Transform playerBody; // Reference to the floating object or player body
    
    private float xRotation = 0f;

    void Start()
    {
        // Lock the cursor to the center of the screen
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Rotate camera up and down by changing xRotation
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); // Limit vertical rotation to avoid flipping
        
        // Apply rotation to the camera
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Rotate the player body left and right based on mouseX
        playerBody.Rotate(Vector3.up * mouseX);
    }
}
