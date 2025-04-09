using UnityEngine;
using UnityEngine.EventSystems;

public class MobileInputHandler : MonoBehaviour
{
    public static float verticalInput = 0f;
    
    public void OnUpPressed()
    {
        verticalInput = 1f; 
    }
    
    public void OnDownPressed()
    {
        verticalInput = -1f; 
    }
    
    public void OnRelease()
    {
        verticalInput = 0f;
    }
}