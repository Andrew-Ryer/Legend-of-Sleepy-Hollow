using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ReturnToOverworld : MonoBehaviour
{
    [SerializeField] private string overworldSceneName = "Overworld";
    
    /// Call this when the minigame is finished or from a UI button.
    /// Only the host/server is allowed to initiate the network scene change.
    public void ReturnToOverworldScene()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError(
                "Cannot return to Overworld because NetworkManager.Singleton is null."
            );
            return;
        }

        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning(
                "Only the host/server can return everyone to the Overworld."
            );
            return;
        }

        NetworkManager.Singleton.SceneManager.LoadScene(
            overworldSceneName,
            LoadSceneMode.Single
        );
    }
}