using Mirror;
using UnityEngine;

public class PlayerMovementMultiplayer : NetworkBehaviour
{
    public float moveSpeed = 5f;

    void Update()
    {
        if (!isLocalPlayer) return;  

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(horizontal, 0, vertical) * moveSpeed * Time.deltaTime;
        transform.Translate(movement, Space.World);
    }
}
