using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Linq;

public class DriveVehicle : NetworkBehaviour{
    public static DriveVehicle Instance {get; private set; }
    private bool isMirrorInitialization = false;
    [Header ("Vehicle Attributes")]
    [SerializeField] private Player ControlPlayer;
    [SerializeField] private int MaxCapacity;
    [SerializeField] private float entryRadius;
    [SerializeField] private float driveSpeed;

    private bool isActiveVehicle;
    private float oldSpeed;
    
    
    List<GameObject> passengers;
    private void Start(){
        passengers = new List<GameObject>();
    }
    private void Awake()
    {
        // This runs before Mirror's processing
        isMirrorInitialization = true;
        gameObject.SetActive(true); // Force active
        Instance = this;
    }
    private void Update(){
        Instance = this;
        if(ControlPlayer == null) ControlPlayer = FindObjectOfType<Player>();
        if(ControlPlayer == null) {
            // Debug.LogError("PlayerController null in DriveVehicle");
            return;
        }
        if(Input.GetKeyDown(KeyCode.K)){ // enter vehicle
            List<GameObject> SelectedChars = ControlPlayer.GetSelectedChars();
            TryEnterVehicle(SelectedChars);
            return;
        }
        if(Input.GetKeyDown(KeyCode.L)){ // exit vehicle
            isActiveVehicle = false;
            List<GameObject> SelectedChars = ControlPlayer.GetSelectedChars();
            CmdExitVehicle(passengers);
            passengers.Clear();
            if(passengers.Count > 0)
                passengers = new List<GameObject>();
            return;
        }
        if(isActiveVehicle && passengers.Count > 0){
            CmdUpdateVehiclePosition(passengers[0]);
            // else UpdateVehiclePosition(passengers[0]);
        }
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, entryRadius);
    }
    private void TryEnterVehicle(List<GameObject> SelectedChars)
    {
        // Check if ANY selected unit is close enough AND can drive this vehicle
        bool canEnter = false;
        foreach (GameObject unit in SelectedChars)
        {
            AIMover mover = unit.GetComponent<AIMover>();
            if(mover == null){
                Debug.LogWarning($"AImover null for {unit}");
            }
            if(mover.IsDriving) continue;
            // debug.log($"{unit} is close enough?");
            float distance = Vector3.Distance(unit.transform.position, transform.position);
            if (distance <= entryRadius && CanDriveThis(unit))
            {
                canEnter = true;
                break;
            }
        }

        if (!canEnter)
        {
            // Debug.LogWarning("No valid units in range!");
            return;
        }
        CmdEnterVehicle(SelectedChars);
    }
    [Command(requiresAuthority = false)]
    private void CmdEnterVehicle(List<GameObject> PlayerUnits){
        ToggleRing();
        RpcEnterVehicle(PlayerUnits);
    }
    [ClientRpc]
    private void RpcEnterVehicle(List<GameObject> PlayerUnits){
        ToggleRing();
        EnterVehicle(PlayerUnits);
    }
    private void EnterVehicle(List<GameObject> PlayerUnits){
        if(passengers.Count >= MaxCapacity){
            Debug.LogWarning($"{this.gameObject} is at max capacity: {MaxCapacity}");
            return;
        }
        foreach(GameObject PlayerUnit in PlayerUnits){
            if (!CanDriveThis(PlayerUnit)) continue;

            AIMover mover = PlayerUnit.GetComponent<AIMover>();
            if(mover == null){
                Debug.LogWarning($"mover null for {PlayerUnit}, cannot exit vehicle");
                continue;
            }
            if(mover.IsDriving) continue;
            mover.IsDriving = true;
            
            SetVars(PlayerUnit);
            CmdToggleRenderer(false, PlayerUnit);
            mover.UpdateSpeed(oldSpeed + driveSpeed);
            passengers.Add(PlayerUnit);
            PlayerUnit.transform.position = transform.position;
            float YRotation = transform.transform.eulerAngles.y;
            PlayerUnit.transform.eulerAngles = new Vector3(PlayerUnit.transform.eulerAngles.x, YRotation, PlayerUnit.transform.eulerAngles.z);
            isActiveVehicle = true;
            mover.SetRunning(false);
            
        }
        ToggleRing();
    }
    [Command(requiresAuthority = false)]
    private void CmdExitVehicle(List<GameObject> PlayerUnits){
        ToggleRing();
        RpcExitVehicle(PlayerUnits);
    }
    [ClientRpc]
    private void RpcExitVehicle(List<GameObject> PlayerUnits){
        ToggleRing();
        ExitVehicle(PlayerUnits);
    }
    private void ExitVehicle(List<GameObject> PlayerUnits){
        // Debug.Log($"{PlayerUnit} exiting vehicle: {this.gameObject}");
        isActiveVehicle = false;
        foreach(GameObject PlayerUnit in PlayerUnits){
            // check if roles match up
            if(ControlPlayer.getPlayerRole() != Player.Roles.Instructor && !PlayerUnit.CompareTag(ControlPlayer.getPlayerRole().ToString())) continue;

            AIMover mover = PlayerUnit.GetComponent<AIMover>();
            if(mover == null){
                Debug.LogWarning($"mover null for {PlayerUnit}, cannot exit vehicle");
            }
            mover.IsDriving = false;
            Vector3 exitPosition = PlayerUnit.transform.position;
            exitPosition.x = transform.position.x + entryRadius;
            
            PlayerUnit.transform.position = exitPosition;
            mover.UpdateSpeed(oldSpeed);
            CmdToggleRenderer(true, PlayerUnit);
            mover.StopAllMovement();
            mover.SetRunning(true);
        }
        ToggleRing();
        passengers.Clear();
    }
    [Command(requiresAuthority = false)]
    private void CmdUpdateVehiclePosition(GameObject PlayerUnit){
        if(!isActiveVehicle || passengers.Count <= 0) return;
        RpcUpdateVehiclePosition(PlayerUnit);
    }
    [ClientRpc]
    private void RpcUpdateVehiclePosition(GameObject PlayerUnit){
        UpdateVehiclePosition(PlayerUnit);
    }
    private void UpdateVehiclePosition(GameObject PlayerUnit){
        if(!isActiveVehicle || passengers.Count <= 0) return;
        AIMover mover = PlayerUnit.GetComponent<AIMover>();
        if(mover == null){
            Debug.LogWarning($"mover null for {PlayerUnit}, cannot update animation");
            return;
        }
        transform.position = PlayerUnit.transform.position;
        float YRotation = PlayerUnit.transform.eulerAngles.y;
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, YRotation, transform.eulerAngles.z);
        mover.SetRunning(false);
    }
    private bool CanDriveThis(GameObject PlayerUnit){
        return (PlayerUnit.CompareTag("LawEnforcement") && gameObject.CompareTag("LawEnforcementVehicle")) ||
            (PlayerUnit.CompareTag("FireDepartment") && gameObject.CompareTag("FireDepartmentVehicle"));
    }
    private void SetVars(GameObject PlayerUnit){
        AIMover mover = PlayerUnit.GetComponent<AIMover>();
        if(mover == null){
            Debug.LogWarning($"mover null for {PlayerUnit}, cannot update animation");
            return;
        }
        oldSpeed = mover.GetSpeed();
        if(oldSpeed > 12) oldSpeed -= driveSpeed;
    }
    [Command(requiresAuthority = false)]
    private void CmdToggleRenderer(bool toggle, GameObject PlayerUnit){
        RpcToggleRenderer(toggle, PlayerUnit);
    }
    [ClientRpc]
    private void RpcToggleRenderer(bool toggle, GameObject PlayerUnit){
        ToggleRenderer(toggle, PlayerUnit);
    }
    private void ToggleRenderer(bool toggle, GameObject PlayerUnit){
        foreach (Renderer childRenderer in PlayerUnit.GetComponentsInChildren<Renderer>()) {
            childRenderer.enabled = toggle;
        }
    }
    private void ToggleRing(){
        GameObject activeRing = this.transform.GetChild(0).gameObject;
        if(activeRing != null) activeRing.SetActive(isActiveVehicle);
        else Debug.LogError($"ring null for {this.gameObject}");
    }
    public void RegisterPlayer(Player player)
    {
        ControlPlayer = player;
    }
    private void OnDisable()
    {
        if (isMirrorInitialization || NetworkServer.active || NetworkClient.active)
        {
            // Debug.Log("PhaseHandling disabled by Mirror (expected during network setup)");
            isMirrorInitialization = false;
            return;
        }
        Debug.LogError("PhaseHandling disabled unexpectedly!");
    }
    
}