using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

public class ArcadePlayerMap : MonoBehaviour
{
    [Header("Arcade Map")]
    [SerializeField] private Transform arcadeMap;
    private float arcadeMapWidth = 150f;
    private float arcadeMapHeight = 150f;

    [Header("Player Tracker")]
    [SerializeField] private RectTransform playerTracker;

    [Header("Other Variables")]
    private Dictionary<ulong, RectTransform> playerTrackers = new Dictionary<ulong, RectTransform>();

    private RectTransform arcadeMapRect;

    private void Awake()
    {
        arcadeMapRect = GetComponent<RectTransform>();
    }

    private void Update()
    {
        if (NetworkManager.Singleton == null)
        {
            return;
        }

        foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null)
            {
                continue;
            }

            ulong playerId = client.ClientId;

            Transform player = client.PlayerObject.transform;

            if (!playerTrackers.ContainsKey(playerId))
            {
                RectTransform newPlayerTracker = Instantiate(playerTracker, arcadeMapRect);

                newPlayerTracker.gameObject.SetActive(true);

                playerTrackers.Add(playerId, newPlayerTracker);
            }

            Vector3 position = arcadeMap.InverseTransformPoint(player.position);

            float x = Mathf.Clamp(position.x / arcadeMapWidth, -0.5f, 0.5f);
            float y = Mathf.Clamp(position.z / arcadeMapHeight, -0.5f, 0.5f);

            float arcadeMapX = x * arcadeMapRect.rect.width;
            float arcadeMapY = y * arcadeMapRect.rect.height;

            playerTrackers[playerId].anchoredPosition = new Vector2(arcadeMapX, arcadeMapY);
            playerTrackers[playerId].localRotation = Quaternion.identity;
        }

        List<ulong> disconnectedArcadePlayers = new List<ulong>();

        foreach (ulong playerId in playerTrackers.Keys)
        {
            bool arcadePlayerExists = false;

            foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (client.ClientId == playerId && client.PlayerObject != null)
                {
                    arcadePlayerExists = true;
                    
                    break;
                }
            }

            if (!arcadePlayerExists)
            {
                disconnectedArcadePlayers.Add(playerId);
            }
        }

        foreach (ulong playerId in disconnectedArcadePlayers)
        {
            Destroy(playerTrackers[playerId].gameObject);

            playerTrackers.Remove(playerId);
        }
    }
}