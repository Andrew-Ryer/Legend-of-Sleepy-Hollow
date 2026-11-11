using UnityEngine;
using Unity.Netcode;

public class ItemPickup_SS : Interactable_SS
{
    [SerializeField] ObjectType_SS _objectType;
    private NetworkObject networkObject;
    private Rigidbody rb;


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        networkObject = GetComponent<NetworkObject>();
        rb = GetComponent<Rigidbody>();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        //despawn leaves the GameObject active and visible
        if (NetworkObject.InScenePlaced && !NetworkManager.ShutdownInProgress)
            gameObject.SetActive(false);
    }

    public override bool CanInteract(ObjectType_SS heldType)
    {
        return true;
    }

    protected override void Interact(PlayerHeldItem_SS heldItem)
    {
        Debug.Log("interacting");

        heldItem.DropItem(transform.position);
        
        heldItem.SetHeldItem(_objectType, networkObject, rb);
    }

    protected override void LetGo(PlayerHeldItem_SS heldItem)
    {
        Debug.Log("let go");

        heldItem.DropItem(transform.position);
    }

}
