using UnityEngine;
using UnityEngine.EventSystems;

public class MobileUIManager : MonoBehaviour {
    public static MobileUIManager Instance { get; private set; }

    [SerializeField] private GameObject mobileUIContainer;
    [SerializeField] public CustomJoystick moveJoystick;
    [SerializeField] public CustomJoystick lookJoystick;
    [SerializeField] public GameObject InstructorButtons;
    [SerializeField] public GameObject EgressButtons;
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

    void InitializeUI() {
        mobileUIContainer.SetActive(false);
        LLEFDButtons.SetActive(false);
        EgressButtons.SetActive(false);
        InstructorButtons.SetActive(false);
        #if UNITY_ANDROID || UNITY_IOS
        enable = true;
        // EnableMobileUI();
        #else
        enable = false;
        // EnableMobileUI();
        #endif
    }

    public void EnableMobileUI() {
        mobileUIContainer.SetActive(enable);

        moveJoystick.SetRaycastBlocking(enable);
        lookJoystick.SetRaycastBlocking(enable);
    }
    public void ToggleEgressUI(bool setActive){
        if(EgressButtons == null){
            Debug.LogWarning("egress buttons null");
        }
        EgressButtons.SetActive(setActive);
    }
    public void RoleBasedUI(Player.Roles role) {
        bool isValidRole = 
            role == Player.Roles.Instructor;
        InstructorButtons.SetActive(isValidRole);

        isValidRole = 
            role == Player.Roles.LawEnforcement
             || role == Player.Roles.FireDepartment
             || role == Player.Roles.Instructor;
        LLEFDButtons.SetActive(isValidRole);

        #if UNITY_ANDROID || UNITY_IOS
        enable = 
            role != Player.Roles.None
             && role != Player.Roles.Dispatch;
        #else
        enable = false;
        #endif

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
}