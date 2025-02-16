using UnityEngine;
using PhaseLink;
using System.Collections;
using System.Collections.Generic;

public class MovementManager : MonoBehaviour
{
    private static MovementManager _instance;
    public static MovementManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<MovementManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("MovementManager");
                    _instance = go.AddComponent<MovementManager>();
                }
            }
            return _instance;
        }
    }

    private Dictionary<GameObject, Coroutine> npcCoroutines = new Dictionary<GameObject, Coroutine>();
    private HashSet<GameObject> playerControlledUnits = new HashSet<GameObject>();
    
    public void RegisterPlayerUnit(GameObject unit)
    {
        playerControlledUnits.Add(unit);
        // If this unit had an NPC coroutine, stop it
        if (npcCoroutines.TryGetValue(unit, out Coroutine coroutine))
        {
            if (coroutine != null)
                StopCoroutine(coroutine);
            npcCoroutines.Remove(unit);
        }
    }

    public void UnregisterPlayerUnit(GameObject unit)
    {
        playerControlledUnits.Remove(unit);
    }

    public bool IsPlayerControlled(GameObject unit)
    {
        return playerControlledUnits.Contains(unit);
    }

    public void StartNPCMovement(GameObject npc, GamePhase currentPhase)
    {
        if (!IsPlayerControlled(npc) && !npcCoroutines.ContainsKey(npc))
        {
            var coroutine = StartCoroutine(HandleIndependentNPCMovement(npc, currentPhase));
            npcCoroutines[npc] = coroutine;
        }
    }

    public void StopNPCMovement(GameObject npc)
    {
        if (npcCoroutines.TryGetValue(npc, out Coroutine coroutine))
        {
            if (coroutine != null)
                StopCoroutine(coroutine);
            npcCoroutines.Remove(npc);
        }
    }

    private IEnumerator HandleIndependentNPCMovement(GameObject npc, GamePhase currentPhase)
    {
        npcMovement moveController = FindObjectOfType<npcMovement>();
        
        while (currentPhase == GamePhase.Phase1 && !IsPlayerControlled(npc))
        {
            // if (moveController.IsNPCMoving(npc))
            // {
            //     yield return new WaitForSeconds(0.1f);
            //     continue;
            // }

            Vector3 randomDirection = new Vector3(
                UnityEngine.Random.Range(-5f, 5f), 
                0, 
                UnityEngine.Random.Range(-5f, 5f)
            );
            
            Vector3 targetPosition = npc.transform.position + randomDirection;

            if (Physics.Raycast(targetPosition + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f))
            {
                targetPosition = hit.point;
                moveController.MoveTo(targetPosition, npc);
            }

            yield return new WaitForSeconds(UnityEngine.Random.Range(2f, 5f));
        }

        npcCoroutines.Remove(npc);
    }
}