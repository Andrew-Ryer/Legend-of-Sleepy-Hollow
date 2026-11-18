using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

public class ItemPickup_SS : Interactable_SS
{
    [SerializeField] ObjectType_SS _objectType;
    // private NetworkObject networkObject;
    // private NetworkTransform _networkTransform;
    // private Rigidbody rb;


    // public override void OnNetworkSpawn()
    // {
    //     base.OnNetworkSpawn();
    //     networkObject = GetComponent<NetworkObject>();
    //     // _networkTransform = GetComponent<NetworkTransform>();
    //     rb = GetComponent<Rigidbody>();
    // }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        //despawn leaves the GameObject active and visible
        if (NetworkObject.InScenePlaced && !NetworkManager.ShutdownInProgress)
            gameObject.SetActive(false);
    }

    public override bool CanInteract(ObjectType_SS heldType)
    {
        Debug.Log("can interact");
        return true;
    }

    protected override void Interact(PlayerHeldItem_SS heldItem)
    {
        Debug.Log("interacting");

        // heldItem.DropItem();
        
        // _networkTransform.enabled = false;
        // heldItem.SetHeldItem(_objectType, networkObject, rb);
        heldItem.SetHeldItem(_objectType);

        NetworkObject.Despawn(!NetworkObject.InScenePlaced);
    }

    // protected override void LetGo(PlayerHeldItem_SS heldItem)
    // {
    //     Debug.Log("let go");

    //     heldItem.DropItem();
    //     // _networkTransform.enabled = true;

    // }

}
