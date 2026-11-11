using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using Unity.Netcode;


public class PlayerController_SS : NetworkBehaviour
{
    [Header("Components")]
    [SerializeField] CharacterController _characterController;
    [SerializeField] PlayerHeldItem_SS _heldItem;
    [SerializeField] GameObject _camera;

    [Header("Detection")]
    [SerializeField] float _detectionRadius = 3f;
    [SerializeField] float _detectionAngle = 60f;
    [SerializeField] LayerMask _pickupLayer;

    [Header("Movement")]
    [SerializeField] float _movementSpeed = 4f;
    [SerializeField] float _rightRotateSensitivity = 50f;
    [SerializeField] float _upRotateSensitivity = 50f;
    Vector2 _smoothedInput;
    Interactable_SS _closestTarget;
    bool prevPress;


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        Debug.Log(
            $"ownerClientId={OwnerClientId} | " +
            $"isOwner={IsOwner} | isServer={IsServer} | isClient={IsClient} | isHost={IsHost}");

        if (IsOwner)
        {
            _camera.SetActive(true);
            prevPress = false;
        }
            
            
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
    }


    void Update()
    {        
        if (!IsOwner) return;
        
        //grabs cursor's location on the screen
        Vector3 mousePos = Mouse.current.position.ReadValue();

        //cursor will be zero at center of screen 
        float upOffset = (mousePos.y - Screen.height * 0.5f) / (Screen.height * 0.5f);
        float rightOffset = (mousePos.x - Screen.width * 0.5f) / (Screen.width * 0.5f);

        //calculates rotation direction and amount based on set sensitivity
        float upRotation = upOffset * -_upRotateSensitivity;
        // float rightRotation = rightOffset * _rightRotateSensitivity;
        
        //only camera rotates while the player stays upright 
        _camera.transform.localRotation = Quaternion.Euler(Mathf.Clamp(upRotation, -100, 100), 0f, 0f);
        
        //plays body rotates to walk in the pointed direction
        // transform.rotation = Quaternion.Euler(0f, rightRotation, 0f);

        //get input from player
        Vector2 input = ReadMovementInput();
        _smoothedInput = Vector2.MoveTowards(_smoothedInput, input, Time.deltaTime * 10f);

        //allows player to turn directions
        // _characterController.Move(transform.right * _smoothedInput.x * _movementSpeed * Time.deltaTime); 
        float rotation = _smoothedInput.x * _rightRotateSensitivity * Time.deltaTime;
        transform.Rotate(0f, rotation, 0f);
      

        //player movement going forward
        _characterController.Move(transform.forward * _smoothedInput.y * _movementSpeed * Time.deltaTime);
            
        UpdateInteractionTarget();

        //detect a target and request interaction on E or left-click.
        if (Mouse.current.leftButton.isPressed)
            HandleInteraction(true);
        else
            HandleInteraction(false);
       
    }


    static Vector2 ReadMovementInput()
    {
        //return WASD input as a two-dimensional vector.

        Vector2 input = Vector2.zero;
        if(Keyboard.current.dKey.isPressed) input.x += 1f;
        if(Keyboard.current.aKey.isPressed) input.x -= 1f;
        if(Keyboard.current.wKey.isPressed) input.y += 1f;
        if(Keyboard.current.sKey.isPressed) input.y -= 1f;

        return input;
    }

    void HandleInteraction(bool wasPressed)
    {
        if (!IsOwner) return;

        if (prevPress == wasPressed) return;
        prevPress = wasPressed;

        //send the target's NetworkObjectId to the server.
        if (_closestTarget == null) return;
        RequestInteractRpc(_closestTarget.NetworkObjectId, wasPressed);
    }


    void UpdateInteractionTarget()
    {
        //find the closest valid Interactable in front of the player.

        Interactable_SS interactable = FindClosestValidInteractable();
        if (interactable == _closestTarget) return;

        ClearSelection();

        if (interactable != null)
        {
            // Debug.Log("found interactable");
            interactable.GetComponent<InRange_SS>().SetInRange(true);
            _closestTarget = interactable;
        }
    }

    Interactable_SS FindClosestValidInteractable()
    {
        // 1. Detect nearby objects with Physics.OverlapSphere, using
        //    _detectionRadius and _pickupLayer.
        // 2. Check each hit and keep the closest Interactable within _detectionAngle.
        //    Ignore hits without an Interactable or whose
        //    CanInteract(_heldItem.ObjectType) returns false.
        // 3. If the closest candidate is still _closestTarget, nothing changed; return.
        // 4. Otherwise, remove the old highlight, store the new candidate, and
        //    highlight it (if there is one).

        Collider[] candidates = Physics.OverlapSphere(transform.position, _detectionRadius, _pickupLayer);
        Interactable_SS closestInteractable = null;
        float closestDistanceSqr = float.MaxValue;

        foreach(Collider c in candidates)
        {
            if (!c.TryGetComponent(out Interactable_SS interactable)) continue;
            if (!interactable.CanInteract(_heldItem.ObjectType_SS)) continue;

            Vector3 directionToInteractable = interactable.transform.position - transform.position;

            float angle = Vector3.Angle(transform.forward, directionToInteractable.normalized);
            if (angle > _detectionAngle) continue;

            float distanceSqr = directionToInteractable.sqrMagnitude;
            if (distanceSqr < closestDistanceSqr)
            {
                closestInteractable = interactable;
                closestDistanceSqr = distanceSqr;
                // Debug.Log("within distance");
            }
        }
        return closestInteractable;
    }

    void ClearSelection()
    {
        if (_closestTarget != null)
            _closestTarget.GetComponent<InRange_SS>().SetInRange(false);

        _closestTarget =  null;
    }


    [Rpc(SendTo.Server)]
    void RequestInteractRpc(ulong networkObjectId, bool wasPressed)
    {
        //resolve the NetworkObject id and invoke its server gateway.
        //target may have despawned after the owner selected it.
        Debug.Log($"Requesting interact with rpc {networkObjectId}");
        Dictionary<ulong, NetworkObject> spawnedObjectMap = NetworkManager.SpawnManager.SpawnedObjects;
        if (!spawnedObjectMap.TryGetValue(networkObjectId, out NetworkObject spawnedObject))
        {
            Debug.LogError($"Couldn't find id: {NetworkObjectId}");
            return;
        }

        if (!spawnedObject.TryGetComponent(out Interactable_SS interactable))
        {
            Debug.LogError($"Object doesn't have interactable");
            return;
        }

        if (interactable.CanInteract(_heldItem.ObjectType_SS) && wasPressed)
        {
            Debug.Log("pick up");
            interactable.ServerInteract(_heldItem);
        }
            

        if (interactable.CanInteract(_heldItem.ObjectType_SS) && !wasPressed)
        {
            Debug.Log("let go");
            interactable.ServerLetGo(_heldItem);
        }
            
    }

}
