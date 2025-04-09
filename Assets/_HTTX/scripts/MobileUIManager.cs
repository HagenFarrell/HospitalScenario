using UnityEngine;

public class MobileUIManager : MonoBehaviour
{
    public static MobileUIManager Instance { get; private set; }
    
    public CustomJoystick moveJoystick;
    public CustomJoystick lookJoystick;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Initialize mobile UI state
            #if !UNITY_ANDROID && !UNITY_IOS
            moveJoystick?.gameObject.SetActive(false);
            lookJoystick?.gameObject.SetActive(false);
            #endif
        }
        else
        {
            Destroy(gameObject);
        }
    }
}