using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class HorsemenGameManager : NetworkBehaviour
{
    private Stack<ulong> vehicleIDs = new Stack<ulong>();
    [SerializeField]
    private Horsemen horsemen;

    List<ulong> playerID = new List<ulong>();
  
       

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
        playerID.Add(0);
        playerID.Add(1);

    }

    void Start()
    {
        if (horsemen == null)
        {
            Debug.LogError("HorsemenGameManager: horsemen is not assigned.", this);
            return;
        }
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
       
        foreach (ulong playerid in playerID) 
        {
            if (!vehicleIDs.Contains(playerid)) 
            { 
                vehicleIDs.Push(playerid);
            }
        }

       for(int i = 0; i < vehicleIDs.Count; i++ )
        {


            int place = i + 1;
            Debug.Log("Place: " + place + ": " + vehicleIDs.Peek());
            vehicleIDs.Pop();

        }
        Time.timeScale = 0;

    }

    // Update is called once per frame
    void Update()
    {
        if (!IsServer) { return; }
        Debug.Log(vehicleIDs.Count);

        if (vehicleIDs.Count == 1) 
        {
            FinishedMiniGame();
        }

        
    }

}
