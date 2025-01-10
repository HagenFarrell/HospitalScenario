using Mirror;
using UnityEngine;

public class NetworkPlayer : NetworkBehaviour
{
    public GameObject playerCamera;  

    void Start()
    {
        if (isLocalPlayer)
        {
            playerCamera.SetActive(true);  
        }
        else
        {
            playerCamera.SetActive(false);  
        }
    }
}
