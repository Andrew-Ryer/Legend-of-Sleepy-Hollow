using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

// Script to handle returning all players to the overworld scene so that they may start a different minigame or the same one
public class SpringEndGameReturn : MonoBehaviour
{
    [SerializeField] string _overworldSceneName = "OverworldJ";
    [SerializeField] GameObject _overworldPlayerPrefab; // Now includes this field to ensure the spawning of normal overworld players and not this scene's player prefab
    [SerializeField] GameObject _gameSessionPrefab; // GameSession prefab from the overworld folder

    bool _returnStarted;

    public void ReturnToOverworld()
    {
        if (_returnStarted)
            return;

        _returnStarted = true;
        Time.timeScale = 1f;

        NetworkManager networkManager = NetworkManager.Singleton;

        if (networkManager == null || !networkManager.IsListening)
        {
            SceneManager.LoadScene(_overworldSceneName, LoadSceneMode.Single);
            return;
        }

        // A client can request the return through the shared roster
        if (!networkManager.IsServer)
        {
            if (PlayerRoster.Local != null && PlayerRoster.Local.IsSpawned)
                PlayerRoster.ReturnToOverworld();

            return;
        }

        // Use the regular overworld player for future connections
        if (_overworldPlayerPrefab != null)
            networkManager.NetworkConfig.PlayerPrefab = _overworldPlayerPrefab;
        else
            Debug.LogError("Assign the regular Overworld Player Prefab.", this);

        // Recreate the shared roster if this Pogo session has no live one
        if (PlayerRoster.Local == null || !PlayerRoster.Local.IsSpawned)
        {
            if (!TryCreateGameSession())
            {
                Debug.LogError(
                    "Could not return to Overworld: no live PlayerRoster and no valid Game Session prefab.",
                    this);
                return;
            }
        }

        PlayerRoster.ReturnToOverworld();
    }

    bool TryCreateGameSession()
    {
        if (_gameSessionPrefab == null)
            return false;

        GameObject session = Instantiate(_gameSessionPrefab);

        if (!session.TryGetComponent(out NetworkObject networkObject) ||
            !session.TryGetComponent(out PlayerRoster _))
        {
            Destroy(session);
            return false;
        }

        networkObject.Spawn(false);
        return PlayerRoster.Local != null && PlayerRoster.Local.IsSpawned;
    }
}