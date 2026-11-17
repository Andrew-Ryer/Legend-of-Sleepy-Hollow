using System;
using Unity.Netcode;
using UnityEngine;

public class RacingGameManager : NetworkBehaviour
{
    [SerializeField] Timer timer;
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
    }

    private void Update()
    {
        float currTime = timer.UpdateTimer();
        
        if (currTime <= 0)
        {
            Debug.Log("Game Starts!");
        }
        else
        {
             Debug.Log("Current Time: " + (int) currTime);
        }
    }

    public void OnFinishLineReached()
    {
        if (!IsServer) return;
        
        Debug.Log("A plane crosses finish line!");
    }
}
