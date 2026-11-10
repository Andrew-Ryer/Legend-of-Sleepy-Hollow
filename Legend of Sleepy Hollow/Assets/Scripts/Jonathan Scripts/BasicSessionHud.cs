using UnityEngine;

/*
 * BasicSessionHud is Demo 01's connection UI and later demos reuse it. Its
 * buttons ask BasicSessionManager to host, join, or disconnect; its labels
 * display the manager's local role and replicated player count.
 */

public class BasicSessionHud : MonoBehaviour
{
    [SerializeField] BasicSessionManager sessionManager;

    const float Margin = 16f;
    const float StatusWidth = 220f;
    const float StatusHeight = 54f;
    const float ControlsWidth = 320f;
    const float ControlsHeight = 40f;
    const float ButtonWidth = 140f;
    const float ButtonHeight = 30f;

    static readonly GUILayoutOption[] ButtonSize =
    {
        GUILayout.Width(ButtonWidth),
        GUILayout.Height(ButtonHeight)
    };

    void OnGUI()
    {
        DrawStatus();
        DrawControls();
    }

    void DrawStatus()
    {
        GUILayout.BeginArea(new Rect(Margin, Margin, StatusWidth, StatusHeight));
        GUILayout.Label(sessionManager.IsConnected ? $"Connected: {sessionManager.LocalRole}" : "Disconnected");

        if (sessionManager.IsConnected)
            GUILayout.Label($"Players: {sessionManager.PlayerCount}");

        GUILayout.EndArea();
    }

    void DrawControls()
    {
        float centerX = (Screen.width - ControlsWidth) * 0.5f;
        GUILayout.BeginArea(new Rect(centerX, Margin, ControlsWidth, ControlsHeight));
        GUILayout.BeginHorizontal();

        if (sessionManager.IsConnected)
        {
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Disconnect", ButtonSize))
                sessionManager.Disconnect();

            GUILayout.FlexibleSpace();
        }
        else
        {
            if (GUILayout.Button("Host Session", ButtonSize))
                sessionManager.StartHost();

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Join Client", ButtonSize))
                sessionManager.StartClient();
        }

        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }
}
