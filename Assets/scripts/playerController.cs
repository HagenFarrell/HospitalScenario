using UnityEngine;

public class playerController : MonoBehaviour
{
    public float moveSpeed = 10f; // Horizontal movement speed
    public float verticalSpeed = 5f; // Vertical movement speed
    public float smoothingSpeed = 0.1f; // Determines how smooth the movement is

    private Vector3 moveDirection = Vector3.zero;
    private Vector3 currentVelocity = Vector3.zero; // Used for SmoothDamp

    void Update()
    {
        // Get WASD or arrow key input for horizontal and vertical movement
        float horizontal = Input.GetAxis("Horizontal"); // A/D or Left/Right Arrows
        float vertical = Input.GetAxis("Vertical"); // W/S or Up/Down Arrows

        // Calculate movement direction
        moveDirection = new Vector3(horizontal, 0, vertical).normalized;

        // Check for upward or downward movement with Space/Shift
        if (Input.GetKey(KeyCode.Space))
        {
            moveDirection.y = 1f; // Move upward
        }
        else if (Input.GetKey(KeyCode.LeftShift))
        {
            moveDirection.y = -1f; // Move downward
        }
        else
        {
            moveDirection.y = 0f; // No vertical movement
        }
    }

    void FixedUpdate()
    {
        // Calculate the target velocity based on moveDirection
        Vector3 targetVelocity = transform.TransformDirection(moveDirection) * moveSpeed;
        targetVelocity.y = moveDirection.y * verticalSpeed;

        // Smoothly interpolate the current velocity towards the target velocity
        currentVelocity = Vector3.SmoothDamp(currentVelocity, targetVelocity, ref currentVelocity, smoothingSpeed);

        // Apply the smoothed velocity to the player's position
        transform.position += currentVelocity * Time.fixedDeltaTime;
    }
}
