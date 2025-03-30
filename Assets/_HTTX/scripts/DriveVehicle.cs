using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DriveVehicle : MonoBehaviour{
    private AIMover mover;
    [Header ("Vehicle Attributes")]
    [SerializeField] private Player ControlPlayer;
    [SerializeField] private int MaxCapacity;
    [SerializeField] private float entryRadius;

    private bool isActiveVehicle;
    private float oldSpeed;
    private float driveSpeed;
    
    List<GameObject> passengers;
    private void Start(){
        if(ControlPlayer == null) ControlPlayer = FindObjectOfType<Player>();
        if(ControlPlayer == null) Debug.LogError("PlayerController null in DriveVehicle");
        passengers = new List<GameObject>();
    }
    private void Update(){
        if(ControlPlayer == null) ControlPlayer = FindObjectOfType<Player>();
        if(ControlPlayer == null) {
            Debug.LogError("PlayerController null in DriveVehicle");
            return;
        }
        if(Input.GetKeyDown(KeyCode.K)){ // enter vehicle
            Debug.Log($"Attempting to enter {this.gameObject}");
            List<GameObject> SelectedChars = ControlPlayer.GetSelectedChars();
            TryEnterVehicle(SelectedChars);
        }
        if(Input.GetKeyDown(KeyCode.L)){ // exit vehicle
            Debug.Log($"Exiting {this.gameObject}");
            List<GameObject> SelectedChars = ControlPlayer.GetSelectedChars();
            foreach(GameObject unit in passengers){
                ExitVehicle(unit);
            }
        }
        if(isActiveVehicle && passengers.Count > 0){
            UpdateVehiclePosition(passengers[0]);
        }
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, entryRadius);
    }
    private void ExitVehicle(GameObject PlayerUnit){
        Debug.LogWarning($"{PlayerUnit} exiting vehicle: {this.gameObject}");
        Vector3 exitPosition = PlayerUnit.transform.position;
        exitPosition.x = transform.position.x + 2.5f;
        PlayerUnit.transform.position = exitPosition;
        mover.UpdateSpeed(oldSpeed);
        toggleRenderer(true, PlayerUnit);
        passengers.Remove(PlayerUnit);
        isActiveVehicle = false;
    }
    private void TryEnterVehicle(List<GameObject> SelectedChars)
    {
        // Check if ANY selected unit is close enough AND can drive this vehicle
        bool canEnter = false;
        foreach (GameObject unit in SelectedChars)
        {
            Debug.Log($"{unit} is close enough?");
            float distance = Vector3.Distance(unit.transform.position, transform.position);
            if (distance <= entryRadius && CanDriveThis(unit))
            {
                canEnter = true;
                break;
            }
        }

        if (!canEnter)
        {
            Debug.LogWarning("No valid units in range!");
            return;
        }

        // Board all valid units (up to max capacity)
        foreach (GameObject unit in SelectedChars)
        {
            if (passengers.Count >= MaxCapacity) break;
            
            if (CanDriveThis(unit)) 
            {
                EnterVehicle(unit);
            }
        }
    }
    private void EnterVehicle(GameObject PlayerUnit){
        if(passengers.Count >= MaxCapacity){
            Debug.LogWarning($"{this.gameObject} is at max capacity: {MaxCapacity}");
            return;
        }
        SetVars(PlayerUnit);
        toggleRenderer(false, PlayerUnit);
        mover.UpdateSpeed(driveSpeed);
        passengers.Add(PlayerUnit);
        isActiveVehicle = true;
    }
    private void UpdateVehiclePosition(GameObject PlayerUnit){
        transform.position = PlayerUnit.transform.position;
        float YRotation = PlayerUnit.transform.eulerAngles.y;
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, YRotation, transform.eulerAngles.z);
        PlayerUnit.GetComponent<Animator>().SetBool("IsRunning", false);
    }
    private bool CanDriveThis(GameObject PlayerUnit){
        return (PlayerUnit.CompareTag("LawEnforcement") && gameObject.CompareTag("LawEnforcementVehicle")) ||
            (PlayerUnit.CompareTag("FireDepartment") && gameObject.CompareTag("FireDepartmentVehicle"));
    }
    private void SetVars(GameObject PlayerUnit){
        mover = PlayerUnit.gameObject.GetComponent<AIMover>();
        if(mover == null){
            Debug.LogError("mover null in DriveVehicle");
            return;
        }
        oldSpeed = mover.GetSpeed();
        driveSpeed = oldSpeed + 5;
    }
    private void toggleRenderer(bool toggle, GameObject PlayerUnit){
        foreach (Renderer childRenderer in PlayerUnit.GetComponentsInChildren<Renderer>()) {
            childRenderer.enabled = toggle;
        }
    }
    
}