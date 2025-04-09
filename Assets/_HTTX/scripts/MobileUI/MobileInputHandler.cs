using UnityEngine;
using UnityEngine.EventSystems;

public class MobileInputHandler : MonoBehaviour
{
    private Player player;
    [SerializeField] private LLEFireController fireController;
    public static float verticalInput = 0f;
    void Awake(){
        player = Player.LocalPlayerInstance;
        if(player == null) {
            Debug.LogWarning("Player null in input handler, searching..");
            player = FindObjectOfType<Player>();
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
        DriveVehicle.Instance.TryEnterVehicle(player.GetSelectedChars());
    }
    public void OnExitVehicle(){
        DriveVehicle.Instance.TryExitVehicle(player.GetSelectedChars());
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