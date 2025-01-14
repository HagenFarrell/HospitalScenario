using UnityEngine;

public class TriggerDoor : MonoBehaviour
{
    private Animator _doorAnimator;

    private void Start()
    {
        _doorAnimator = GetComponent<Animator>();
    }
    private void OnTriggerEnter(Collider other)
    {
        // Checks if player is entering box collider.
        if (other.CompareTag("Player"))
        {
            _doorAnimator.SetTrigger("Open");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Checks if player is leaving box collider.
        if (other.CompareTag("Player"))
        {
            _doorAnimator.SetTrigger("Close");
        }
    }
}