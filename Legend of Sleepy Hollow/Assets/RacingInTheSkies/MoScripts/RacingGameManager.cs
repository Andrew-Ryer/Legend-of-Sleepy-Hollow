using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class RacingGameManager : NetworkBehaviour
{
    [SerializeField] TextMeshProUGUI statusText;
    [SerializeField] CountdownToRace countdownToRace;
    // [SerializeField] PlayerController plane;
    // [SerializeField] private Rigidbody rb;
    
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
        float currTime = countdownToRace.UpdateTimer();
        
        if (currTime <= 0)
        {
            statusText.text = $"Game Starts!";
            // Debug.Log("Game Starts!");
            // plane moves forward by default in the x direction
            // rb = GetComponent<NetworkRigidbody>();
            // rb.linearVelocity = speed;
        }
        else
        {
            // Debug.Log("Current Time: " + (int) currTime);
            statusText.text = $"Time Left to Start: {((int)currTime).ToString()}";
        }
    }

    public void OnFinishLineReached()
    {
        if (!IsServer) return;
        
        Debug.Log("A plane crosses finish line!");
    }
}
