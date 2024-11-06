using UnityEngine;

public class playerController : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float floatHeight = 1.5f;
    public float smoothing = 0.1f;

    private Rigidbody rb;

    void Start()
    {
        rb = gameObject.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.linearDamping = 2f; // Adds a bit of drag to smooth movement
    }

    void Update()
    {
        // WASD Movement
        float horizontal = Input.GetAxis("Horizontal"); // A/D or Left/Right Arrows
        float vertical = Input.GetAxis("Vertical"); // W/S or Up/Down Arrows
        
        Vector3 moveDirection = new Vector3(horizontal, 0, vertical).normalized;
        
        // Apply movement with respect to the camera direction
        Vector3 moveVector = transform.TransformDirection(moveDirection) * moveSpeed;
        
        rb.AddForce(moveVector, ForceMode.Acceleration);

        // Float at a certain height
        Vector3 targetPosition = new Vector3(transform.position.x, floatHeight, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothing * Time.deltaTime);
    }
}
