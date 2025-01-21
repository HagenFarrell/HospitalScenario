using UnityEngine;
using System.Collections.Generic;


public class RoleManager : MonoBehaviour
{
    public List<PlayerController> players; // List of players (assigned in Inspector)
    private int currentPlayerIndex = 0; // Index of the current player
    private Dictionary<PlayerController, Stack<Vector3>> actionHistory = new Dictionary<PlayerController, Stack<Vector3>>();

    public void ResetRoleTurns()
    {
        currentPlayerIndex = 0;

        // Clear action history for all players
        foreach (var player in players)
        {
            if (!actionHistory.ContainsKey(player))
            {
                actionHistory[player] = new Stack<Vector3>();
            }
            else
            {
                actionHistory[player].Clear();
            }
        }
    }

    public void StartRoleTurn()
    {
        if (currentPlayerIndex < players.Count)
        {
            players[currentPlayerIndex].EnableControl(true);
            Debug.Log($"Turn started for: {players[currentPlayerIndex].PlayerRole}");
        }
        else
        {
            Debug.Log("All player turns completed for this phase.");
        }
    }

    public void EndRoleTurn(PlayerController player)
    {
        if (player == players[currentPlayerIndex])
        {
            // Store the player's final position in action history
            actionHistory[player].Push(player.transform.position);

            // End the player's turn
            player.EnableControl(false);

            // Move to the next player
            currentPlayerIndex++;
            StartRoleTurn();
        }
    }

    public void UndoAllActionsForPhase()
    {
        foreach (var player in players)
        {
            if (actionHistory[player].Count > 0)
            {
                player.transform.position = actionHistory[player].Pop();
            }
        }
    }

    public enum Roles
    {
        LawEnforcement,
        FireDepartment,
        // OnSiteSecurity,  // no control
        // RadiationSafety, // no control
        // Dispatch,        // no control, can only mute alarm
        // Spectator,       // no control
        Instructor,
    }

}
