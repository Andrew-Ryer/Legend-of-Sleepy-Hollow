using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MazeSessionHud : MonoBehaviour
{
    [SerializeField]
    MazeSessionManager _sessionManager;

    [SerializeField]
    Button _hostButton;

    [SerializeField]
    Button _clientButton;

    [SerializeField]
    Button _disconnectButton;

    [SerializeField]
    TMP_Text _statusText;

    [SerializeField]
    TMP_Text _winnerText;

    SessionStateMachine _sessionStateMachine;

    public enum SessionAction
    {
        Host,
        Client,
        Disconnect,
    }

    public static EventHandler<SessionRequestedArgs> SessionRequested;

    public class SessionRequestedArgs : EventArgs
    {
        public SessionAction action;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetEvents() => SessionRequested = null;

    void OnEnable()
    {
        _sessionStateMachine = FindFirstObjectByType<SessionStateMachine>();
        _hostButton.onClick.AddListener(HandleHostClicked);
        _clientButton.onClick.AddListener(HandleClientClicked);
        _disconnectButton.onClick.AddListener(HandleDisconnectClicked);
        MazeSessionManager.SessionChanged += HandleSessionChanged;
        ApplyState(_sessionManager.State);
    }

    void OnDisable()
    {
        _hostButton.onClick.RemoveListener(HandleHostClicked);
        _clientButton.onClick.RemoveListener(HandleClientClicked);
        _disconnectButton.onClick.RemoveListener(HandleDisconnectClicked);
        MazeSessionManager.SessionChanged -= HandleSessionChanged;
    }

    void Update()
    {
        if (_sessionStateMachine == null)
            _sessionStateMachine = FindFirstObjectByType<SessionStateMachine>();

        if (_sessionStateMachine == null)
            return;

        bool isGameOver = _sessionStateMachine.CurrentPhase == SessionPhase.GameOver;
        if (_winnerText != null)
        {
            _winnerText.gameObject.SetActive(isGameOver);
            _winnerText.enabled = isGameOver;
        }

        if (!isGameOver || _winnerText == null)
            return;

        ulong winnerClientId = _sessionStateMachine.WinnerClientId;
        if (winnerClientId != ulong.MaxValue)
            _winnerText.text = $"Player {winnerClientId + 1} Wins!";
    }

    void HandleHostClicked() => Request(SessionAction.Host);

    void HandleClientClicked() => Request(SessionAction.Client);

    void HandleDisconnectClicked() => Request(SessionAction.Disconnect);

    void Request(SessionAction action)
    {
        SessionRequested?.Invoke(this, new SessionRequestedArgs { action = action });
    }

    void HandleSessionChanged(object sender, MazeSessionManager.SessionChangedArgs e) =>
        ApplyState(e);

    void ApplyState(MazeSessionManager.SessionChangedArgs state)
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
