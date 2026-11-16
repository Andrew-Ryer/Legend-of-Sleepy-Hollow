// using UnityEngine;

// public class ArcadePlayerMap : MonoBehaviour
// {
//     [Header("ArcadePlayer")]
//     [SerializeField] private Transform _ArcadePlayer;
    
//     [Header("ArcadeMap")]
//     [SerializeField] private Transform arcadeMap;
    
//     [Header("PlayerPosition")]
//     [SerializeField] private RectTransform playerPosition;

//     private RectTransform mapRect;

//     private void Awake()
//     {
//         mapRect = GetComponent<RectTransform>();
//     }

//     private void Update()
//     {
//         Vector3 localPosition = arcadeMap.InverseTransformPoint(_ArcadePlayer.position);

//         float x = Mathf.Clamp(localPosition.x, -0.5f, 0.5f);
//         float y = Mathf.Clamp(localPosition.y, -0.5f, 0.5f);

//         float mapX = x * mapRect.rect.width;
//         float mapY = y * mapRect.rect.height;

//         playerPosition.anchoredPosition = new Vector2(mapX, mapY);
//         playerPosition.localRotation = Quaternion.identity;
//     }
// }