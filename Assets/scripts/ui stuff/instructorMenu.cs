using UnityEngine;
using UnityEngine.UI;

public class instructorMenu : MonoBehaviour
{
    public ToggleDomeVisibility domeScript; // Reference to the dome's script

    public void ToggleDome()
    {
        if (domeScript != null)
        {
            domeScript.CmdToggleVisibility(); // Call the networked function
        }
        else
        {
            Debug.LogWarning("Dome script reference is missing!");
        }
    }
}
