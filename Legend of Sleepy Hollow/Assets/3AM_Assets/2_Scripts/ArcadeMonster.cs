using UnityEngine;
using Unity.Netcode;

public class ArcadeMonster : NetworkBehaviour
{
    [Header("Monster Properties")]
    [SerializeField] private float speed = 5f;
    private float detectionRange = 10f;

    [Header("Arcade Map")]
    [SerializeField] private Transform arcadeMap;

    [Header("Other Variable")]
    private ArcadeGameManager gameManager;

    private Transform targetPlayer;
    
    private Vector3 moveDirection;

    private float changeDirectionTimer = 3f;

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            return;
        }

        gameManager = FindAnyObjectByType<ArcadeGameManager>();

        ChangeDirection();
    }

    private void Update()
    {
        if (!IsServer)
        {
            return;
        }

        if (!gameManager.GameStarted || gameManager.GameEnded)
        {
            return;
        }

        FindNearestPlayer();

        if (targetPlayer != null)
        {
            ChasePlayer();
        }
        else
        {
            Roam();
        }
    }

    private void FindNearestPlayer()
    {
        targetPlayer = null;

        ArcadePlayer [] players = FindObjectsByType<ArcadePlayer>(FindObjectsSortMode.None);

        float closestDistance = detectionRange;

        foreach (ArcadePlayer player in players)
        {
            if (!player.IsSpawned)
            {
                continue;
            }

            float distance = Vector3.Distance(transform.position, player.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;

                targetPlayer = player.transform;
            }
        }
    }

    private void ChasePlayer()
    {
        Vector3 targetDirection = targetPlayer.position - transform.position;

        targetDirection.y = 0f;

        MonsterMove(targetDirection);
    }

    private void Roam()
    {
        changeDirectionTimer -= Time.deltaTime;

        if (changeDirectionTimer <= 0f)
        {
            ChangeDirection();
        }

        MonsterMove(moveDirection);
    }

    private void ChangeDirection()
    {
        float randomX = Random.Range(-1f, 1f);
        float randomZ= Random.Range(-1f, 1f);

        moveDirection = new Vector3(randomX, 0f, randomZ).normalized;

        changeDirectionTimer = 3f;
    }

    private void MonsterMove(Vector3 direction)
    {
        transform.position += direction * speed * Time.deltaTime;
        transform.rotation = Quaternion.LookRotation(direction);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer || !gameManager.GameStarted || gameManager.GameEnded)
        {
            return;
        }

        ArcadePlayer player = collision.gameObject.GetComponentInParent<ArcadePlayer>();

        if (player != null && player.IsSpawned)
        {
            gameManager.playerDiedTMP.text = $"Monster Has Killed {player.name}!";

            player.NetworkObject.Despawn(true);

            gameManager.CheckAllPlayerStatus();
        }
    }
}