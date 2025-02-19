using UnityEngine;
using Mirror;

public class Player : NetworkBehaviour
{
    public float moveSpeed = 5f; // Speed for forward/backward and strafing movement
    public float mouseSensitivity = 100f; // Sensitivity for mouse look

    private float yaw = 0f; // Horizontal rotation (Y-axis)
    private float pitch = 0f; // Vertical rotation (X-axis)

    [SerializeField] private Camera playerCamera; // Assign the camera in the Inspector

    [Client]
    void Start()
    {
        if (!isLocalPlayer)
        {
            // Disable camera for remote players
            if (playerCamera != null)
            {
                playerCamera.enabled = false;
            }
            return;
        }

        // Ensure the cursor is visible and not locked
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Enable the local player's camera
        if (playerCamera != null)
        {
            playerCamera.enabled = true;
        }
    }

    [Client]
    void Update()
    {
        if (!isLocalPlayer) return;

        // Handle mouse look
        HandleMouseLook();

        // Handle movement
        HandleMovement();
    }

    private void HandleMouseLook()
    {
        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // Rotate the camera horizontally (yaw)
        yaw += mouseX;
        transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

        // Rotate the camera vertically (pitch)
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -90f, 90f); // Prevent flipping
        playerCamera.transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void HandleMovement()
    {
        // Get input for movement
        float moveX = Input.GetAxis("Horizontal"); // Strafe left/right
        float moveZ = Input.GetAxis("Vertical");   // Move forward/backward

        // Create movement vector relative to the camera's facing direction
        Vector3 moveDirection = (playerCamera.transform.right * moveX + playerCamera.transform.forward * moveZ).normalized;
        Vector3 movement = moveDirection * moveSpeed * Time.deltaTime;

        // Apply movement locally
        transform.position += movement;

        // Send movement request to server
        CmdMove(transform.position);
    }

    [Command]
    private void CmdMove(Vector3 newPosition)
    {
        // Server-side movement logic
        transform.position = newPosition;

        // Sync position with all clients except the local player
        RpcMove(newPosition);
    }

    [ClientRpc]
    private void RpcMove(Vector3 newPosition)
    {
        // Only update the position if this is not the local player
        if (!isLocalPlayer)
        {
            transform.position = newPosition;
        }
    }
}