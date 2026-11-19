using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class GameDuration_SS : NetworkBehaviour
{
    public Button readyButton;
    [SerializeField] float startTime = 60f;
    bool isPlaying;
    private float timer;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        readyButton.gameObject.SetActive(true);
        readyButton.onClick.AddListener(StartTimer);
        
        isPlaying = false;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
    }


    public void StartTimer()
    {        
        RequestClickRpc();
    }

    void Update()
    {
        if (!IsServer) return;
        
        if (isPlaying)
        {
            RequestTimerRpc();
        }
    }

    void CheckDoneTimer()
    {
        if (!IsServer) return;
        
        if (timer <= 0)
        {
            isPlaying = false;
            Debug.Log("stop timer");
            Debug.Log("GAME IS OVER");
        }
            
    }
    

    [Rpc(SendTo.Server)]
    void RequestTimerRpc()
    {
        Debug.Log("requesting rpc timer");

        if (isPlaying)
        {
            timer -= Time.deltaTime;
            Debug.Log(timer);
            
            CheckDoneTimer();
        }
    }

    void RequestClickRpc()
    {
        readyButton.gameObject.SetActive(false);

        timer = startTime;
        isPlaying = true;
        Debug.Log("start timer");
        Debug.Log(isPlaying);
    }
}
