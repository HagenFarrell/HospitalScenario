using UnityEngine;
using UnityEngine.SceneManagement;

public class OptionsController : MonoBehaviour
{
    public void exitOptions()
    {
        SceneManager.LoadScene("Start Screen");
    }
}
