using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

// Script to handle returning all players to the overworld scene so that they may start a different minigame or the same one
public class SpringEndGameReturn : MonoBehaviour
{
    [SerializeField] string _overworldSceneName = "OverworldJ";

    bool _returnStarted;

    public void ReturnToOverworld()
    {
        if (_returnStarted)
            return;

        _returnStarted = true;
        Time.timeScale = 1f;

        if (PlayerRoster.Local != null && PlayerRoster.Local.IsSpawned)
        {
            PlayerRoster.ReturnToOverworld();
            return;
        }

        NetworkManager networkManager = NetworkManager.Singleton;

        if (networkManager != null && networkManager.IsListening)
        {
            if (!networkManager.IsServer)
            {
                Debug.LogError(
                    "Cannot return all players: PlayerRoster is unavailable and this client is not the server.",
                    this);
                return;
            }

            SceneEventProgressStatus status =
                networkManager.SceneManager.LoadScene(_overworldSceneName, LoadSceneMode.Single);

            if (status != SceneEventProgressStatus.Started)
                Debug.LogError($"Could not start the return to {_overworldSceneName}: {status}", this);

            return;
        }

        SceneManager.LoadScene(_overworldSceneName, LoadSceneMode.Single);
    }
}