using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    public GameObject playerListContainer; // Panel holding player names
    public GameObject playerListItemPrefab; // Prefab for each player
    
    private Dictionary<int, GameObject> playerListItems = new Dictionary<int, GameObject>();

    void Start()
    {
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnJoinedRoom()
    {
        UpdatePlayerList();
    }

    void UpdatePlayerList()
    {
        // Clear previous list
        foreach (var item in playerListItems.Values)
        {
            Destroy(item);
        }
        playerListItems.Clear();

        // Add each player to the UI list
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            GameObject listItem = Instantiate(playerListItemPrefab, playerListContainer.transform);
            string role = player.CustomProperties.ContainsKey("Role") ? player.CustomProperties["Role"].ToString() : "Not Selected";
            listItem.GetComponent<Text>().text = player.NickName + " - " + role;
            playerListItems[player.ActorNumber] = listItem;
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdatePlayerList();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdatePlayerList();
    }
}
