using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class MobileInputHandler : MonoBehaviour
{
    private Player player;
    [SerializeField] private LLEFireController fireController;
    [SerializeField] private GameObject carcar;
    private List<DriveVehicle> playerVehicles;
    public static float verticalInput = 0f;
    void Awake(){
        player = Player.LocalPlayerInstance;
        if(player == null) {
            Debug.LogWarning("Player null in input handler, searching..");
            player = FindObjectOfType<Player>();
        }
        initVehicles();
    }
    private void initVehicles(){
        int children = carcar.transform.childCount;
        playerVehicles = new List<DriveVehicle>();
        for(int i=0; i<children; i++){
            playerVehicles.Add(carcar.transform.GetChild(i).GetComponent<DriveVehicle>());
        }
    }
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
    
    public void OnNextPhase(){
        if(player == null) player = FindObjectOfType<Player>();
        if(player.getPlayerRole() == Player.Roles.Instructor){
            player.MoveNextPhase();
        }
    }
    public void OnPrevPhase(){
        if(player == null) player = FindObjectOfType<Player>();
        if(player.getPlayerRole() == Player.Roles.Instructor){
            player.MovePrevPhase();
        }
        
    }
    public void OnBubbleToggle(){
        if(player == null) player = FindObjectOfType<Player>();
        if(player.getPlayerRole() == Player.Roles.Instructor){
            player.ToggleBubble();
        }
    }
    public void OnRoofToggle(){
        if(player == null) player = FindObjectOfType<Player>();
        if(player.getPlayerRole() == Player.Roles.Instructor){
            player.ToggleRoof();
        }
    }
    public void OnFireButton(){
        if(player == null) player = FindObjectOfType<Player>();
        if(player.getPlayerRole() == Player.Roles.Instructor){
            fireController.ExternalFire();
        }
    }
    public void OnEnterVehicle(){
        foreach(DriveVehicle vehicle in playerVehicles){
            vehicle.TryEnterVehicle(player.GetSelectedChars());
        }
    }
    public void OnExitVehicle(){
        foreach(DriveVehicle vehicle in playerVehicles){
            vehicle.TryExitVehicle(vehicle.passengers);
        }
    }
    public void OnDeselectAll(){
        player.DeselectAll();
    }
    public void OnEgress1(){
        PhaseManager.Instance.SetEgressPhase(1);
    }
    public void OnEgress2(){
        PhaseManager.Instance.SetEgressPhase(2);
    }
    public void OnEgress3(){
        PhaseManager.Instance.SetEgressPhase(3);
    }
    public void OnEgress4(){
        PhaseManager.Instance.SetEgressPhase(4);
    }
    public void OnEgressRand(){
        int temp = Random.Range(1,5);
        PhaseManager.Instance.SetEgressPhase(temp);
    }
}