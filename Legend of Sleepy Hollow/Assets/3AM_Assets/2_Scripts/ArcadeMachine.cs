using UnityEngine;
using Unity.Netcode;

public class ArcadeMachine : NetworkBehaviour
{
    [Header("Machine Properties")]
    [SerializeField] private Transform monsterSpawnPoint;

    [SerializeField] private GameObject monster;

    private NetworkVariable<bool> machineFixed = new NetworkVariable<bool>(false);

    public bool alreadyFixed => machineFixed.Value;

    [Header("Monster Spawn Properties")]
    [SerializeField] private float spawnTimer = 30f;
    
    [Header("Other Variables")]
    private ArcadeGameManager gameManager;

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            return;
        }

        gameManager = FindAnyObjectByType<ArcadeGameManager>();
    }

    private void Update()
    {
        if (!IsServer)
        {
            return;
        }

        if (!gameManager.GameStarted || gameManager.GameEnded || alreadyFixed)
        {
            return;
        }

        spawnTimer -= Time.deltaTime;

        if (spawnTimer <= 0f)
        {
            SpawnMonster();

            spawnTimer = 30f;
        }
    }

    private void SpawnMonster()
    {
        GameObject enemy = Instantiate(monster, monsterSpawnPoint.position, monsterSpawnPoint.rotation);

        enemy.GetComponent<NetworkObject>().Spawn();
    }

    public void RepairMachine()
    {
        if (!IsServer || alreadyFixed)
        {
            return;
        }

        machineFixed.Value = true;

        gameManager.CheckMachines();
    }
}