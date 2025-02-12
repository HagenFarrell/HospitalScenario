using Photon.Pun;
using UnityEngine;

public class MultiplayerManager : MonoBehaviourPunCallbacks
{
    void Start()
    {
        PhotonNetwork.ConnectUsingSettings(); // Connect to Photon
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Photon Server");
    }

    public void CreateRoom()
    {
        PhotonNetwork.CreateRoom("MyRoom");
    }

    public void JoinRoom()
    {
        PhotonNetwork.JoinRoom("MyRoom");
    }
}
