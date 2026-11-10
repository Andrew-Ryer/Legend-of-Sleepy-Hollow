using UnityEngine;

public class Timer : MonoBehaviour
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

    public void Update()
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
    }
}
