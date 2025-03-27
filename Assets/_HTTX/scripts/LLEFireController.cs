using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LLEFireController : MonoBehaviour
{
    public KeyCode fireKey = KeyCode.F;          
    public float fireRange = 30f;                 
    public LayerMask losMask;                    

    void Update()
    {
        // Listen for hotkey press
        if (Input.GetKeyDown(fireKey))
        {
            FireAllLLEUnits();
        }
    }

    void FireAllLLEUnits()
    {
        // Find all AI-controlled NPCs
        AIMover[] allUnits = FindObjectsOfType<AIMover>(); 

        foreach (AIMover unit in allUnits)
        {
            if (!unit.IsArmedUnit)
            {
                // Only fire if this unit is marked as armed
                continue;
            }

            if (!HasLineOfSightToHostile(unit.transform))
            {
                Debug.Log($"{unit.gameObject.name} has no LOS to hostile");
                // Skip firing if no clear line of sight
                continue; 
            }

            Animator animator = unit.GetComponent<Animator>();
            if (animator == null)
            {
                continue;
            }

            // Trigger firing animation
            animator.SetTrigger("FireWeapon");
            Debug.Log($"{unit.gameObject.name} triggered fire");

            // Reset the trigger next frame to avoid animation stuck
            StartCoroutine(ResetFireTrigger(animator));
        }
    }

    // Checks if this LLE unit has LOS to any hostile
    bool HasLineOfSightToHostile(Transform unit)
    {
        // Grab all hostiles tagged either "Villain" or "OutsideVillain"
        GameObject[] hostiles = GameObject.FindGameObjectsWithTag("Villain");
        List<GameObject> allHostiles = new List<GameObject>(hostiles);
        allHostiles.AddRange(GameObject.FindGameObjectsWithTag("OutsideVillain"));

        foreach (GameObject hostile in allHostiles)
        {
            Vector3 dir = (hostile.transform.position - unit.position).normalized;
            float dist = Vector3.Distance(unit.position, hostile.transform.position);

            // Raycast slightly above ground (1.5f)
            if (Physics.Raycast(unit.position + Vector3.up * 1.5f, dir, out RaycastHit hit, dist, losMask))
            {
                if (hit.collider.gameObject == hostile)
                {
                    // Clear LOS
                    return true; 
                }
            }
        }

        // No visible hostile
        return false; 
    }

    // Resets the FireWeapon animation trigger one frame later
    IEnumerator ResetFireTrigger(Animator animator)
    {
        yield return new WaitForSeconds(0.1f); 
        animator.ResetTrigger("FireWeapon");
    }
}
