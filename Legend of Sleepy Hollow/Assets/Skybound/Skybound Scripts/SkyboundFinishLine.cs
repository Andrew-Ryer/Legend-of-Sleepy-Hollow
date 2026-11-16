using UnityEngine;
using Unity.Netcode;

public class SkyboundFinishLine : NetworkBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        SkyboundPlayerController player = other.GetComponent<SkyboundPlayerController>();
    
        if (player != null)
        {
            if (NetworkManager.Singleton != null)
            {
                if (NetworkManager.Singleton.IsServer)
                {
                    Debug.Log($"Server: Player {player.OwnerClientId} crossed the finish line!");
                    // TriggerWinState(player);
                }
            }
            else
            {
                // Fallback for offline/single-player testing
                Debug.Log("Player crossed the finish line!");
                // TriggerWinState(player);
            }
        }
    }
}
