using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void StartSimulation() 
    {
        SceneManager.LoadScene("Hospital");
        Debug.Log("loading scene");
    }

    public void StartMultiplayer()
    {
        SceneManager.LoadScene("MultiplayerScene");
        Debug.Log("load multiplayer scene");
    }
    public void openOptions()
    {
        SceneManager.LoadScene("OptionsMenu");

    }
    public void returnToMainMenu()
    {
        SceneManager.LoadScene("MainMenuScene");
    }
    public void returnToOptionsMenu()
    {
        SceneManager.LoadScene("OptionsMenu");
    }
    public void openVolume()
    {
        SceneManager.LoadScene("VolumeMenu");
    }
    public void QuitGame() 
    {
        Application.Quit();
        Debug.Log("Game Closed!"); 
    }
}
