using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DriveVehicle : MonoBehaviour{
    private AIMover mover;
    private float oldSpeed;
    private float driveSpeed;
    private void OnTriggerEnter(Collider player){
        bool isWrongDriver = 
            (player.CompareTag("LawEnforcement") != gameObject.CompareTag("LawEnforcementVehicle")) ||
            (player.CompareTag("FireDepartment") != gameObject.CompareTag("FireDepartmentVehicle"));

        if (isWrongDriver) {
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
                exitPosition.x = transform.position.x + 10f;
                player.transform.position = exitPosition;
                mover.UpdateSpeed(oldSpeed);
                toggleRenderer(true, player);
                yield break;
            }
            transform.position = player.transform.position;
            yield return null;
        }
        // yield break;
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