using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class ArcadeGameManager : MonoBehaviour
{
    [Header("NetworkUI")]
    [SerializeField] private GameObject networkUI;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;

    private void Awake()
    {
        networkUI.SetActive(true);
        hostButton.onClick.AddListener(HostStart);
        clientButton.onClick.AddListener(ClientStart);
    }

    private void HostStart()
    {
        if (NetworkManager.Singleton.StartHost())
        {
            networkUI.SetActive(false);
            
            Debug.Log("Starting HOST");
        }
    }

    private void ClientStart()
    {
        if (NetworkManager.Singleton.StartClient())
        {
            networkUI.SetActive(false);

            Debug.Log("Starting CLIENT");
        }
    }
}