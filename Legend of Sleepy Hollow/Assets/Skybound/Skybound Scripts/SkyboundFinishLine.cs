using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using System.Text;

public class SkyboundFinishLine : NetworkBehaviour
{
    [SerializeField] private SkyboundTimer timer;
    private List<ulong> finishedPlayerIds = new List<ulong>();
    private List<ulong> dnfPlayerIds = new List<ulong>();
    private HashSet<ulong> processedPlayers = new HashSet<ulong>();

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            SkyboundTimer.OnTimerEnded += EndGame;
            SkyboundGamemanager.OnGameStarted += StartRaceServer;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            SkyboundTimer.OnTimerEnded -= EndGame;
            SkyboundGamemanager.OnGameStarted -= StartRaceServer;
        }
    }


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
        Debug.Log("Entered");
        if (!IsServer || !IsSpawned) return;
        
        SkyboundPlayerController player = other.GetComponentInChildren<SkyboundPlayerController>();

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
                    SkyboundTimer timer = SkyboundTimer.Instance;
                    timer.firstFinish();
                }

            }
        }
    }

    private void EndGame(int reason)
    {
        if (!IsServer) return;
        dnfPlayerIds.Clear();
        if (reason == 1 && NetworkManager.Singleton != null)
        {
            foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                if (!processedPlayers.Contains(clientId))
                {
                    processedPlayers.Add(clientId);
                    dnfPlayerIds.Add(clientId);
                }
            }
        }

        ulong[] finishedArray = finishedPlayerIds.ToArray();
        ulong[] dnfArray = dnfPlayerIds.ToArray();

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Finish:");

        if (finishedArray.Length == 0)
        {
            sb.AppendLine("None");
        }
        else
        {
            for (int i = 0; i < finishedArray.Length; i++)
            {
                sb.AppendLine($"{i + 1}. Player {finishedArray[i]}");
            }
        }

        if (reason == 1)
        {
            sb.Append("DNF: ");
            if (dnfArray.Length == 0)
            {
                sb.Append("None");
            }
            else
            {
                sb.Append(string.Join(", ", dnfArray));
            }
        }

        Debug.Log(sb.ToString());
    }

}