using UnityEngine;
using UnityEngine.EventSystems;

public class MobileUIManager : MonoBehaviour {
    private Player player;
    public static MobileUIManager Instance { get; private set; }

    [SerializeField] private CanvasGroup mobileUICanvasGroup;
    [SerializeField] public CustomJoystick moveJoystick;
    [SerializeField] public CustomJoystick lookJoystick;
    [SerializeField] public GameObject InstructorButtons;
    [SerializeField] public GameObject LLEFDButtons;
    bool enable;

    void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeUI();
        }
        else {
            Destroy(gameObject);
        }
    }
    public void RegisterPlayer(Player player) {
        this.player = player;
    }

    void InitializeUI() {
        mobileUICanvasGroup.alpha = 0;
        #if UNITY_ANDROID || UNITY_IOS
        enable = true;
        // EnableMobileUI();
        #else
        enable = false;
        // EnableMobileUI();
        #endif
    }

    public void EnableMobileUI() {
        mobileUICanvasGroup.blocksRaycasts = enable;
        mobileUICanvasGroup.interactable = enable;
        mobileUICanvasGroup.alpha = enable ? 1 : 0;

        moveJoystick.SetRaycastBlocking(enable);
        lookJoystick.SetRaycastBlocking(enable);
    }
    public void RoleBasedUI(Player.Roles role) {
        bool isValidRole = 
            role == Player.Roles.Instructor;

        InstructorButtons.SetActive(isValidRole && enable);
        isValidRole = 
            role == Player.Roles.LawEnforcement
             || role == Player.Roles.FireDepartment
             || role == Player.Roles.Instructor;

        LLEFDButtons.SetActive(isValidRole && enable);

        enable = 
            role != Player.Roles.None
             && role != Player.Roles.Dispatch;

        EnableMobileUI();
    }
    public bool IsTouchingUI() {
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

    public Player GetPlayer(){
        return player;
    }
}