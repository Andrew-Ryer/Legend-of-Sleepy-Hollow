using UnityEngine;
using Unity.Netcode;
public class KitchenTimer : NetworkBehaviour
{
    [SerializeField] private float timer = 5f;

    private NetworkVariable<float> timer1 = new NetworkVariable<float>();
    private NetworkVariable<bool> timerCheck = new NetworkVariable<bool>();
    public NetworkVariable<int> fireCount = new NetworkVariable<int>();
    public NetworkVariable<int> allScores = new NetworkVariable<int>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            fireCount.Value = 4;
        }
    }

    // Update is called once per frame
    private void Update()
    {
        if (!IsServer) return;

        if (NetworkManager.Singleton.ConnectedClients.Count >= 2)
        {
            //Debug.Log("Timer Ready!.");
            if (!timerCheck.Value)
            {
                timer1.Value = timer;
                timerCheck.Value = true;
            }
        }
        else
        {
            {
                timerCheck.Value = false;
                timer1.Value = timer;
            }
        }

        if (timerCheck.Value)
        {
            timer1.Value -= Time.deltaTime;

            if (timer1.Value <= 0) {
                timer1.Value = 0;
                timerCheck.Value = false;

                CheckWinner();
                
            }
        }
    }

    public void CheckWinner()
    {
        if (!IsServer) return;

        WaterGun_KF[] playerTotal = FindObjectsByType<WaterGun_KF>(FindObjectsSortMode.None);

        int highestScore = -1; // set a base score
        ulong winnerId = ulong.MaxValue;
        bool tie = false;

        foreach (WaterGun_KF player in playerTotal) //All players will go into a list. This will determine it all...
        {
            int score = player.playerScore.Value;
            if (score > highestScore)
            {
                highestScore = score;
                winnerId = player.OwnerClientId; //the winner id.
                tie = false;
            } else if (score == highestScore)
            {
                tie = true;
            }
        }

        if (tie)
        {
            Debug.Log("We need a tiebreaker");
        } else
        {
            Debug.Log("Our Winner... Player " + winnerId);
            Debug.Log("Winning Score: " + highestScore);
        }
    }
}
