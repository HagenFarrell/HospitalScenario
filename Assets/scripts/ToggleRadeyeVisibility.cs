using UnityEngine;

public class ToggleRadeyeVisibility : MonoBehaviour
{
    private Renderer[] renderers;
    private bool isVisible = true;

    void Start()
    {
        // Get all renderers on this GameObject and its children
        renderers = GetComponentsInChildren<Renderer>();
    }

    void Update()
    {
        // Check if the "R" key is pressed
        if (Input.GetKeyDown(KeyCode.R))
        {
            // Toggle visibility
            isVisible = !isVisible;

            // Update the visibility of the object
            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = isVisible;
            }
        }
    }
}
