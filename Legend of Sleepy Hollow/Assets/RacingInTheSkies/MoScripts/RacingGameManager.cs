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
        
    }

    public void OnFinishLineReached()
    {
        if (!IsServer) return;
        
        Debug.Log("A plane crosses finish line!");
    }
}
