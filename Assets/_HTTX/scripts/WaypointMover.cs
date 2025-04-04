using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public class WaypointMover : MonoBehaviour
{
    // Stores a referece to the waypoint system this object will use
    [SerializeField] public Waypoints waypoints;

    [Range(1f, 10f)]
    [SerializeField] public float moveSpeed = 2f;

    [SerializeField] private float distanceThreshold = 0.1f;
    public Stack<WaypointState> waypointStorage;
    public GameObject paths;
    public int pathidx;


    [Range(1f, 20f)]
    [SerializeField] private float rotateSpeed = 10f;

    // The current waypoint target that the object is moving towards
    public Transform currentWaypoint;

    // The roation target for the current frame
    private Quaternion rotationGoal;
    // The direction to the next waypoint that the NPC needs to rotate towards
    private Vector3 directionToWaypoint;
    // Check if civilian/medical reach final waypoint so they can be despawned
    public bool despawnAtLastWaypoint = false;
    private bool isWaitingForAnimation = false;
    private Animator animator;

    // Start is called before the first frame update
    void Start()
    {
        waypointStorage = new Stack<WaypointState>();
        animator = GetComponent<Animator>();

        if (waypoints == null)
        {
            Debug.LogError($"WaypointMover on {gameObject.name} has no Waypoints assigned!");
            enabled = false;
            return;
        }

        currentWaypoint = waypoints.GetNextWaypoint(null);
        if (currentWaypoint == null)
        {
            Debug.LogError($"WaypointMover on {gameObject.name}: first waypoint returned null!");
            enabled = false;
            return;
        }

        transform.position = currentWaypoint.position;

        // Set next target
        currentWaypoint = waypoints.GetNextWaypoint(currentWaypoint);
        if (currentWaypoint == null)
        {
            Debug.LogWarning($"WaypointMover on {gameObject.name}: next waypoint is null (might be at the end of path).");
            enabled = false;
            return;
        }

        transform.LookAt(currentWaypoint);

        paths = waypoints.transform.parent?.gameObject;
        pathidx = 0;
    }


    // Update is called once per frame
    void Update()
    {
        //Debug

        // Debug.Log("In WaypointMover update"); //a debug log every frame is lethal
        if (PhaseManager.Instance == null || !PhaseManager.Instance.gameObject.activeInHierarchy)
        {
            return; // Wait for Mirror to finish setup
        }

        if (!PhaseManager.Instance.gameObject.activeInHierarchy)
        {
            Debug.LogWarning($"PhaseHandling GameObject is inactive! Active: {PhaseManager.Instance.gameObject.activeSelf}, " + $"In Hierarchy: {PhaseManager.Instance.gameObject.activeInHierarchy}");
            return;
        }
        //Debug.Log($"PhaseManager.Instance = {PhaseManager.Instance}");
        //gameObject.SetActive(true);
        //Debug.Log("")
        if (isWaitingForAnimation) return;

        transform.position = Vector3.MoveTowards(transform.position, currentWaypoint.position, moveSpeed * Time.deltaTime);
        if (Vector3.Distance(transform.position, currentWaypoint.position) < distanceThreshold)
        {
            // Get current phase
            PhaseManager phaseManager = FindObjectOfType<PhaseManager>();

            // Speed Change for Walking/Running
            if (phaseManager != null) {
                if (phaseManager.GetCurrentPhase() == GamePhase.Phase1) {
                    moveSpeed = 2f;
                } else if (phaseManager.GetCurrentPhase() == GamePhase.Phase2) {
                    moveSpeed = 5f;
                }
            }

            if (phaseManager.GetCurrentPhase() == GamePhase.Phase2)
            {
                // Ensure NPCs run in Phase 2
                if (animator != null)
                {
                    if (animator.GetBool("ToSitting"))
                    {
                        // NPC is currently sitting, wait for it to stand up before moving
                        StartCoroutine(WaitForStandingAnimation());
                        return;
                    }
                    else
                    {
                        // Non villians run
                        if (!(gameObject.CompareTag("OutsideVillains") || gameObject.CompareTag("Villains")))
                        {
                            animator.SetBool("IsWalking", false);
                            animator.SetBool("IsRunning", true);
                        }
                    }
                }
            }
            
            // For Phase 1, check against the active waypoints limit
            int lastActiveIndex;
            if (phaseManager != null && phaseManager.GetCurrentPhase() == GamePhase.Phase1) {
                // In Phase 1 with no branching, the last active waypoint is one less than the count
                lastActiveIndex = waypoints.ActiveChildLength - 1;
            } else {
                // Otherwise, the last active waypoint is the last child
                lastActiveIndex = currentWaypoint.parent.childCount - 1;
            }

            // Check if this is the last waypoint
            bool isLastWaypoint = currentWaypoint.GetSiblingIndex() == lastActiveIndex;

            // Check if this is the first waypoint
            bool isFirstWaypoint = currentWaypoint.GetSiblingIndex() == 0;

            if (isLastWaypoint && !waypoints.canLoop && waypoints.ActiveChildLength > 1 &&
                phaseManager != null && phaseManager.GetCurrentPhase() == GamePhase.Phase1) {
                // Reverse direction
                waypoints.isMovingForward = false;
                // Debug.Log($"Phase 1: NPC {gameObject.name} reversing path");
                // Get the next waypoint after changing direction
                currentWaypoint = waypoints.GetNextWaypoint(currentWaypoint);
                return;
            }
            else if (isFirstWaypoint && !waypoints.isMovingForward && !waypoints.canLoop && waypoints.ActiveChildLength > 1 &&
                phaseManager != null && phaseManager.GetCurrentPhase() == GamePhase.Phase1) {
                // Change direction to forward again
                waypoints.isMovingForward = true;
                // Debug.Log($"Phase 1: NPC {gameObject.name} going forward again");
                // Get the next waypoint after changing direction
                currentWaypoint = waypoints.GetNextWaypoint(currentWaypoint);
                return;
            }


            // If it's the last waypoint, we should despawn, we're not looping, and we're in Phase 2
            if (isLastWaypoint && despawnAtLastWaypoint && !waypoints.canLoop && 
                phaseManager != null && phaseManager.GetCurrentPhase() == GamePhase.Phase2) {
                // Despawn the NPC
                // Debug.Log($"Phase 2: NPC {gameObject.name} reached final waypoint and is despawning");
                if(!(gameObject.CompareTag("Hostages") || gameObject.CompareTag("PhysicianHostage"))) {
                    // Debug.Log("not a hostage, despawning");
                    gameObject.SetActive(false);
                } else {
                    animator.SetBool("IsRunning", false);
                    animator.SetBool("IsThreatPresent", true);
                }
                
                return;
            } else if(!despawnAtLastWaypoint){
                gameObject.SetActive(true);
            }

            // If its last waypoint and in phase 4
            if (isLastWaypoint && !waypoints.canLoop && 
                phaseManager != null && phaseManager.GetCurrentPhase() == GamePhase.Phase4)
            {
                // Start rummaging to get radiation source
                if (gameObject.CompareTag("PhysicianHostage"))
                {
                    animator.SetBool("IsRunning", false);
                    animator.SetBool("ToRummaging", true);

                    // Spawn in radation source

                }
                // Other villains
                else 
                {
                    animator.SetBool("IsRunning", false);
                }
            } 
            else if (isLastWaypoint && !waypoints.canLoop && 
                phaseManager != null && (phaseManager.GetCurrentPhase() == GamePhase.Phase5 || phaseManager.GetCurrentPhase() == GamePhase.Phase6 || phaseManager.GetCurrentPhase() == GamePhase.Phase7))
                {
                    // Start rummaging to get radiation source
                    if (gameObject.CompareTag("PhysicianHostage"))
                    {
                        animator.SetBool("ToRummaging", false);
                        animator.SetBool("IsRunning", false);
                        // Spawn in radation source

                    }
                    // Other villains
                    else 
                    {
                        animator.SetBool("IsRunning", false);
                    }
                }
            // Otherwise, continue to the next waypoint
            currentWaypoint = waypoints.GetNextWaypoint(currentWaypoint);
        }
        if (waypoints.canLoop || PhaseManager.Instance.GetCurrentPhase() == GamePhase.Phase7)
        {
            RotateTowardsWaypoint();
        }
        else
        {
            // If path is not looping, only rotate if not at the last waypoint
            if (currentWaypoint.GetSiblingIndex() < waypoints.transform.childCount && waypoints.ActiveChildLength > 1)
            {
                // Not at the last waypoint, so continue rotating
                RotateTowardsWaypoint();
            }
            // If at the last waypoint, do nothing (don't rotate)
        }
    }

    IEnumerator WaitForStandingAnimation()
    {
        isWaitingForAnimation = true;
        animator.SetBool("ToSitting", false); // Transition to standing up
        yield return new WaitUntil(() => animator.GetCurrentAnimatorStateInfo(0).IsName("Idle_Standing") || animator.GetCurrentAnimatorStateInfo(0).IsName("Running"));
        isWaitingForAnimation = false;
    }

    // Will Slowly rotate the agent towards the current waypoint it is moving towards
    public void RotateTowardsWaypoint()
    {
        // Gets direction to waypoint
        directionToWaypoint = (currentWaypoint.position - transform.position).normalized;
        
        // Check to stop last waypoint rotation if Canloop = false
        if (directionToWaypoint != Vector3.zero)
        {
            rotationGoal = Quaternion.LookRotation(directionToWaypoint);

            // Slow rotation
            transform.rotation = Quaternion.Slerp(transform.rotation, rotationGoal, rotateSpeed * Time.deltaTime);
        }
    }
}
