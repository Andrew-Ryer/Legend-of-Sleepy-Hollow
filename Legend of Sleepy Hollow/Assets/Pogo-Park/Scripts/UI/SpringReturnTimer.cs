using TMPro;
using UnityEngine;

// Timer that should call the SpringEndGameReturn script once it runs out (aka the game ends)
public class SpringReturnTimer : MonoBehaviour
{
    [SerializeField] CanvasGroup _scoreboardCanvasGroup;
    [SerializeField] TMP_Text _returnTimerText;
    [SerializeField] SpringEndGameReturn _endGameReturn;
    [SerializeField, Min(0f)] float _countdownSeconds = 10f;
    [SerializeField, Min(0f)] float _delayAfterZero = 1f;

    bool _countdownStarted;
    bool _zeroReached;
    bool _returnStarted;
    float _remainingTime;
    float _zeroReachedAt;
    int _lastDisplayedSecond = -1;

    void Awake()
    {
        if (_scoreboardCanvasGroup == null)
            _scoreboardCanvasGroup = GetComponent<CanvasGroup>();

        if (_endGameReturn == null)
            _endGameReturn = GetComponent<SpringEndGameReturn>();
    }

    void Update()
    {
        if (_returnStarted || _scoreboardCanvasGroup == null)
            return;

        if (!_countdownStarted)
        {
            if (_scoreboardCanvasGroup.alpha <= 0.99f)
                return;

            _countdownStarted = true;
            _remainingTime = _countdownSeconds;
            UpdateText(Mathf.CeilToInt(_remainingTime));
        }

        if (!_zeroReached)
        {
            _remainingTime = Mathf.Max(0f, _remainingTime - Time.unscaledDeltaTime);

            int displayedSecond = Mathf.CeilToInt(_remainingTime);
            if (displayedSecond != _lastDisplayedSecond)
                UpdateText(displayedSecond);

            if (_remainingTime <= 0f)
            {
                _zeroReached = true;
                _zeroReachedAt = Time.unscaledTime;
                UpdateText(0);
            }

            return;
        }

        if (Time.unscaledTime >= _zeroReachedAt + _delayAfterZero)
        {
            _returnStarted = true;

            if (_endGameReturn != null)
                _endGameReturn.ReturnToOverworld();
            else
                Debug.LogError("SpringReturnTimer needs a SpringEndGameReturn reference.", this);
        }
    }

    void UpdateText(int seconds)
    {
        _lastDisplayedSecond = seconds;

        if (_returnTimerText != null)
            _returnTimerText.text = $"Returning to Overworld... {seconds}";
    }
}