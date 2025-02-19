using UnityEngine;
using Mirror;

public class Player : NetworkBehaviour
{
    public float moveSpeed = 5f;
    public float verticalSpeed = 3f;
    private Vector3 movement = Vector3.zero;

    [Client]
    void Update()
    {
        if (!isLocalPlayer) return;

        // Get input for movement
        float moveX = Input.GetKey(KeyCode.RightArrow) ? 1 : Input.GetKey(KeyCode.LeftArrow) ? -1 : 0;
        float moveZ = Input.GetKey(KeyCode.UpArrow) ? 1 : Input.GetKey(KeyCode.DownArrow) ? -1 : 0;
        float moveY = Input.GetKey(KeyCode.Space) ? 1 : Input.GetKey(KeyCode.LeftShift) ? -1 : 0;

        // Create movement vector
        movement = new Vector3(moveX, moveY * verticalSpeed / moveSpeed, moveZ).normalized * moveSpeed * Time.deltaTime;

        // Send movement request to server
        CmdMove(movement);
    }

    [Command]
    private void CmdMove(Vector3 moveDirection)
    {
        // Server-side movement logic
        transform.position += moveDirection;

        // Sync position with all clients
        RpcMove(transform.position);
    }

    [ClientRpc]
    private void RpcMove(Vector3 newPosition)
    {
        // Apply movement on all clients
        transform.position = newPosition;
    }
}
