using System;
using UnityEngine;

public class FinishLineTrigger : MonoBehaviour
{
    [SerializeField] RacingGameManager gameManager;
    
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Plane"))
        {
            gameManager.OnFinishLineReached();
        }
    }
}
