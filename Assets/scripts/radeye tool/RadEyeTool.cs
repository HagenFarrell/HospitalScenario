using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class RadEyeTool : NetworkBehaviour
{
    public Transform source;
    public AnimationCurve radiationGraph;
    public Text radiationDisplay;
    public Camera mainCamera;

    private bool isActive = false;
    private Renderer[] renderers;
    private Player player;
    private bool isToolLocal = false;

    public override void OnStartClient()
    {
        base.OnStartClient();

        // Find the local player
        player = NetworkClient.localPlayer?.GetComponent<Player>();

        if (player != null && player.isLocalPlayer)
        {
            isToolLocal = true;
            Debug.Log("RadEyeTool detected the local player.");

            //  Assign the local player's camera to the tool
            mainCamera = player.GetComponentInChildren<Camera>();
            if (mainCamera == null)
            {
                Debug.LogError(" Could not find Camera on the Player!");
            }
            else
            {
                Debug.Log($" Assigned Camera: {mainCamera.name}");
            }

            InitializeToolForLocalPlayer();
        }
    }


    private void InitializeToolForLocalPlayer()
    {
        Debug.Log("RadEyeTool now belongs to the local player!");

        AssignRadiationSource();

        // Ensure the GameObject is enabled so Update() runs
        gameObject.SetActive(true);

        // Get all renderers & colliders
        renderers = GetComponentsInChildren<MeshRenderer>();

        // Ensure tool starts OFF but Update() is still running
        isActive = false;
        SetToolVisibility(isActive);
    }

    void Update()
    {
        if (!isToolLocal || player == null) return;

        // Keep the tool slightly ahead of the player in the world space
        transform.position = player.transform.position + player.transform.right * 0.0f + player.transform.up * -0.35f + player.transform.forward * 0.5f;

        // Toggle RadEye Tool when pressing "R"
        if (Input.GetKeyDown(KeyCode.R))
        {
            isActive = !isActive;
            SetToolVisibility(isActive);
        }

        // Check for clicks and only update radiation if the clicked object has the correct tag
        if (isActive && Input.GetMouseButtonDown(0)) // Left mouse click
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // Check if the object has the correct tag
                if (hit.collider.CompareTag("FireDepartment") || hit.collider.CompareTag("LawEnforcement"))
                {
                    float radiationLevel = CalculateRadiation(hit.point);
                    DisplayRadiation(radiationLevel, true);
                    Debug.Log($"Radiation displayed for: {hit.collider.name} (Tag: {hit.collider.tag})");
                }
                else
                {
                    Debug.Log($"Ignoring object: {hit.collider.name} (Tag: {hit.collider.tag})");
                    DisplayRadiation(0f, false); // Show "Select NPC" instead of 0 R
                }
            }
        }
    }

    void AssignRadiationSource()
    {
        GameObject grandparent = GameObject.Find("Villain League");
        if (grandparent != null)
        {
            Transform parent = grandparent.transform.Find("Villain 1");
            if (parent != null)
            {
                Transform child = parent.Find("GammaProjector");
                if (child != null && child.CompareTag("RadiationSource"))
                {
                    source = child;
                    Debug.Log($"Assigned radiation source: {source.name}");
                    return;
                }
            }
        }

        Debug.LogError("Radiation source not found! Make sure the hierarchy and tag are correct.");
    }



    void SetToolVisibility(bool state)
    {
        if (renderers == null)
            renderers = GetComponentsInChildren<MeshRenderer>();

        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = state;
        }

        // Ensure the UI is also toggled
        if (radiationDisplay != null)
        {
            radiationDisplay.gameObject.SetActive(state);
        }

        Debug.Log($"RadEye Tool toggled: {(state ? "ON" : "OFF")}");
    }

    float CalculateRadiation(Vector3 objectPosition)
    {
        if (source == null)
        {
            Debug.LogWarning(" Radiation source is not assigned yet.");
            return 0f;
        }

        float distance = Vector3.Distance(source.position, objectPosition);
        float radiation = radiationGraph.Evaluate(distance);

        Debug.Log($" Clicked Object at {objectPosition} | Distance: {distance}m | Radiation: {radiation} R");

        return radiation;
    }


    void DisplayRadiation(float radiation, bool validTarget)
    {
        if (radiationDisplay != null)
        {
            if (validTarget)
            {
                radiationDisplay.text = $"{radiation:F2} R"; // Display the radiation value
            }
            else
            {
                radiationDisplay.text = "Select NPC"; // Show "Select NPC" when clicking an invalid object
            }

            radiationDisplay.gameObject.SetActive(true);
        }
    }

    public bool IsActive()
    {
        return isActive;
    }

}
