#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Waypoints))]
public class WaypointsEditor : Editor
{
    private Transform sourceWaypoint;
    private Transform targetWaypoint;
    private GamePhase requiredPhase = GamePhase.Phase1;
    private bool anyPhase = true;
    private float probability = 1f;
    
    public override void OnInspectorGUI()
    {
        Waypoints waypoint = (Waypoints)target;
        
        // Draw the default inspector
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Add Connection", EditorStyles.boldLabel);
        
        // Fields for adding connections
        sourceWaypoint = (Transform)EditorGUILayout.ObjectField("Source Waypoint", sourceWaypoint, typeof(Transform), true);
        targetWaypoint = (Transform)EditorGUILayout.ObjectField("Target Waypoint", targetWaypoint, typeof(Transform), true);
        
        anyPhase = EditorGUILayout.Toggle("Valid for Any Phase", anyPhase);
        
        // Only show the phase selection if anyPhase is false
        if (!anyPhase)
        {
            requiredPhase = (GamePhase)EditorGUILayout.EnumPopup("Required Phase", requiredPhase);
        }
        
        probability = EditorGUILayout.Slider("Probability", probability, 0.01f, 1f);
        
        // Add connection button
        if (GUILayout.Button("Add Connection"))
        {
            if (sourceWaypoint != null && targetWaypoint != null)
            {
                waypoint.AddConnection(sourceWaypoint, targetWaypoint, requiredPhase, anyPhase, probability);
                EditorUtility.SetDirty(waypoint);
                
                // Reset fields
                targetWaypoint = null;
                anyPhase = true;
                probability = 1f;
            }
            else
            {
                EditorUtility.DisplayDialog("Missing References", 
                    "Please assign both source and target waypoints.", "OK");
            }
        }
    }
}
#endif