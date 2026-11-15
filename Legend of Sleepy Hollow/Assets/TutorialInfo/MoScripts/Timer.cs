using System;
using Unity.Netcode;
using TMPro;
using UnityEngine;

// Updated to now end the game by setting deltaTime to 0 if that feature is enabled
public class Timer : MonoBehaviour
{
    [SerializeField, Min(0f)] float timeLeft = 60f;
    [SerializeField] bool timerIsRunning = true;
    [SerializeField] bool endGameWhenTimeRunsOut;
    [SerializeField] TMP_Text timerText;

    float _startingTime;
    float _elapsedTime;
    int _lastLoggedSecond = -1;
    int _lastDisplayedSecond = -1;
    bool _hasExpired;

    public float TimeLeft => Mathf.Max(0f, timeLeft);
    public float ElapsedTime => _elapsedTime;
    public bool IsRunning => timerIsRunning;
    public bool HasExpired => _hasExpired;
    public event Action TimerExpired;

    void Awake()
    {
        _startingTime = Mathf.Max(0f, timeLeft);
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
        timerIsRunning = true;
        UpdateTimerText();
    }

    public void StopTimer()
    {
        timerIsRunning = false;
    }

    void Update()
    {
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

        int displayedSeconds = Mathf.CeilToInt(Mathf.Max(0f, timeLeft));
        if (displayedSeconds == _lastDisplayedSecond)
            return;

        _lastDisplayedSecond = displayedSeconds;
        timerText.text = $"Time\n{displayedSeconds / 60}:{displayedSeconds % 60:00}";
    }
}
