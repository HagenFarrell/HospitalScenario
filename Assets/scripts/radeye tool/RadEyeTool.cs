using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class RadEyeTool : NetworkBehaviour
{
    public Transform source; // Automatically assigned radiation source
    public AnimationCurve radiationGraph; // Assign this in Unity to define radiation levels
    public Text radiationDisplay; // UI text on RadEye Tool
    public Camera mainCamera;

    private bool isActive = false; // Tool starts OFF
    private MeshRenderer[] renderers; // Store all renderers in the tool
    private Collider toolCollider; // For raycast detection

    void Awake()
    {
        // Immediately hide tool on spawn before Start() runs
        gameObject.SetActive(false);
    }

    void Start()
    {
        if (!isLocalPlayer) return; // Ensure only local player manages their own tool

        AssignRadiationSource();

        // Get all renderers & colliders
        renderers = GetComponentsInChildren<MeshRenderer>();
        toolCollider = GetComponent<Collider>();

        // Ensure the tool remains off at start
        ToggleRadEyeTool(false);
    }

    void Update()
    {
        if (!isLocalPlayer) return; // Prevents remote players from toggling your tool

        // Toggle RadEye Tool when pressing "R"
        if (Input.GetKeyDown(KeyCode.R))
        {
            isActive = !isActive;
            ToggleRadEyeTool(isActive);
        }

        // Only check for clicks if tool is active
        if (isActive && Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                float radiationLevel = CalculateRadiation(hit.point);
                DisplayRadiation(radiationLevel);
            }
        }
    }

    void AssignRadiationSource()
    {
        GameObject foundSource = GameObject.FindWithTag("RadiationSource"); // Finds the single source

        if (foundSource != null)
        {
            source = foundSource.transform;
            Debug.Log($"Assigned radiation source: {source.name}");
        }
        else
        {
            Debug.LogError("Radiation source not found! Make sure the source has the 'RadiationSource' tag.");
        }
    }

    float CalculateRadiation(Vector3 objectPosition)
    {
        if (source == null)
        {
            Debug.LogWarning("Radiation source is not assigned yet.");
            return 0f; // No source assigned yet
        }

        float distance = Vector3.Distance(source.position, objectPosition);
        return radiationGraph.Evaluate(distance); // Get radiation from the graph
    }

    void DisplayRadiation(float radiation)
    {
        if (radiationDisplay != null)
        {
            radiationDisplay.text = $"{radiation:F2} R";
            radiationDisplay.gameObject.SetActive(true); // Ensure UI is enabled
        }

        Debug.Log($"Radiation at object: {radiation:F2} R");
    }

    void ToggleRadEyeTool(bool state)
    {
        if (!isLocalPlayer) return; // Only allow local player to toggle

        gameObject.SetActive(state); // Properly toggle the whole tool

        // Enable/disable only visuals, NOT the script
        foreach (MeshRenderer renderer in renderers)
        {
            renderer.enabled = state;
        }

        // Enable/disable UI separately
        if (radiationDisplay != null)
        {
            radiationDisplay.gameObject.SetActive(state);
        }

        // Disable the collider so it doesn't interact when off
        if (toolCollider != null)
        {
            toolCollider.enabled = state;
        }
    }
}
