using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

// Script to handle returning all players to the overworld scene so that they may start a different minigame or the same one
public class SpringEndGameReturn : MonoBehaviour
{
    [SerializeField] string _overworldSceneName = "OverworldJ";
    [SerializeField] GameObject _overworldPlayerPrefab; // Now includes this field to ensure the spawning of normal overworld players and not this scene's player prefab

    bool _returnStarted;

    public void ReturnToOverworld()
    {
        if (_returnStarted)
            return;

        _returnStarted = true;
        Time.timeScale = 1f;

        NetworkManager networkManager = NetworkManager.Singleton;

        if (networkManager != null && networkManager.IsServer)
        {
            if (_overworldPlayerPrefab == null)
            {
                Debug.LogError("Assign the regular Overworld Player Prefab.", this);
            }
            else
            {
                networkManager.NetworkConfig.PlayerPrefab = _overworldPlayerPrefab;
            }
        }

        if (PlayerRoster.Local != null && PlayerRoster.Local.IsSpawned)
        {
            PlayerRoster.ReturnToOverworld();
            return;
        }

        if (networkManager != null && networkManager.IsListening)
        {
            if (!networkManager.IsServer)
                return;

            SceneEventProgressStatus status =
                networkManager.SceneManager.LoadScene(_overworldSceneName, LoadSceneMode.Single);

            if (status != SceneEventProgressStatus.Started)
                Debug.LogError($"Could not start the return to {_overworldSceneName}: {status}", this);

            return;
        }

        SceneManager.LoadScene(_overworldSceneName, LoadSceneMode.Single);
    }
}