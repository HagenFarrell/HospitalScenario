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

    [Command(requiresAuthority = false)]
    void CmdFireCommand()
    {
        // First, handle any selected units (for backward compatibility)
        List<GameObject> SelectedChars = player.GetSelectedChars();
        
        // Even if no units are selected, tell all clients to check their units
        RpcTriggerAllLLEUnits(SelectedChars);
    }

    // Add this new RPC method
    [ClientRpc]
    void RpcTriggerAllLLEUnits(List<GameObject> SelectedChars)
    {
        // Each client runs this to check all their LLE units
        FireAllLLEUnits(SelectedChars);
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
                    Debug.DrawRay(unit.position + Vector3.up * 1.5f, direction * distance, Color.yellow, 1f);

                    Quaternion baseRotation = Quaternion.LookRotation(direction);
                    Quaternion offsetRotation = Quaternion.Euler(0, 45, 0); // fire animation turns them ~45 degrees. offset it

                    unit.rotation = baseRotation * offsetRotation;
                    return hostile;
                }
            }
        }

        return null;
    }

    IEnumerator ResetFireTrigger(Animator animator, GameObject unit)
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

        // Quaternion currentRotation = unit.transform.rotation;
        // Quaternion offsetRotation = Quaternion.Euler(0, -45, 0);

        // unit.transform.rotation = currentRotation * offsetRotation;
        yield break;
    }

    // Confirms whether the local player is the Instructor
    bool IsLocalPlayerInstructor()
    {
        if(player == null) player = FindObjectOfType<Player>();
        return player.getPlayerRole() == Player.Roles.Instructor;
    }

    void FireAllLLEUnits(List<GameObject> lleUnits)
    {
        // Get all LLE units
        // List<GameObject> lleUnits = player.GetSelectedChars();
        
        foreach (GameObject unit in lleUnits)
        {
            if (unit == null) continue;
            if(!unit.CompareTag("LawEnforcement")) continue;
            
            AIMover mover = unit.GetComponent<AIMover>();
            // Skip if this NPC isn't armed or mover is null
            if (mover == null || !mover.IsArmedUnit)
            {
                continue;
            }
            // sometimes they fire twice, use this to debug
            // Debug.Log($"{unit} is firing because he was selected");

            GameObject visibleHostile = GetVisibleHostile(unit.transform);
            if (visibleHostile != null)
            {
                Animator unitAnimator = unit.GetComponent<Animator>();
                if (unitAnimator == null) continue;
                
                Animator hostileAnimator = visibleHostile.GetComponent<Animator>();
                if (hostileAnimator == null) continue;

                mover.StopAllMovement();
                unitAnimator.SetBool("IsHoldingWeapon", true);
                unitAnimator.SetTrigger("FireWeapon");

                hostileAnimator.SetTrigger("Kill");
                hostileAnimator.SetBool("IsDead", true);
                // decide between headshot or normal (0 or 1)
                int rand = Random.Range(0,2);
                hostileAnimator.SetInteger("KillVariable", rand);
                
                StartCoroutine(ResetFireTrigger(unitAnimator, unit));
            }
        }
    }
}
