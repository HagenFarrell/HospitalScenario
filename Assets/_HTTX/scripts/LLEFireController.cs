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
        // Only the Instructor role can trigger this
        if (!IsLocalPlayerInstructor())
        {
            return;
        }

        if (Input.GetKeyDown(fireKey))
        {
            Debug.Log("Instructor triggered fire command.");
            FireAllLLEUnits();
        }
    }

    void FireAllLLEUnits()
    {
        AIMover[] allUnits = FindObjectsOfType<AIMover>();
        int totalUnits = 0;
        int firedUnits = 0;

        foreach (AIMover unit in allUnits)
        {
            // Skip if this NPC isn’t armed or isn’t Law Enforcement
            if (!unit.IsArmedUnit || unit.tag != "LawEnforcement")
            {
                continue;
            }

            totalUnits++;

            GameObject visibleHostile = GetVisibleHostile(unit.transform);
            if (visibleHostile != null)
            {
                Animator unitAnimator = unit.GetComponent<Animator>();
                if (unitAnimator != null)
                {
                    unitAnimator.SetTrigger("FireWeapon");
                    Debug.Log($"{unit.name} fired at {visibleHostile.name}");

                    Animator hostileAnimator = visibleHostile.GetComponent<Animator>();
                    if (hostileAnimator != null)
                    {
                        hostileAnimator.SetTrigger("Killed Holding Gun");
                    }

                    StartCoroutine(ResetFireTrigger(unitAnimator));
                    firedUnits++;
                }
            }
        }

        Debug.Log($"Firing complete. Units checked: {totalUnits}, Fired: {firedUnits}");
    }

    // Checks for visible hostiles using raycasting (LOS check)
    GameObject GetVisibleHostile(Transform unit)
    {
        GameObject[] allHostiles = GameObject.FindGameObjectsWithTag("Villains");
        List<GameObject> totalHostiles = new List<GameObject>(allHostiles);
        totalHostiles.AddRange(GameObject.FindGameObjectsWithTag("OutsideVillains"));

        foreach (GameObject hostile in totalHostiles)
        {
            Vector3 direction = (hostile.transform.position - unit.position).normalized;
            float distance = Vector3.Distance(unit.position, hostile.transform.position);

            // Raycast slightly above ground level to avoid obstacles like floors
            if (Physics.Raycast(unit.position + Vector3.up * 1.5f, direction, out RaycastHit hit, distance, losMask))
            {
                if (hit.collider.gameObject == hostile)
                {
                    return hostile;
                }
            }
        }

        return null;
    }

    IEnumerator ResetFireTrigger(Animator animator)
    {
        yield return new WaitForSeconds(0.1f);

        animator.ResetTrigger("FireWeapon");

        // Force Animator to return to neutral
        animator.Rebind();         // Completely resets the animator's state machine
        animator.Update(0f);       // Forces immediate update

        Debug.Log("🔁 Animator trigger reset and rebound.");
        Debug.Log($"🎞️ Current state: {animator.GetCurrentAnimatorStateInfo(0).IsName("Idle_Standing 0")}");

    }

    // Confirms whether the local player is the Instructor
    bool IsLocalPlayerInstructor()
    {
        if (Player.LocalPlayerInstance == null)
        {
            return false;
        }

        return Player.LocalPlayerInstance.getPlayerRole() == Player.Roles.Instructor;
    }
}
