using System;
using TMPro;
using UnityEngine;

public class GameStartCountdownUI : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI countdownText;

    [SerializeField]
    private SessionStateMachine sessionStateMachine;

    private Animator animator;
    private CanvasGroup canvasGroup;
    private int previousCountdownNumber;
    private const string NUMBER_POPUP = "NumberPopup";

    private void Awake()
    {
        animator = GetComponent<Animator>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Start()
    {
        if (sessionStateMachine == null)
            sessionStateMachine = FindFirstObjectByType<SessionStateMachine>();

        Hide();
    }

    private void Update()
    {
        if (sessionStateMachine == null)
            return;

        if (sessionStateMachine.CurrentPhase != SessionPhase.Countdown)
        {
            Hide();
            return;
        }

        Show();

        int countdownNumber = Mathf.CeilToInt(sessionStateMachine.CountdownRemaining);

        countdownText.text = countdownNumber.ToString();

        if (previousCountdownNumber != countdownNumber)
        {
            previousCountdownNumber = countdownNumber;
            animator.SetTrigger(NUMBER_POPUP);
        }
    }

    private void Show()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    private void Hide()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
}
