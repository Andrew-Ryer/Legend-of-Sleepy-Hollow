using UnityEngine;
using UnityEngine.InputSystem;

public class ArcadePlayerCamera : MonoBehaviour
{
    [Header("PlayerPrefab")]
    [SerializeField] private Transform _ArcadePlayer;

    [Header("DefaultVariables")]
    [SerializeField] private float sensitivity = 1f;
    private float verticalRotation = 0f;

    private bool usingCamera = true;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            usingCamera = false;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame && !usingCamera)
        {
            usingCamera = true;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (!usingCamera)
        {
            return;
        }

        Vector2 mouseMovement = Mouse.current.delta.ReadValue();

        float x = mouseMovement.x * sensitivity;
        float y = mouseMovement.y * sensitivity;

        verticalRotation -= y;
        verticalRotation = Mathf.Clamp(verticalRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);

        _ArcadePlayer.Rotate(Vector3.up * x);
    }
}