using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class CustomJoystick : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
{
    [Header("Visuals")]
    public RectTransform background;
    public RectTransform handle;
    public float handleRange = 1f;
    
    [Header("Behavior")] 
    public bool returnsToCenter = true;
    public bool isLookJoystick = false;


    [Header("Look Control Settings")]
    public float responseCurve = 2.0f;
    public float deadzone = 0f; // optional deadzone (off)

    private Vector2 inputVector = Vector2.zero;
    private Vector2 joystickCenter;
    private bool isActive = false;
    [SerializeField] private CanvasGroup canvasGroup;

    void Start()
    {
        joystickCenter = background.position;
        if (returnsToCenter) handle.anchoredPosition = Vector2.zero;
    }

    void Awake()
    {
        if(canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        SetRaycastBlocking(true);
    }

    public void SetRaycastBlocking(bool shouldBlock)
    {
        canvasGroup.blocksRaycasts = shouldBlock;
        // Keep interactable true so events still work
        canvasGroup.interactable = true; 
    }

    public float GetSmoothedHorizontal()
    {
        float raw = Horizontal;
        if (Mathf.Abs(raw) < deadzone) return 0f;
        return Mathf.Sign(raw) * Mathf.Pow(Mathf.Abs(raw), responseCurve);
    }

    public float GetSmoothedVertical()
    {
        float raw = Vertical;
        if (Mathf.Abs(raw) < deadzone) return 0f;
        return Mathf.Sign(raw) * Mathf.Pow(Mathf.Abs(raw), responseCurve);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 direction = eventData.position - joystickCenter;
        float radius = background.sizeDelta.x * 0.5f * handleRange;
        
        // Handle position
        if (direction.magnitude > radius)
        {
            direction = direction.normalized * radius;
        }
        handle.anchoredPosition = direction;
        
        // Normalized input
        inputVector = direction / radius;
        isActive = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (returnsToCenter) handle.anchoredPosition = Vector2.zero;
        inputVector = Vector2.zero;
        isActive = false;
    }

    public float Horizontal => inputVector.x;
    public float Vertical => inputVector.y;
    public bool IsActive => isActive;
}