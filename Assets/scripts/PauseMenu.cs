using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections; // Needed for IEnumerator

#if UNITY_EDITOR
using UnityEditor;
#endif

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenuUI;
    public Button pauseButton;
    private bool isPaused = false;

    void Start()
    {
        // Ensure the pause menu starts hidden
        pauseMenuUI.SetActive(false);

        // Add button listener
        pauseButton.onClick.AddListener(TogglePauseMenu);
        Time.timeScale = 1;
    }

    public void TogglePauseMenu()
    {
        isPaused = !isPaused;
        pauseMenuUI.SetActive(isPaused);

        if (isPaused)
        {
            Debug.Log("Pausing game...");
            Time.timeScale = 0;
        }
        else
        {
            Debug.Log("Resuming game...");
            Time.timeScale = 1;
        }
    }

    public void ResumeGame()
    {
        isPaused = false;
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1;
    }

    public void QuitToMainMenu()
    {
        Debug.Log("Returning to Main Menu...");
        Time.timeScale = 1;
        SceneManager.LoadScene("MainMenuScene");  // Make sure scene name matches
    }

    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();

#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#endif
    }
}
