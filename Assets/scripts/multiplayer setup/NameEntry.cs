using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class NameEntry : MonoBehaviour
{
    public InputField nameInput;

    public void SaveNameAndJoinLobby()
    {
        string playerName = nameInput.text;
        if (string.IsNullOrEmpty(playerName))
        {
            Debug.Log("Name cannot be empty!");
            return;
        }

        PlayerPrefs.SetString("PlayerName", playerName); // Save name locally
        SceneManager.LoadScene("MultiplayerMenu"); // Load the lobby scene
        Debug.Log("Player name: " + playerName);
    }
}
