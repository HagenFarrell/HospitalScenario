using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror; // Networking

public class LLEFireController : NetworkBehaviour
{
    public KeyCode fireKey = KeyCode.F;
    public float fireRange = 30f;
    public LayerMask losMask;
    Player player;
    void Start(){
        player = FindObjectOfType<Player>();
    }

    void Update()
    {
        if(player == null){
            player = FindObjectOfType<Player>();
        }
        if(player == null) return;
        // Only let the *local* Instructor initiate fire
        if (!IsLocalPlayerInstructor())
        {
            // Debug.LogWarning($" only instructor bozo, ur a {player.getPlayerRole()}");
            return;
        }

        if (Input.GetKeyDown(fireKey))
        {
            // Debug.Log("firekey pressed");
            CmdFireCommand();
        }
    }

    // Called on server when the local Instructor presses the fire key
    [Command(requiresAuthority = false)]
    void CmdFireCommand()
    {
        // Debug.Log("cmdfirecmd");
        RpcDoFireOnAllClients();
    }

    // Broadcast to all clients to trigger fire animations
    [ClientRpc]
    void RpcDoFireOnAllClients()
    {
        // Debug.Log("rpc called");
        FireAllLLEUnits();
    }


    void FireAllLLEUnits()
    {
        // only cycle through selected units
        List<GameObject> SelectedChars = player.GetSelectedChars();

        foreach (GameObject unit in SelectedChars)
        {
            AIMover mover = unit.GetComponent<AIMover>();
            // Skip if this NPC isn’t armed or isn’t Law Enforcement
            if (!mover.IsArmedUnit || !unit.CompareTag("LawEnforcement"))
            {
                // Debug.Log($"Armed: {mover.IsArmedUnit}, LLE: {unit.CompareTag("LawEnforcement")}");
                continue;
            }

            GameObject visibleHostile = GetVisibleHostile(unit.transform);
            if (visibleHostile != null)
            {
                // Debug.Log("hostile found");
                Animator unitAnimator = unit.GetComponent<Animator>();
                Animator hostileAnimator = visibleHostile.GetComponent<Animator>();

                if (unitAnimator != null)
                {
                    mover.StopAllMovement();
                    unitAnimator.SetTrigger("FireWeapon");
                    unitAnimator.SetBool("IsHoldingWeapon", true);
                    // unitAnimator.SetBool("IsAiming", true);
                }
                if (hostileAnimator != null)
                {
                    hostileAnimator.SetTrigger("Kill");
                    hostileAnimator.SetBool("IsDead", true);
                    // decides randomly between headshot or normal
                    int rand = Random.Range(0,2);
                    hostileAnimator.SetInteger("KillVariable", rand); 
                    // Debug.Log($"Killvar: {rand}");
                }
                StartCoroutine(ResetFireTrigger(unitAnimator));
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
            Animator animator = hostile.GetComponent<Animator>();

            if(animator == null) continue;
            if(animator.GetBool("IsDead")) continue; // dont kill if already dead

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
        // Wait until the FireWeapon animation begins
        yield return new WaitUntil(() => 
            animator.GetCurrentAnimatorStateInfo(0).IsName("Fire Weapon"));

        // Now wait until it finishes (normalizedTime >= 1)
        while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
        {
            yield return null;
        }

        animator.ResetTrigger("FireWeapon"); // Optional (triggers auto-reset)
        yield break;
    }

    // Confirms whether the local player is the Instructor
    bool IsLocalPlayerInstructor()
    {
        if(player == null) player = FindObjectOfType<Player>();
        return player.getPlayerRole() == Player.Roles.Instructor;
    }
}
