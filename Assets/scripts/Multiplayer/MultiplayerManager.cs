using Mirror;
using UnityEngine;

public class MultiplayerManager : NetworkManager
{
    public GameObject PlayerPrefab;

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        Vector3 spawnPosition = new Vector3(0, 0, 0); // modify based on spawn logic
        GameObject player = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
        NetworkServer.AddPlayerForConnection(conn, player);
    }

    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        base.OnServerConnect(conn);
        Debug.Log("Player connected: " + conn);
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        base.OnServerDisconnect(conn);
        Debug.Log("Player disconnected: " + conn);
    }
}
