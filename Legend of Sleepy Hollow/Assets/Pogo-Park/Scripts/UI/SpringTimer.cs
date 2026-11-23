using TMPro;
using System;
using Unity.Netcode;
using UnityEngine;

// Updated to now end the game by setting deltaTime to 0 if that feature is enabled
// As well as featuring an optional countdown that only affects the players in PogoPark
// Updated both timers to only start when a host joins the game
[RequireComponent(typeof(NetworkObject))]
public class SpringTimer : NetworkBehaviour
{
    [SerializeField, Min(0f)] float timeLeft = 60f;
    [SerializeField] bool endGameWhenTimeRunsOut;
    [SerializeField] bool countdownTimer;
    [SerializeField, Min(0f)] float countdownTime = 3f;
    [SerializeField] TMP_Text timerText;

    const byte Waiting = 0;
    const byte Countdown = 1;
    const byte Running = 2;
    const byte Finished = 3;
    const byte Stopped = 4;

    readonly NetworkVariable<float> _remainingTime = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    readonly NetworkVariable<byte> _state = new NetworkVariable<byte>(Waiting,NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Server);

    float _serverRemainingTime;
    float _elapsedTime;
    int _lastLoggedSecond = -1;
    int _lastDisplayedSecond = -1;
    int _lastPublishedSecond = -1;

    public float TimeLeft => Mathf.Max(0f, _remainingTime.Value);
    public float ElapsedTime => _elapsedTime;
    public bool IsRunning => _state.Value == Countdown || _state.Value == Running;
    public bool HasExpired => _state.Value == Finished;
    public bool GameplayStarted => _state.Value == Running;
    public event Action TimerExpired;

    void Awake()
    {
        UpdateTimerText();
    }

    public override void OnNetworkSpawn()
    {
        _remainingTime.OnValueChanged += HandleRemainingTimeChanged;
        _state.OnValueChanged += HandleStateChanged;

        if (IsServer)
        {
            if (Time.timeScale == 0f)
                Time.timeScale = 1f;

            _elapsedTime = 0f;
            _lastLoggedSecond = -1;
            _lastPublishedSecond = -1;

            if (countdownTimer && countdownTime > 0f)
            {
                _serverRemainingTime = countdownTime;
                _state.Value = Countdown;
            }
            else
            {
                _serverRemainingTime = Mathf.Max(0f, timeLeft);
                _state.Value = Running;
            }

            _remainingTime.Value = _serverRemainingTime;
        }

        UpdateTimerText();
    }

    public override void OnNetworkDespawn()
    {
        _remainingTime.OnValueChanged -= HandleRemainingTimeChanged;
        _state.OnValueChanged -= HandleStateChanged;

        if (IsServer && Time.timeScale == 0f)
            Time.timeScale = 1f;
    }

    void Update()
    {
        if (!IsSpawned || !IsServer)
            return;

        if (_state.Value != Countdown && _state.Value != Running)
            return;

        float deltaTime = Time.deltaTime;
        _serverRemainingTime = Mathf.Max(0f, _serverRemainingTime - deltaTime);

        if (_state.Value == Running)
            _elapsedTime += deltaTime;

        int remainingSeconds = Mathf.CeilToInt(_serverRemainingTime);
        if (remainingSeconds != _lastLoggedSecond)
        {
            _lastLoggedSecond = remainingSeconds;
            Debug.Log(_state.Value == Countdown ? $"Game starts in: {remainingSeconds}" : $"Time remaining: {remainingSeconds}");
        }

        if (_serverRemainingTime <= 0f)
        {
            if (_state.Value == Countdown)
            {
                _elapsedTime = 0f;
                _serverRemainingTime = Mathf.Max(0f, timeLeft);
                _lastLoggedSecond = -1;
                _lastPublishedSecond = -1;
                _state.Value = Running;
                _remainingTime.Value = _serverRemainingTime;
            }
            else
            {
                _remainingTime.Value = 0f;
                _state.Value = Finished;
                TimerExpired?.Invoke();

                if (endGameWhenTimeRunsOut)
                {
                    Debug.Log("Game Over!");
                    Time.timeScale = 0f;
                }
            }

            return;
        }

        if (remainingSeconds != _lastPublishedSecond)
        {
            _lastPublishedSecond = remainingSeconds;
            _remainingTime.Value = _serverRemainingTime;
            UpdateTimerText();
        }
    }

    public void StopTimer()
    {
        if (!IsServer || !IsSpawned)
            return;

        if (_state.Value == Finished)
            return;

        _state.Value = Stopped;
        UpdateTimerText();
    }

    void HandleRemainingTimeChanged(float previousValue, float newValue)
    {
        UpdateTimerText();
    }

    void HandleStateChanged(byte previousValue, byte newValue)
    {
        _lastDisplayedSecond = -1;
        UpdateTimerText();

        if (newValue == Finished && !IsServer)
        {
            TimerExpired?.Invoke();
            if (endGameWhenTimeRunsOut)
                Time.timeScale = 0f;
        }
    }

    void UpdateTimerText()
    {
        if (timerText == null)
            return;

        if (_state.Value == Waiting)
        {
            timerText.text = "Waiting for host";
            return;
        }

        int displayedSeconds = Mathf.CeilToInt(Mathf.Max(0f, _remainingTime.Value));
        if (displayedSeconds == _lastDisplayedSecond)
            return;

        _lastDisplayedSecond = displayedSeconds;
        timerText.text = _state.Value == Countdown ? $"Starting in\n{displayedSeconds}" : $"Time\n{displayedSeconds / 60}:{displayedSeconds % 60:00}";
    }

    public float UpdateTimer()
    {
        return timeLeft;
    }
}
