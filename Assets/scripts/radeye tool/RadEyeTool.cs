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

    private void InitializeToolForLocalPlayer()
    {
        Debug.Log("RadEyeTool now belongs to the local player!");

        AssignRadiationSource();

        gameObject.SetActive(true);
        renderers = GetComponentsInChildren<MeshRenderer>();
        isActive = false;
        SetToolVisibility(isActive);
    }

    void Update()
    {
        if (!isToolLocal || player == null) return;

        transform.position = player.transform.position + player.transform.right * 0.0f + player.transform.up * -0.35f + player.transform.forward * 0.5f;

        if (Input.GetKeyDown(KeyCode.R))
        {
            isActive = !isActive;
            SetToolVisibility(isActive);
        }

        if (isActive && Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.CompareTag("FireDepartment") || hit.collider.CompareTag("LawEnforcement"))
                {
                    float radiationLevel = CalculateRadiation(hit.point);
                    DisplayRadiation(radiationLevel, true);
                    Debug.Log($"Radiation displayed for: {hit.collider.name} (Tag: {hit.collider.tag})");
                }
                else
                {
                    Debug.Log($"Ignoring object: {hit.collider.name} (Tag: {hit.collider.tag})");
                    DisplayRadiation(0f, false);
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
            radiationDisplay.text = validTarget ? $"{radiation:F2} R" : "Select NPC";
            radiationDisplay.gameObject.SetActive(true);
        }
    }

    public bool IsActive()
    {
        return isActive;
    }

    public void AssignPlayer(uint ownerNetId)
    {
        if (NetworkIdentity.spawned.TryGetValue(ownerNetId, out NetworkIdentity identity))
        {
            player = identity.GetComponent<Player>();
            if (player != null && player.isLocalPlayer)
            {
                isToolLocal = true;
                mainCamera = player.GetComponentInChildren<Camera>();
                InitializeToolForLocalPlayer();
                Debug.Log("Assigned correct local player to RadEyeTool.");
            }
            else
            {
                Debug.Log("RadEyeTool: Player found, but not local. Tool won't respond to input.");
            }
        }
        else
        {
            Debug.LogError($"RadEyeTool: Could not find player with netId {ownerNetId}");
        }
    }
}
