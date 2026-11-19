using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class HorsemenGameManager : NetworkBehaviour
{
    private Stack<ulong> vehicleIDs = new Stack<ulong>();
    [SerializeField]
    private Horsemen horsemen;


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsServer) {  return; }
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private enum Gamestate
    {
        Starting,
        Started,
        Ended

    }

    private void Awake()
    {
        
    }

    void Start()
    {
        horsemen.OnKillevent += Horsemen_OnKillevent;
        
    }

    private void OnClientConnected(ulong clientID)
    {
        if(NetworkManager.Singleton.ConnectedClientsIds.Count < 2)
        {
            print("Game start");
        }
        
    }

    private void Horsemen_OnKillevent(ulong clientID)
    {
        Debug.Log("Player " + clientID);
        vehicleIDs.Push(clientID);
    }

    private void FinishedMiniGame()
    {
        IReadOnlyList<ulong> clientIds = NetworkManager.Singleton.ConnectedClientsIds;
        foreach (ulong clientID in clientIds) 
        {
            if (!vehicleIDs.Contains(clientID))
            {
                vehicleIDs.Push((ulong)clientID);
            }
        
        }

        while (vehicleIDs.Count > 0)
        {
            int i = 1;
            Debug.Log(i + ": " + vehicleIDs.Peek());
            vehicleIDs.Pop();

        }
        Time.timeScale = 0;

    }

    // Update is called once per frame
    void Update()
    {
        if (!IsServer) { return; }

        if (vehicleIDs.Count == 1) 
        {
            FinishedMiniGame();
        }

        
    }

}
