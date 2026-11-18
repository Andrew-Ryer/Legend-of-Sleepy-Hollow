using TMPro;
using UnityEngine;

public class CountdownToRace : MonoBehaviour
{
    [SerializeField] float timeLeft = 5;
    [SerializeField] bool timerIsRunning = true;

    public void StartTimer()
    {
        timerIsRunning = true;
    }

    public void StopTimer()
    {
        timerIsRunning = false;
    }

    public float UpdateTimer()
    {
        if (timerIsRunning)
        {
            timeLeft -= Time.deltaTime;
            // Debug.Log("Time remaining: " + timeLeft);
        }
        if (timeLeft <= 0)
        {
            // Debug.Log("Times Up!");
            StopTimer();
        }
        return timeLeft;
    }
}