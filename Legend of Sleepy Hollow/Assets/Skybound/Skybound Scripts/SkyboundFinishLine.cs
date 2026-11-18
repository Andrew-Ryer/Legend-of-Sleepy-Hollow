using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class SkyboundFinishLine : NetworkBehaviour
{
    [SerializeField] private SkyboundTimer timer;
    private List<ulong> finishedPlayerIds = new List<ulong>();
    private List<ulong> dnfPlayerIds = new List<ulong>();
    private HashSet<ulong> processedPlayers = new HashSet<ulong>();

    public void StartRaceServer()
    {
        if (!IsServer) return;
        finishedPlayerIds.Clear();
        dnfPlayerIds.Clear();
        processedPlayers.Clear();

        Debug.Log("Server: Race has officially started!");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        SkyboundPlayerController player = other.GetComponent<SkyboundPlayerController>();

        if (player != null)
        {
            ulong clientId = player.OwnerClientId;

            if (!processedPlayers.Contains(clientId))
            {
                processedPlayers.Add(clientId);
                finishedPlayerIds.Add(clientId);

                Debug.Log($"Server: Player {clientId} crossed the finish line in position {finishedPlayerIds.Count}!");

                if (finishedPlayerIds.Count == 1)
                {
                    timer.firstFinish();
                }

                // CheckAllPlayersFinishedServer();
            }
        }
    }

    // private void CheckAllPlayersFinishedServer()
    // {
    //     int totalConnectedPlayers = NetworkManager.Singleton.ConnectedClientsIds.Count;

    //     if (finishedPlayerIds.Count >= totalConnectedPlayers)
    //     {
    //         Debug.Log("Server: All players have crossed the finish line!");
    //         EndGameServer();
    //     }
    // }

    // private void TriggerTimeOutServer()
    // {
    //     if (raceEnded) return;

    //     Debug.Log("Server: Race timer expired! Collecting remaining players into DNF array.");

    //     // Find all connected clients who haven't finished yet and add them to DNF
    //     foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
    //     {
    //         if (!processedPlayers.Contains(clientId))
    //         {
    //             processedPlayers.Add(clientId);
    //             dnfPlayerIds.Add(clientId);
    //             Debug.Log($"Server: Player {clientId} ran out of time (DNF).");
    //         }
    //     }

    //     EndGameServer();
    // }

    // private void EndGameServer()
    // {
    //     if (raceEnded) return;
    //     raceEnded = true;

    //     // Distribute points based on finish order
    //     DistributePoints();

    //     // Call method for game end
    //     CallGameEndMethod();
    // }


    // private void CallGameEndMethod()
    // {
    //     Debug.Log("Server: Triggering end of game sequence across clients.");

    //     // Inform clients via ClientRpc that the match is over
    //     TriggerGameEndClientRpc(finishedPlayerIds.ToArray(), dnfPlayerIds.ToArray());
    // }

    // [ClientRpc]
    // private void TriggerGameEndClientRpc(ulong[] finalStandings, ulong[] dnfStandings)
    // {
    //     // Executed on all clients (and host). Use this to display UI leaderboards, play sounds, etc.
    //     Debug.Log("Client: Match has ended. Displaying final scoreboard UI.");
        
    //     // Example: UIController.Instance.ShowScoreboard(finalStandings, dnfStandings);
    // }
}