using TMPro;
using System;
using Unity.Netcode;
using TMPro;
using UnityEngine;

// Updated to now end the game by setting deltaTime to 0 if that feature is enabled
// As well as featuring an optional countdown that only affects the players in PogoPark
public class Timer : MonoBehaviour
{
    [SerializeField, Min(0f)] float timeLeft = 60f;
    [SerializeField] bool timerIsRunning = true;
    [SerializeField] bool endGameWhenTimeRunsOut;
    [SerializeField] bool countdownTimer;
    [SerializeField, Min(0f)] float countdownTime = 3f;
    [SerializeField] TMP_Text timerText;

    float _startingTime;
    float _countdownRemaining;
    float _elapsedTime;
    int _lastLoggedSecond = -1;
    int _lastDisplayedSecond = -1;
    bool _isCountingDown;
    bool _hasExpired;

    public float TimeLeft => Mathf.Max(0f, timeLeft);
    public float ElapsedTime => _elapsedTime;
    public bool IsRunning => timerIsRunning;
    public bool HasExpired => _hasExpired;
    public bool GameplayStarted => timerIsRunning && !_isCountingDown && !_hasExpired;
    public event Action TimerExpired;

    void Awake()
    {
        _startingTime = Mathf.Max(0f, timeLeft);
        if (timerIsRunning)
            BeginCountdownOrRun();
        UpdateTimerText();
    }

    public void StartTimer()
    {
        if (_hasExpired && endGameWhenTimeRunsOut)
            Time.timeScale = 1f;

        timeLeft = _startingTime;
        _elapsedTime = 0f;
        _lastLoggedSecond = -1;
        _hasExpired = false;
        BeginCountdownOrRun();
        UpdateTimerText();
    }

    public void StopTimer()
    {
        _isCountingDown = false;
        timerIsRunning = false;
        UpdateTimerText();
    }

    void Update()
    {
        if (_isCountingDown)
        {
            _countdownRemaining = Mathf.Max(0f, _countdownRemaining - Time.deltaTime);
            UpdateTimerText();

            if (_countdownRemaining <= 0f)
            {
                _isCountingDown = false;
                timerIsRunning = true;
                _elapsedTime = 0f;
                _lastLoggedSecond = -1;
                _lastDisplayedSecond = -1;
                UpdateTimerText();
            }

            return;
        }

        if (!timerIsRunning)
            return;

        float deltaTime = Time.deltaTime;
        timeLeft = Mathf.Max(0f, timeLeft - deltaTime);
        _elapsedTime += deltaTime;
        UpdateTimerText();

        int remainingSeconds = Mathf.CeilToInt(timeLeft);
        if (remainingSeconds != _lastLoggedSecond)
        {
            _lastLoggedSecond = remainingSeconds;
            if (ShouldLog())
                Debug.Log($"Time remaining: {remainingSeconds}");
        }

        if (timeLeft > 0f || _hasExpired)
            return;

        _hasExpired = true;
        timerIsRunning = false;
        TimerExpired?.Invoke();

        if (endGameWhenTimeRunsOut)
        {
            if (ShouldLog())
                Debug.Log("Game Over!");
            Time.timeScale = 0f;
        }
    }

    bool ShouldLog()
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        return networkManager == null || networkManager.IsServer;
    }

    void UpdateTimerText()
    {
        if (timerText == null)
            return;

        int displayedSeconds = _isCountingDown
            ? Mathf.CeilToInt(_countdownRemaining)
            : Mathf.CeilToInt(Mathf.Max(0f, timeLeft));
        if (displayedSeconds == _lastDisplayedSecond)
            return;

        _lastDisplayedSecond = displayedSeconds;
        timerText.text = _isCountingDown
            ? $"Starting in\n{displayedSeconds}"
            : $"Time\n{displayedSeconds / 60}:{displayedSeconds % 60:00}";
    }

    void BeginCountdownOrRun()
    {
        if (countdownTimer && countdownTime > 0f)
        {
            _isCountingDown = true;
            timerIsRunning = false;
            _countdownRemaining = countdownTime;
            _lastDisplayedSecond = -1;
        }
        else
        {
            _isCountingDown = false;
            timerIsRunning = true;
        }
    }

    public float UpdateTimer()
    {
        return timeLeft;
    }
}
