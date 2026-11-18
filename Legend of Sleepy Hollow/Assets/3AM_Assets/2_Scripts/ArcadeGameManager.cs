using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;

public class ArcadeGameManager : NetworkBehaviour
{
    [Header("NetworkUI")]
    [SerializeField] private GameObject networkUI;

    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;

    [Header("DefaultUI")]
    [SerializeField] private GameObject defaultUI;

    [SerializeField] private TextMeshProUGUI gameTimerTMP;

    [Header("PopUps")]
    [SerializeField] private TextMeshProUGUI gameWonTMP;
    [SerializeField] private TextMeshProUGUI gameLoseTMP;

    public TextMeshProUGUI availableArcadeMachineTMP;
    public TextMeshProUGUI fixedArcadeMachineTMP;
    public TextMeshProUGUI arcadeMachineAlreadyFixedTMP;

    public TextMeshProUGUI flashlightLifeTMP;
    public TextMeshProUGUI flashlightLifeDeadTMP;

    public TextMeshProUGUI playerDiedTMP;

    [Header("Other Variables")]
    private int requiredPlayers = 2;
    private int requiredMachines = 6;

    private NetworkVariable<float> gameTimer = new NetworkVariable<float>(45f);

    private NetworkVariable<bool> gameStarted = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> gameEnded = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> gameWon = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> allPlayersDied = new NetworkVariable<bool>(false);

    public bool GameStarted => gameStarted.Value;
    public bool GameEnded => gameEnded.Value;
    public bool GameWon => gameWon.Value;

    private void Awake()
    {
        networkUI.SetActive(true);

        hostButton.onClick.AddListener(HostStart);
        clientButton.onClick.AddListener(ClientStart);

        defaultUI.SetActive(false);
        gameTimerTMP.text = gameTimer.Value.ToString("F0");

        gameWonTMP.gameObject.SetActive(false);
        gameLoseTMP.gameObject.SetActive(false);

        availableArcadeMachineTMP.gameObject.SetActive(false);
        availableArcadeMachineTMP.text = "Press E To Fix Arcade Machine";

        fixedArcadeMachineTMP.gameObject.SetActive(false);
        fixedArcadeMachineTMP.text = "Arcade Machine Fixed";

        arcadeMachineAlreadyFixedTMP.gameObject.SetActive(false);
        arcadeMachineAlreadyFixedTMP.text = "Arcade Machine Already Fixed";

        playerDiedTMP.gameObject.SetActive(false);
    }

    private void Update()
    {
        gameTimerTMP.text = gameTimer.Value.ToString("F0");

        if (gameEnded.Value)
        {
            if (gameWon.Value)
            {
                gameWonTMP.gameObject.SetActive(true);
                gameWonTMP.text = "PLAYERS WON\nALL MACHINES HAVE BEEN FIXED";
            }
            else
            {
                gameLoseTMP.gameObject.SetActive(true);

                if (allPlayersDied.Value)
                {
                    gameLoseTMP.text = "GAME OVER\nALL PLAYERS DIED";
                }
                else
                {
                    gameLoseTMP.text = "GAME OVER\nTIME RAN OUT";
                }
            }
        }

        if (!IsServer)
        {
            return;
        }

        if (gameEnded.Value)
        {
            return;
        }

        if (!gameStarted.Value)
        {
            if (NetworkManager.Singleton.ConnectedClientsIds.Count >= requiredPlayers)
            {
                gameStarted.Value = true;
            }

            return;
        }

        CheckMachines();

        CheckAllPlayerStatus();

        if (gameEnded.Value)
        {
            return;
        }   

        if (gameTimer.Value > 0f)
        {
            gameTimer.Value = Mathf.Max(0f, gameTimer.Value - Time.deltaTime);
        }

        if (gameTimer.Value <= 0f)
        {
            gameTimer.Value = 0f;

            gameStarted.Value = false;
            gameEnded.Value = true;
        }
    }

    private void HostStart()
    {
        if (NetworkManager.Singleton.StartHost())
        {
            networkUI.SetActive(false);

            defaultUI.SetActive(true);
        }
    }

    private void ClientStart()
    {
        if (NetworkManager.Singleton.StartClient())
        {
            networkUI.SetActive(false);

            defaultUI.SetActive(true);
        }
    }

    public void CheckMachines()
    {
        if (!IsServer || !gameStarted.Value || gameEnded.Value)
        {
            return;
        }

        ArcadeMachine [] machines = FindObjectsByType<ArcadeMachine>(FindObjectsSortMode.None);

        int fixedMachines = 0;

        foreach (ArcadeMachine machine in machines)
        {
            if (machine.alreadyFixed)
            {
                fixedMachines++;
            }
        }

        if (machines.Length == requiredMachines && fixedMachines == requiredMachines)
        {
            gameWon.Value = true;
            gameEnded.Value = true;
            gameStarted.Value = false;
        }
    }

    public void CheckAllPlayerStatus()
    {
        if (!IsServer || !gameStarted.Value || gameEnded.Value)
        {
            return;
        }

        ArcadePlayer [] players = FindObjectsByType<ArcadePlayer>(FindObjectsSortMode.None);

        int alivePlayers = 0;

        foreach (ArcadePlayer player in players)
        {
            if (player.IsSpawned)
            {
                alivePlayers++;
            }
        }
        
        if (alivePlayers == 0)
        {
            gameStarted.Value = false;
            gameEnded.Value = true;
            allPlayersDied.Value = true;
        }
    }
}