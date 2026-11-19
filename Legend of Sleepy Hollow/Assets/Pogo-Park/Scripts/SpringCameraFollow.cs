using Unity.Netcode;
using UnityEngine;

/*
 * FollowCamera holds the camera at a fixed offset from a target. The offset is
 * set in the inspector; only the owning player moves its local camera.
 *
 * Updated to now be able to zoom in the camera, will be updated even later
 */
public class SpringCameraFollow : NetworkBehaviour
{
    [SerializeField, Range(0f, 100f)]
    float _zoom = 61f;
    [SerializeField, Range(0f, 360f)]
    float _orbitYawDegrees;

    const float FarthestDistance = 10f;
    const float ClosestDistance = 2f;

    Camera _camera;
    Vector3 _planePosition;
    Vector3 _cameraPosition;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsOwner)
            return;

        _camera = Camera.main;
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && _camera != null)
        {
            // camera.transform.position = originPosition;
        }

        base.OnNetworkDespawn();
    }

    void LateUpdate()
    {
        if (!IsOwner || _camera == null)
            return;

        _planePosition = transform.position;
        float distance = Mathf.Lerp(FarthestDistance, ClosestDistance, Mathf.Clamp01(_zoom / 100f));
        Vector3 defaultOffsetDirection = new Vector3(-5f, 1f, 0f).normalized;
        Vector3 offsetDirection = Quaternion.AngleAxis(_orbitYawDegrees, Vector3.up) * defaultOffsetDirection;
        _cameraPosition = _planePosition + offsetDirection * distance;
        _camera.transform.position = _cameraPosition;
        Vector3 lookDirection = _planePosition - _cameraPosition;
        _camera.transform.rotation = Quaternion.LookRotation(lookDirection);
    }
}
