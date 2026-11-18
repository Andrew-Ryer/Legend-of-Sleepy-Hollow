using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LeaderboardUI : MonoBehaviour
{
    [SerializeField] RaceProgressTracker _raceProgressTracker;
    [SerializeField] RectTransform _slotContainer;
    [SerializeField] RectTransform _slotTemplate;
    [SerializeField, Min(1f)] float _rowSpacing = 20;

    const float RefreshIntervalSeconds = 0.2f;
    float _nextRefreshTime;
    Vector2 _templatePosition;

    readonly List<LeaderboardRow> _rows = new();

    class LeaderboardRow
    {
        public RectTransform Transform;
        public TMP_Text Text;
    }

    void Awake()
    {
        if (_raceProgressTracker == null)
            _raceProgressTracker = FindFirstObjectByType<RaceProgressTracker>();

        if (_slotContainer == null)
            _slotContainer = transform as RectTransform;

        if (_slotContainer == null || _slotTemplate == null)
        {
            Debug.LogError("LeaderboardUI needs a slot container and a slot template.", this);
            enabled = false;
            return;
        }

        _templatePosition = _slotTemplate.anchoredPosition;
        LeaderboardRow templateRow = CreateRow(_slotTemplate);
        if (templateRow == null)
        {
            enabled = false;
            return;
        }

        _rows.Add(templateRow);
        _slotTemplate.gameObject.SetActive(false);
    }

    void Update()
    {
        if (_raceProgressTracker == null || Time.unscaledTime < _nextRefreshTime)
            return;

        _nextRefreshTime = Time.unscaledTime + RefreshIntervalSeconds;
        IReadOnlyList<RaceProgressTracker.PlayerProgress> rankings = _raceProgressTracker.RankedPlayers;
        EnsureRowCount(rankings.Count);

        for (int i = 0; i < _rows.Count; i++)
        {
            bool hasPlayer = i < rankings.Count;
            LeaderboardRow row = _rows[i];
            row.Transform.gameObject.SetActive(hasPlayer);
            if (!hasPlayer)
                continue;

            row.Transform.anchoredPosition = _templatePosition + Vector2.down * (_rowSpacing * i);
            row.Transform.SetSiblingIndex(i);
            row.Text.text = $"{i + 1}. {rankings[i].PlayerName} ({Mathf.RoundToInt(rankings[i].Progress)}%)";
        }
    }

    void EnsureRowCount(int count)
    {
        while (_rows.Count < count)
        {
            RectTransform rowTransform = Instantiate(_slotTemplate, _slotContainer);
            rowTransform.name = $"Leaderboard Slot {_rows.Count + 1}";
            rowTransform.gameObject.SetActive(true);

            LeaderboardRow row = CreateRow(rowTransform);
            if (row == null)
                return;

            _rows.Add(row);
        }
    }

    LeaderboardRow CreateRow(RectTransform rowTransform)
    {
        TMP_Text text = rowTransform.GetComponentInChildren<TMP_Text>(true);
        if (text == null)
        {
            Debug.LogError("The leaderboard slot template needs a TextMeshPro UI child.", rowTransform);
            return null;
        }

        return new LeaderboardRow
        {
            Transform = rowTransform,
            Text = text
        };
    }
}
