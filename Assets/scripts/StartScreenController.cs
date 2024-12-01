using UnityEngine;
using UnityEngine.SceneManagement;

public class StartScreenController : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene("Hospital");
    }

    public void openOptions()
    {
        SceneManager.LoadScene("Options Screen");
    }
}
