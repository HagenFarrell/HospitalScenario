using UnityEngine;

public class CameraView : MonoBehaviour {
    public Transform target; // player
    public Vector3 offset = new Vector3(0, 20, 0);

    void LateUpdate() {
        if (target != null) {
            transform.position = target.position + offset;
        }
    }
}