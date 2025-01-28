using Mirror;
using UnityEngine;

public class PlayerInteraction : NetworkBehaviour
{
    [Command]
    public void CmdInteractWithObject()
    {
        RpcUpdateObjectState();  
    }

    [ClientRpc]
    void RpcUpdateObjectState()
    {
        // update object
        Debug.Log("Object interacted with by player!");
    }
}
