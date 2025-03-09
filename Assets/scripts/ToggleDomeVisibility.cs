using Mirror;
using UnityEngine;

public class ToggleDomeVisibility : NetworkBehaviour
{
    private Renderer[] renderers;
    
    [SyncVar(hook = nameof(OnVisibilityChanged))] // SyncVar to keep visibility updated across clients
    private bool isVisible = false; 

    void Start()
    {
        renderers = GetComponentsInChildren<Renderer>();
        SetVisibility(isVisible); // Ensure visibility matches the current state
    }

    // Public function that clients call to request a toggle
    [Command(requiresAuthority = false)] // Allows any client to call this on the server
    public void CmdToggleVisibility()
    {
        isVisible = !isVisible; // Toggle state on the server
    }

    // This function runs on all clients whenever the SyncVar changes
    private void OnVisibilityChanged(bool oldValue, bool newValue)
    {
        SetVisibility(newValue);
    }

    // Apply the visibility change
    private void SetVisibility(bool visible)
    {
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = visible;
        }
    }
}
