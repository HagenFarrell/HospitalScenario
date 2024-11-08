using UnityEngine;
using System.Collections.Generic;
using System.Linq;
public class GamerMovement : MonoBehaviour {
    public float moveSpeed = 5f;
    private Vector3 targetPosition;
    private bool isMoving = false;
    private Camera mainCamera;
    
    void Start() {
        mainCamera = Camera.main;
        targetPosition = transform.position;
    }
    
    void Update() {
        if (Input.GetMouseButtonDown(0)) {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit)) {
                targetPosition = hit.point;
                targetPosition.y = transform.position.y;
                isMoving = true;
            }
        }
    
        if (isMoving) {
            float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
            
            if (distanceToTarget > 0.1f) {
                Vector3 direction = (targetPosition - transform.position).normalized;
                transform.position += direction * moveSpeed * Time.deltaTime;
                
                transform.forward = direction;
            }
            else {
                isMoving = false;
            }
        }
    }
}