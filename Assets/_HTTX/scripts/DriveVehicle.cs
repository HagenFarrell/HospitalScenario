using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DriveVehicle : MonoBehaviour{
    public bool isDriving;
    private AIMover mover;
    private float oldSpeed;
    private float driveSpeed;
    private void OnTriggerEnter(Collider player){
        isDriving = false;
        bool isWrongDriver = 
            (player.CompareTag("LawEnforcement") != gameObject.CompareTag("LawEnforcementVehicle")) ||
            (player.CompareTag("FireDepartment") != gameObject.CompareTag("FireDepartmentVehicle"));

        // players drive their specific vehicles, and cant drive multiple vehicles.
        if (isWrongDriver || isDriving) {
            // Debug.LogWarning($"wrong vehicle for: {player} when entering {this.gameObject}");
            return;
        } 

        // Debug.Log($"\"i'm in,\" said {player} when entering {this.gameObject}");
        GameObject playerObject = player.gameObject;
        SetVars(playerObject);
        toggleRenderer(false, playerObject);
        mover.UpdateSpeed(driveSpeed);
        StartCoroutine(Driving(playerObject));
    }

    private IEnumerator Driving(GameObject player) {
        while(true){
            if (Input.GetKeyDown(KeyCode.V)) {
                Debug.LogWarning($"{player} exiting vehicle: {this.gameObject}");
                Vector3 exitPosition = player.transform.position;
                exitPosition.x = transform.position.x + 2.5f;
                player.transform.position = exitPosition;
                mover.UpdateSpeed(oldSpeed);
                toggleRenderer(true, player);
                isDriving = false;
                yield break;
            }
            isDriving = true;
            transform.position = player.transform.position;
            float YRotation = player.transform.eulerAngles.y;
            transform.eulerAngles = new Vector3(transform.eulerAngles.x, YRotation, transform.eulerAngles.z);
            player.GetComponent<Animator>().SetBool("IsRunning", false);
            yield return null;
        }
    }
    private void SetVars(GameObject player){
        mover = player.gameObject.GetComponent<AIMover>();
        if(mover == null){
            Debug.LogError("mover null in DriveVehicle");
            return;
        }
        oldSpeed = mover.getSpeed();
        driveSpeed = oldSpeed + 5;
    }
    private void toggleRenderer(bool toggle, GameObject player){
        foreach (Renderer childRenderer in player.GetComponentsInChildren<Renderer>()) {
            childRenderer.enabled = toggle;
        }
    }
}