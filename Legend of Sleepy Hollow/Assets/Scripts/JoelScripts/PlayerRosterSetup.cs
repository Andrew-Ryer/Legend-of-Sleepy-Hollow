using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/*
 * PlayerRosterSetup is the small overworld lobby panel: a username field and
 * two team buttons. Everything it touches is wired in the inspector; the only
 * code path is the button click.
 */

public class PlayerRosterSetup : MonoBehaviour
{
    public static string LocalUsername { get; set; } = "";

    [SerializeField] TMP_InputField _usernameInput;
    [SerializeField] Button _saveUsernameButton;
    [SerializeField] Button _mortalTeamButton;
    [SerializeField] Button _ghostTeamButton;

    void OnEnable()
    {
        if (_usernameInput != null && string.IsNullOrEmpty(_usernameInput.text))
            _usernameInput.text = LocalUsername;

        _saveUsernameButton.onClick.AddListener(SaveUsername);
        _mortalTeamButton.onClick.AddListener(HandleMortalClicked);
        _ghostTeamButton.onClick.AddListener(HandleGhostClicked);
    }

    void OnDisable()
    {
        _saveUsernameButton.onClick.RemoveListener(SaveUsername);
        _mortalTeamButton.onClick.RemoveListener(HandleMortalClicked);
        _ghostTeamButton.onClick.RemoveListener(HandleGhostClicked);
    }

    void SaveUsername()
    {
        PlayerIdentity.LocalSetUsername(_usernameInput.text);
        _usernameInput.text = LocalUsername;
    }

    void HandleMortalClicked() => SwitchTeam(GameTeam.Mortal);
    void HandleGhostClicked() => SwitchTeam(GameTeam.Ghost);

    void SwitchTeam(GameTeam team)
    {
        if (NetworkManager.Singleton == null || !(NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsClient)) return;
        PlayerIdentity.LocalSwitchTeam(team);
    }
}
