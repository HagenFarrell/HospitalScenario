using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror; // Networking

public class LLEFireController : NetworkBehaviour
{
    public KeyCode fireKey = KeyCode.F;
    public float fireRange = 30f;
    public LayerMask losMask;

    void Update()
    {
        // Only let the *local* Instructor initiate fire
        if (!isLocalPlayer || !IsLocalPlayerInstructor())
        {
            return;
        }

        if (Input.GetKeyDown(fireKey))
        {
            CmdFireCommand();
        }
    }

    // Called on server when the local Instructor presses the fire key
    [Command]
    void CmdFireCommand()
    {
        RpcDoFireOnAllClients();
    }

    // Broadcast to all clients to trigger fire animations
    [ClientRpc]
    void RpcDoFireOnAllClients()
    {
        FireAllLLEUnits();
    }


    void FireAllLLEUnits()
    {
        AIMover[] allUnits = FindObjectsOfType<AIMover>();

        foreach (AIMover unit in allUnits)
        {
            // Skip if this NPC isn’t armed or isn’t Law Enforcement
            if (!unit.IsArmedUnit || unit.tag != "LawEnforcement")
            {
                continue;
            }

            GameObject visibleHostile = GetVisibleHostile(unit.transform);
            if (visibleHostile != null)
            {
                Animator unitAnimator = unit.GetComponent<Animator>();
                Animator hostileAnimator = visibleHostile.GetComponent<Animator>();

                if (unitAnimator != null)
                {
                    unitAnimator.SetTrigger("FireWeapon");

                    if (hostileAnimator != null)
                    {
                        hostileAnimator.SetTrigger("Killed Holding Gun");
                    }
                }
            }
        }
    }

    // Checks for visible hostiles using raycasting (LOS check)
    GameObject GetVisibleHostile(Transform unit)
    {
        List<GameObject> hostiles = new List<GameObject>();
        hostiles.AddRange(GameObject.FindGameObjectsWithTag("Villains"));
        hostiles.AddRange(GameObject.FindGameObjectsWithTag("OutsideVillains"));

        foreach (GameObject hostile in hostiles)
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
    }

    // Confirms whether the local player is the Instructor
    bool IsLocalPlayerInstructor()
    {
        return Player.LocalPlayerInstance != null && Player.LocalPlayerInstance.getPlayerRole() == Player.Roles.Instructor;
    }
}
