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
        return (Input.touchCount > 0) && EventSystem.current.IsPointerOverGameObject(GetTouchId());
    }

    private int GetTouchId()
    {
        #if UNITY_ANDROID || UNITY_IOS
        return Input.GetTouch(0).fingerId;
        #else
        return -1; // Mouse pointer
        #endif
    }
}