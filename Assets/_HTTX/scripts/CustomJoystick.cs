using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CustomJoystick : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
{
    [Header("References")]
    public RectTransform joystickBackground;
    public RectTransform joystickHandle;
    
    [Header("Settings")]
    [Range(0.1f, 2f)] public float maxHandleDistance = 1f;
    public bool normalizeInput = false;

    private CanvasGroup canvasGroup;
    private Vector2 inputVector = Vector2.zero;
    private Vector2 joystickCenter;
    private bool isDragging = false;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        
        #if !UNITY_ANDROID && !UNITY_IOS
        // Disable completely on non-mobile platforms
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        this.enabled = false; // Disables the entire script
        return;
        #endif
        
        joystickCenter = joystickBackground.position;
        joystickHandle.anchoredPosition = Vector2.zero;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 touchPosition = eventData.position;
        Vector2 direction = touchPosition - joystickCenter;
        
        // Calculate input based on distance from center
        float distance = direction.magnitude;
        float normalizedDistance = Mathf.Clamp01(distance / (joystickBackground.sizeDelta.x * 0.5f));
        
        if (distance > maxHandleDistance * joystickBackground.sizeDelta.x * 0.5f)
        {
            direction = direction.normalized * maxHandleDistance * joystickBackground.sizeDelta.x * 0.5f;
        }
        
        // Position the "stick"
        joystickHandle.anchoredPosition = direction;
        
        // Calculate input values
        if (normalizeInput)
        {
            inputVector = direction.normalized * normalizedDistance;
        }
        else
        {
            inputVector = direction / (joystickBackground.sizeDelta.x * 0.5f * maxHandleDistance);
        }
        
        isDragging = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        inputVector = Vector2.zero;
        joystickHandle.anchoredPosition = Vector2.zero;
        isDragging = false;
    }

    // Public properties to access input
    public float Horizontal => inputVector.x;
    public float Vertical => inputVector.y;
    public float InputMagnitude => inputVector.magnitude; // For speed control
    public bool IsDragging => isDragging;
}