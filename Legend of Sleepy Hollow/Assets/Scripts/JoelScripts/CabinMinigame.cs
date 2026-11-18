using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CabinMinigame : MonoBehaviour
{
    [SerializeField] string _sceneName;
    [SerializeField] string _displayName;
    [SerializeField] Color _nameColor = new Color(1f, 0.82f, 0.2f);

    static CabinMinigame _enteringCabin;
    TextMeshPro _nameLabel;
    Camera _labelCamera;

    void Awake()
    {
        BoxCollider doorway = gameObject.AddComponent<BoxCollider>();
        doorway.isTrigger = true;
        doorway.center = new Vector3(0f, 1.2f, -1.6f);
        doorway.size = new Vector3(1.2f, 2.4f, 1f);

        _nameLabel = new GameObject("Minigame Name").AddComponent<TextMeshPro>();
        _nameLabel.transform.SetParent(transform, false);
        _nameLabel.transform.localPosition = new Vector3(0f, 4.8f, -0.6f);
        _nameLabel.text = string.IsNullOrWhiteSpace(_displayName) ? _sceneName : _displayName;
        _nameLabel.fontSize = 5f;
        _nameLabel.fontStyle = FontStyles.Bold;
        _nameLabel.alignment = TextAlignmentOptions.Center;
        _nameLabel.color = _nameColor;
        _nameLabel.textWrappingMode = TextWrappingModes.NoWrap;
        _nameLabel.rectTransform.sizeDelta = new Vector2(9f, 1f);
        _labelCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (_labelCamera != null)
            _nameLabel.transform.rotation = _labelCamera.transform.rotation;
    }

    void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (_enteringCabin != null || player == null || !player.IsSpawned || !player.IsOwner || !player.IsHost) return;

        if (!Application.CanStreamedLevelBeLoaded(_sceneName))
        {
            Debug.LogError($"Cabin scene '{_sceneName}' is missing from the Scene List.", this);
            return;
        }

        PlayerRoster.Local.EnterCabinRpc(_sceneName);
    }

    public static void EnterScene(string sceneName)
    {
        if (_enteringCabin != null) return;
        _enteringCabin = FindFirstObjectByType<CabinMinigame>();
        if (_enteringCabin != null)
            _enteringCabin.StartCoroutine(_enteringCabin.EnterMinigame(sceneName));
    }

    IEnumerator EnterMinigame(string sceneName)
    {
        Time.timeScale = 1f;
        NetworkManager manager = NetworkManager.Singleton;

        if (manager != null && manager.IsHost)
        {
            manager.ConnectionApprovalCallback = (_, response) => response.Approved = false;
            // Let every client receive the scene change and leave before the host shuts down.
            while (manager.ConnectedClientsIds.Count > 1) yield return null;
        }

        // Leave the overworld session so the minigame can use its own network setup.
        GameSessionManager bootstrap = FindFirstObjectByType<GameSessionManager>();
        if (bootstrap != null) Destroy(bootstrap.gameObject);
        yield return null;

        if (manager != null)
        {
            manager.Shutdown();
            while (manager != null && (manager.IsListening || manager.ShutdownInProgress))
                yield return null;

            if (manager != null) Destroy(manager.gameObject);
            yield return null;
        }

        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
}
