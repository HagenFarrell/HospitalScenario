using UnityEngine;
using UnityEngine.EventSystems;

public class MobileUIManager : MonoBehaviour
{
    public static MobileUIManager Instance { get; private set; }

    [SerializeField] private CanvasGroup mobileUICanvasGroup;
    [SerializeField] public CustomJoystick moveJoystick;
    [SerializeField] public CustomJoystick lookJoystick;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeUI();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void InitializeUI()
    {
        #if UNITY_ANDROID || UNITY_IOS
        EnableMobileUI(true);
        #else
        EnableMobileUI(false);
        #endif
    }

    public void EnableMobileUI(bool enable)
    {
        mobileUICanvasGroup.blocksRaycasts = enable;
        mobileUICanvasGroup.interactable = enable;
        mobileUICanvasGroup.alpha = enable ? 1 : 0;

        moveJoystick.SetRaycastBlocking(enable);
        lookJoystick.SetRaycastBlocking(enable);
    }
    public bool IsTouchingUI()
    {
        // Safety check
        if (EventSystem.current == null || !EventSystem.current.IsActive()){
            return false;
        }

        // Handle both mouse and touch input
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            return EventSystem.current.IsPointerOverGameObject(touch.fingerId);
        }
        else
        {
            return EventSystem.current.IsPointerOverGameObject();
        }
    }
}