using UnityEngine;
using UnityEngine.InputSystem;

/*
 * OverworldScoreboard is the box in the top-left corner. Collapsed it is a
 * small square; hold Tab to expand the full scores, release to shrink back.
 */

public class OverworldScoreboard : ScoreboardUi
{
    [SerializeField] GameObject _expandedContent;

    static readonly Vector2 ExpandedSize = new(684, 420);

    RectTransform _rect;
    Vector2 _collapsedSize;

    void Awake()
    {
        _rect = (RectTransform)transform;
        _collapsedSize = _rect.sizeDelta;
    }

    protected void OnEnable()
    {
        Subscribe();
        Refresh();
    }

    protected void OnDisable()
    {
        Unsubscribe();
    }

    void Update()
    {
        SetExpanded(Keyboard.current != null && Keyboard.current.tabKey.isPressed);
    }

    void SetExpanded(bool open)
    {
        if (_expandedContent.activeSelf == open) return;

        _expandedContent.SetActive(open);
        _rect.sizeDelta = open ? ExpandedSize : _collapsedSize;
        if (open) Refresh();
    }
}
