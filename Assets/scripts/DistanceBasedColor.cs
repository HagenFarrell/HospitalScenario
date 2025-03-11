using UnityEngine;

public class DistanceBasedColor : MonoBehaviour
{
    public GameObject MRIMachine; // Assign the MRIMachine in the Inspector
    public Renderer cylinderRenderer; // Assign the Renderer of the cylinder in the Inspector
    public Transform center; // Assign the Center GameObject for the cylinder in the Inspector
    public float maxDistance = 50f; // The distance at which the object is fully green
    public float transparency = 0.5f; // Transparency value (0 = fully transparent, 1 = fully opaque)

    void Start()
    {
        // Ensure the material's rendering mode is Transparent
        Material material = cylinderRenderer.material;
        material.SetFloat("_Mode", 3); // Set rendering mode to Transparent
        material.renderQueue = 3000;  // Set render queue for transparency
    }

    void Update()
    {

        if(cylinderRenderer == null) Debug.LogError("cylinderRenderer null");
        
        if(MRIMachine == null) Debug.LogError("MRIMachine null");
        
        if(center == null) Debug.LogError("Center null");

        if (MRIMachine != null && cylinderRenderer != null && center != null)
        {
            // Calculate the distance between the Center GameObject and the MRIMachine
            float distance = Vector3.Distance(center.position, MRIMachine.transform.position);

            // Normalize the distance to a range of 0 to 1
            float t = Mathf.Clamp01(distance / maxDistance);

            // Interpolate between red (close) and green (far)
            Color color;
            if (t < 0.5f)
            {
                // Red to Yellow transition in the first half of the distance range
                color = Color.Lerp(Color.red, Color.yellow, t * 2f);
            }
            else
            {
                // Yellow to Green transition in the second half of the distance range
                color = Color.Lerp(Color.yellow, Color.green, (t - 0.5f) * 2f);
            }

            // Apply the color and maintain transparency
            color.a = transparency; // Set the alpha value to maintain transparency
            cylinderRenderer.material.color = color;

            // Debug the calculated distance and normalized value
            //Debug.Log($"Distance: {distance}, Normalized: {t}, Color: {color}");
        }
        else
        {


            Debug.LogError("MRIMachine, cylinderRenderer, or Center is not assigned!");
        }
    }
}
