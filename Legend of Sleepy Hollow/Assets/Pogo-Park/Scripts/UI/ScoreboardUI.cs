using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class ScoreboardUI : MonoBehaviour
{
    [Serializable]
    class ScoreboardSlot
    {
        public RectTransform slot;
        public TMP_Text playerText;
        public TMP_Text timeAndPointsText;
        public Image background;
    }

    [SerializeField] RaceProgressTracker _raceProgressTracker;
    [SerializeField] RectTransform _slotContainer;

    const float RefreshIntervalSeconds = 0.2f;
    float _nextRefreshTime;
    CanvasGroup _canvasGroup;
    readonly List<ScoreboardSlot> _slots = new();
    readonly Color[] _placeColors = new Color[4];

    void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (_raceProgressTracker == null)
            _raceProgressTracker = FindFirstObjectByType<RaceProgressTracker>();

        if (_slotContainer == null || _slotContainer == transform)
        {
            Transform background = transform.Find("Background");
            _slotContainer = background != null
                ? background as RectTransform
                : transform as RectTransform;
        }

        SetVisible(false);
        LoadPlaceColors();
        FindSlots();
    }

    void Update()
    {
        if (_raceProgressTracker == null || Time.unscaledTime < _nextRefreshTime)
            return;

        _nextRefreshTime = Time.unscaledTime + RefreshIntervalSeconds;
        IReadOnlyList<RaceProgressTracker.PlayerProgress> players = _raceProgressTracker.FinalPlayers;
        bool shouldShow = _raceProgressTracker.RaceFinished && players.Count > 0;
        SetVisible(shouldShow);

        for (int i = 0; i < _slots.Count; i++)
        {
            ScoreboardSlot slot = _slots[i];
            bool hasPlayer = shouldShow && i < players.Count;
            if (slot.slot != null)
                slot.slot.gameObject.SetActive(hasPlayer);

            if (!hasPlayer)
                continue;

            RaceProgressTracker.PlayerProgress player = players[i];
            if (slot.playerText != null)
                slot.playerText.text = $"{i + 1}{GetPlaceSuffix(i + 1)} Place: {player.PlayerName}";

            if (slot.timeAndPointsText != null)
            {
                slot.timeAndPointsText.text = player.HasFinished
                    ? $"{player.FinishTime:F3}s ({player.Points} points)"
                    : $"{Mathf.RoundToInt(player.Progress)}% ({player.Points} points)";
            }

            if (slot.background != null)
                slot.background.color = GetPlaceColor(i);
        }

        if (shouldShow && players.Count > _slots.Count)
            Debug.LogWarning($"Scoreboard has {_slots.Count} slots but {players.Count} players are in the race.", this);
    }

    void FindSlots()
    {
        if (_slotContainer == null)
        {
            Debug.LogError("ScoreboardUI needs a slot container.", this);
            return;
        }

        for (int i = 0; i < _slotContainer.childCount; i++)
        {
            if (_slotContainer.GetChild(i) is not RectTransform slotTransform)
                continue;

            TMP_Text playerText = null;
            TMP_Text timeAndPointsText = null;
            TMP_Text[] texts = slotTransform.GetComponentsInChildren<TMP_Text>(true);

            foreach (TMP_Text text in texts)
            {
                string name = text.name.ToLowerInvariant();
                if (name.Contains("time") || name.Contains("score"))
                    timeAndPointsText = text;
                else if (name.Contains("player") || name.Contains("name"))
                    playerText = text;
                else if (playerText == null)
                    playerText = text;
                else if (timeAndPointsText == null)
                    timeAndPointsText = text;
            }

            if (playerText == null || timeAndPointsText == null)
            {
                Debug.LogWarning($"{slotTransform.name} needs separate player/name and time/score TextMeshPro children.", slotTransform);
            }

            _slots.Add(new ScoreboardSlot
            {
                slot = slotTransform,
                playerText = playerText,
                timeAndPointsText = timeAndPointsText,
                background = slotTransform.GetComponent<Image>()
            });

            slotTransform.gameObject.SetActive(false);
        }
    }

    void LoadPlaceColors()
    {
        _placeColors[0] = ParseColor("#FFEB04");
        _placeColors[1] = ParseColor("#FF8C00");
        _placeColors[2] = ParseColor("#B0E0E6");
        _placeColors[3] = ParseColor("#B2B2B2");
    }

    static Color ParseColor(string htmlColor)
    {
        ColorUtility.TryParseHtmlString(htmlColor, out Color color);
        return color;
    }

    Color GetPlaceColor(int index)
    {
        return _placeColors[Mathf.Min(index, _placeColors.Length - 1)];
    }

    void SetVisible(bool visible)
    {
        if (_canvasGroup == null)
            return;

        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.interactable = visible;
        _canvasGroup.blocksRaycasts = visible;
    }

    static string GetPlaceSuffix(int place)
    {
        int lastTwoDigits = place % 100;
        if (lastTwoDigits >= 11 && lastTwoDigits <= 13)
            return "th";

        return (place % 10) switch
        {
            1 => "st",
            2 => "nd",
            3 => "rd",
            _ => "th"
        };
    }
}
