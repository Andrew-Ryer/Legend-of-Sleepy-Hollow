using TMPro;
using Unity.Netcode;
using UnityEngine;

/*
 * PlayerNameLabel sets the overhead nameplate text from this player's replicated
 * roster entry and keeps the nameplate facing the main camera. Same structure as
 * the Resource Collector nameplate: Nameplate -> Canvas -> Label.
 *
 * It is a plain MonoBehaviour: every player object is replicated to every client,
 * and its NetworkObject.OwnerClientId tells whose roster row to show.
 */

public class PlayerNameLabel : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI _label;

    NetworkObject _playerObject;

    void Start()
    {
        _playerObject = GetComponentInParent<NetworkObject>();

        PlayerRoster.PlayerRosterUpdated += HandleRosterUpdated;
        ApplyText();
    }

    void OnDestroy()
    {
        PlayerRoster.PlayerRosterUpdated -= HandleRosterUpdated;
    }

    void HandleRosterUpdated(object sender, PlayerRoster.PlayerRosterUpdatedArgs e)
    {
        ApplyText();
    }

    void ApplyText()
    {
        if (_playerObject == null) return;

        PlayerRosterEntry? entry = PlayerRoster.Local?.FindByClientId(_playerObject.OwnerClientId);
        _label.text = entry.HasValue ? entry.Value.Username : $"Player {_playerObject.OwnerClientId}";
    }

    void LateUpdate()
    {
        if (Camera.main == null) return;

        // A world-space canvas shows its text toward its back (-Z) side, so the
        // canvas must look AWAY from the camera for the name to face it.
        Vector3 awayFromCamera = _label.transform.position - Camera.main.transform.position;
        if (awayFromCamera.sqrMagnitude > 0.0001f)
            _label.transform.rotation = Quaternion.LookRotation(awayFromCamera);
    }
}
