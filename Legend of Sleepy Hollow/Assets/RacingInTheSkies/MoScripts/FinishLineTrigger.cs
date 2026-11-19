using System;
using Unity.Netcode;
using UnityEngine;

public class FinishLineTrigger : MonoBehaviour
{
    [SerializeField] RacingGameManager gameManager;
    [SerializeField] SessionManager sessionManager;
    
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Plane"))
        {
            Debug.Log("Player: " + other.gameObject.GetComponent<NetworkObject>().OwnerClientId + " crossed finish line.");
            gameManager.OnFinishLineReached();
        }
    }
}
