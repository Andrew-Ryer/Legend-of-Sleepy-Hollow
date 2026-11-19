using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class GameDuration_SS : NetworkBehaviour
{
    public Button readyButton;
    [SerializeField] float startTime = 60f;
    NetworkVariable<bool> isPlaying = new NetworkVariable<bool>();
    NetworkVariable<float> timer = new NetworkVariable<float>();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        readyButton.gameObject.SetActive(true);
        readyButton.onClick.AddListener(StartTimer);
        
        isPlaying.OnValueChanged += HandlePlayingChanged;
        
        //sets isPlaying value to false to start
        HandlePlayingChanged(false, isPlaying.Value);
    }

    public override void OnNetworkDespawn()
    {
        readyButton.onClick.RemoveListener(StartTimer);
        isPlaying.OnValueChanged -= HandlePlayingChanged;
        
        base.OnNetworkDespawn();
    }


    void HandlePlayingChanged(bool prevValue, bool newValue)
    {
        //hides and shows button on host and client side 
        readyButton.gameObject.SetActive(!newValue);
    }


    public void StartTimer()
    {        
        //button click requests server to start timer 
        RequestClickRpc();
    }

    void FixedUpdate()
    {
        if (!IsServer) return;
        
        //only counts down after button has been pressed
        if (isPlaying.Value)
        {
            timer.Value -= Time.fixedDeltaTime;
            Debug.Log(timer.Value);
            
            CheckDoneTimer();
        }
    }

    void CheckDoneTimer()
    {
        if (!IsServer) return;
        
        //stops timer when value hits zero 
        if (timer.Value <= 0)
        {
            isPlaying.Value = false;
            Debug.Log("stop timer");
            Debug.Log("GAME IS OVER");
        }
            
    }


    [Rpc(SendTo.Server)]

    void RequestClickRpc()
    {
        //server starts the timer
        timer.Value = startTime;
        isPlaying.Value = true;
        Debug.Log("start timer");
        Debug.Log(isPlaying);
    }

}
