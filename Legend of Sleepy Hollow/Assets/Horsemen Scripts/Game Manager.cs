using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class HorsemenGameManager : NetworkBehaviour
{
    private List<ulong> vehicleIDs = new List<ulong>();
    [SerializeField]
    private Horsemen horsemen;

    List<ulong> playerID = new List<ulong>();

    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;

    int spawnIndex = 0;


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
        if (horsemen == null)
        {
            Debug.LogError("HorsemenGameManager: horsemen is not assigned.", this);
            return;
        }
        horsemen.OnKillevent += Horsemen_OnKillevent;
        
    }

    private void OnClientConnected(ulong clientID)
    {
        if(NetworkManager.Singleton.ConnectedClientsIds.Count >= 2)
        {
            print("Game start");
        }
        playerID.Add(clientID);
        SpawnPlayerForClient(clientID);

    }

    private void Horsemen_OnKillevent(ulong clientID)
    {
        Debug.Log("Player " + clientID);
        vehicleIDs.Add(clientID);
    }

    private void FinishedMiniGame()
    {
       
        foreach (ulong playerid in playerID) 
        {
            if (!vehicleIDs.Contains(playerid)) 
            { 
                vehicleIDs.Add(playerid);
            }
        }

       for(int i = 0; i < vehicleIDs.Count; i++ )
        {


            int place = i + 1;
            Debug.Log("Place: " + place + ": " + vehicleIDs[i]);
           

        }
        //Time.timeScale = 0;

    }


    private void SpawnPlayerForClient(ulong clientId)
    {
        if (!IsServer) { return; }
        Transform spawnPoint = spawnPoints[spawnIndex];
        spawnIndex++;

        GameObject playerInstance = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);

        NetworkObject netObj = playerInstance.GetComponent<NetworkObject>();
        netObj.SpawnAsPlayerObject(clientId, destroyWithScene: true);
    }


    // Update is called once per frame
    void Update()
    {
        if (!IsServer) { return; }
        

        if (vehicleIDs.Count == playerID.Count - 1) 
        {
            Debug.Log("Game finished");
            FinishedMiniGame();

        }

        
    }

}
