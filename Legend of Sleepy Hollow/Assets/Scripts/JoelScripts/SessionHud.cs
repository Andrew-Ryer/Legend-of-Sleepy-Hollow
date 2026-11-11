using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SessionHud : MonoBehaviour
{
    [SerializeField] SessionManager _sessionManager;
    [SerializeField] Button _hostButton;
    [SerializeField] Button _clientButton;
    [SerializeField] Button _disconnectButton;
    [SerializeField] TMP_Text _statusText;

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
        SessionManager.SessionChanged += HandleSessionChanged;
        ApplyState(_sessionManager.State);
    }

    void OnDisable()
    {
        _hostButton.onClick.RemoveListener(HandleHostClicked);
        _clientButton.onClick.RemoveListener(HandleClientClicked);
        _disconnectButton.onClick.RemoveListener(HandleDisconnectClicked);
        SessionManager.SessionChanged -= HandleSessionChanged;
    }

    void HandleHostClicked() => Request(SessionAction.Host);
    void HandleClientClicked() => Request(SessionAction.Client);
    void HandleDisconnectClicked() => Request(SessionAction.Disconnect);

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
    }
}
