using System.Collections.Generic;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

/*
 * FollowCamera holds the camera at a fixed offset from a target. The offset is
 * set in the inspector; only the owning player moves its local camera.
 *
 * Updated to now be able to zoom in the camera, will be updated even later.
 * Updated to now instead be hooked to a cinemachine camera that keeps all players in view.
 */
public class SpringCameraFollow : MonoBehaviour
{
    [SerializeField] CinemachineCamera _cinemachineCamera;
    [SerializeField] CinemachineTargetGroup _targetGroup;
    [SerializeField] Transform _finishLine;
    [SerializeField] float _playerWeight = 1f;
    [SerializeField] float _playerRadius = 1f;
    [SerializeField] float _finishLineWeight = 1f;
    [SerializeField] float _finishLineRadius = 1f;

    readonly HashSet<Transform> _trackedPlayers = new();
    readonly HashSet<Transform> _playersThisFrame = new();
    readonly List<Transform> _playersToRemove = new();

    void Awake()
    {
        if (_cinemachineCamera == null)
            _cinemachineCamera = GetComponent<CinemachineCamera>();

        if (_targetGroup == null)
            _targetGroup = GetComponent<CinemachineTargetGroup>();

        if (_targetGroup == null)
        {
            Debug.LogError("SpringCameraFollow needs a Cinemachine Target Group.", this);
            enabled = false;
            return;
        }

        if (_cinemachineCamera == null)
        {
            Debug.LogError("SpringCameraFollow needs a Cinemachine Camera.", this);
            enabled = false;
            return;
        }

        _cinemachineCamera.Follow = _targetGroup.transform;
        _cinemachineCamera.LookAt = _targetGroup.transform;

        if (_finishLine != null && _targetGroup.FindMember(_finishLine) < 0)
            _targetGroup.AddMember(_finishLine, _finishLineWeight, _finishLineRadius);
    }

    void Update()
    {
        _playersThisFrame.Clear();

        NetworkManager networkManager = NetworkManager.Singleton;

        if (networkManager != null && networkManager.IsListening)
        {
            foreach (NetworkObject networkObject in networkManager.SpawnManager.SpawnedObjectsList)
            {
                if (networkObject == null ||
                    !networkObject.IsPlayerObject ||
                    !networkObject.TryGetComponent(out SpringPlayerMovementInput player) ||
                    player.HasFinished)
                {
                    continue;
                }

                Transform playerTransform = networkObject.transform;
                _playersThisFrame.Add(playerTransform);

                if (_trackedPlayers.Add(playerTransform) &&
                    _targetGroup.FindMember(playerTransform) < 0)
                {
                    _targetGroup.AddMember(
                        playerTransform,
                        _playerWeight,
                        _playerRadius);
                }
            }
        }

        _playersToRemove.Clear();

        foreach (Transform playerTransform in _trackedPlayers)
        {
            if (playerTransform == null ||
                !_playersThisFrame.Contains(playerTransform))
            {
                _playersToRemove.Add(playerTransform);
            }
        }

        foreach (Transform playerTransform in _playersToRemove)
        {
            if (playerTransform != null)
                _targetGroup.RemoveMember(playerTransform);

            _trackedPlayers.Remove(playerTransform);
        }
    }
}