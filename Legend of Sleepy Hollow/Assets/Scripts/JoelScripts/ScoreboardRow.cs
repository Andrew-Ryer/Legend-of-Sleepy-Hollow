using TMPro;
using UnityEngine;

/*
 * ScoreboardRow is one row in a team column: a username cell and a points cell.
 * The scoreboard scene prefab wires both TMP_Text fields in the inspector.
 */

public class ScoreboardRow : MonoBehaviour
{
    [SerializeField] TMP_Text _usernameText;
    [SerializeField] TMP_Text _pointsText;

    public void SetContent(string username, int points)
    {
        _usernameText.text = username;
        _pointsText.text = points.ToString();
    }
}
