using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SessionHud : MonoBehaviour
{
    [SerializeField] SessionManager _sessionManager;
    [SerializeField] Button _hostButton;
    [SerializeField] Button _clientButton;
    [SerializeField] Button _disconnectButton;
    
    [SerializeField] Button _returnButton;
    
    [SerializeField] TMP_Text _statusText;
    
    [SerializeField] string _overworldSceneName = "OverworldJ";

    public enum SessionAction { Host, Client, Disconnect }

    public static EventHandler<SessionRequestedArgs> SessionRequested;

    public class SessionRequestedArgs : EventArgs
    {
        public SessionAction action;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetEvents() => SessionRequested = null;

    void OnEnable()
    {
        _hostButton.onClick.AddListener(HandleHostClicked);
        _clientButton.onClick.AddListener(HandleClientClicked);
        _disconnectButton.onClick.AddListener(HandleDisconnectClicked);
        
        _returnButton.onClick.AddListener(HandleReturnClicked);
        
        SessionManager.SessionChanged += HandleSessionChanged;
        // The session root survives a trip to a minigame and back, so this
        // scene's copy of the manager may already be gone.
        ApplyState((SessionManager.Local != null ? SessionManager.Local : _sessionManager).State);
    }

    void OnDisable()
    {
        _hostButton.onClick.RemoveListener(HandleHostClicked);
        _clientButton.onClick.RemoveListener(HandleClientClicked);
        _disconnectButton.onClick.RemoveListener(HandleDisconnectClicked);
        
        _returnButton.onClick.RemoveListener(HandleReturnClicked);
        
        SessionManager.SessionChanged -= HandleSessionChanged;
    }

    void HandleHostClicked() => Request(SessionAction.Host);
    void HandleClientClicked() => Request(SessionAction.Client);
    void HandleDisconnectClicked() => Request(SessionAction.Disconnect);

    void HandleReturnClicked()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("Cannot return to the Overworld because NetworkManager.Singleton is null.");
            return;
        }

        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("Only the host/server can return everyone to the Overworld.");
            return;
        }

        if (SceneManager.GetActiveScene().name == _overworldSceneName)
            return;

        _returnButton.interactable = false;

        NetworkManager.Singleton.SceneManager.LoadScene(
            _overworldSceneName,
            LoadSceneMode.Single
        );
    }

    void Request(SessionAction action)
    {
        SessionRequested?.Invoke(this, new SessionRequestedArgs { action = action });
    }

    void HandleSessionChanged(object sender, SessionManager.SessionChangedArgs e) => ApplyState(e);

    void ApplyState(SessionManager.SessionChangedArgs state)
    {
        _statusText.text = state.status;
        bool idle = !state.connected && !state.connecting;
        _hostButton.gameObject.SetActive(!state.connected);
        _clientButton.gameObject.SetActive(!state.connected);
        _hostButton.interactable = idle;
        _clientButton.interactable = idle;
        _disconnectButton.gameObject.SetActive(state.connected || state.connecting);
        
        bool canReturn =
            state.connected &&
            NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsServer &&
            SceneManager.GetActiveScene().name != _overworldSceneName;

        _returnButton.gameObject.SetActive(canReturn);
        _returnButton.interactable = canReturn;

    }
}
