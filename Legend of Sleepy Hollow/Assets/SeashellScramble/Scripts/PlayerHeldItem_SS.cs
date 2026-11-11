using UnityEngine;
using System.Collections.Generic;
using System;
using Unity.Netcode;


public class PlayerHeldItem_SS : NetworkBehaviour
{
    private NetworkObject _networkObject;
    private Rigidbody _networkRb;
    [SerializeField] Transform transformHandHeld;
    [SerializeField] Transform transformCartHeld;

    [Serializable]
    public struct ItemCatalogEntry
    {
        public ObjectType_SS type;
        public GameObject model;
        public NetworkObject prefab;
    }

    [Header("Item Catalog")]
    [SerializeField] List<ItemCatalogEntry> _itemCatalog = new();

    public ObjectType_SS ObjectType_SS => _heldObjectType.Value;

    readonly NetworkVariable<ObjectType_SS> _heldObjectType = new();
    
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        _heldObjectType.OnValueChanged += HandleObjectTypeChanged;
        HandleObjectTypeChanged(ObjectType_SS.None, _heldObjectType.Value);
    }

    public override void OnNetworkPreDespawn()
    {
        base.OnNetworkPreDespawn();

        //drop the held item unless the host is shutting down
        if (!IsServer) return;
        if (!NetworkManager.ShutdownInProgress)
            DropItem(transform.position);
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        
        _heldObjectType.OnValueChanged -= HandleObjectTypeChanged;
    }

     
    public void SetHeldItem(ObjectType_SS objectType, NetworkObject child, Rigidbody rb)
    {
        if (!IsServer) return;

        Debug.Log("set held item");
        _heldObjectType.Value = objectType;
        _networkObject = child;
        _networkRb = rb;

        if (_heldObjectType.Value != ObjectType_SS.Cart)
        {
            child.transform.position = transformHandHeld.position;
            child.TrySetParent(transform, true);
            _networkRb.isKinematic = true;
        }

        else
        {
            child.transform.position = transformCartHeld.position;
            child.TrySetParent(transform, true);
            _networkRb.isKinematic = true;
        }             
    }


    public void DropItem(Vector3 position)
    {
        if (!IsServer) return;
        
        Debug.Log("drop held item");
        if (_heldObjectType.Value == ObjectType_SS.None) return;

        if (_heldObjectType.Value != ObjectType_SS.Cart)
            DropShell(_networkObject);

        if (_heldObjectType.Value == ObjectType_SS.Cart)
        {
            DropCart(_networkObject);
        }
            
    }
    
    void DropShell(NetworkObject child)
    {
        // ItemCatalogEntry matchingEntry = _itemCatalog.Find(item => item.type == _heldObjectType.Value);
        // NetworkObject.InstantiateAndSpawn(matchingEntry.prefab.gameObject, NetworkManager, position: position);

        child.TryRemoveParent(true);
        _networkRb.isKinematic = false;

        ClearItem();
    }

    void DropCart(NetworkObject child)
    {
        child.TryRemoveParent(true);
        _networkRb.isKinematic = false;
        
        ClearItem();
    }


    void ClearItem()
    {
        if (!IsServer) return;

        if (_heldObjectType.Value == ObjectType_SS.Cart)


        _heldObjectType.Value = ObjectType_SS.None;
    }



    void HandleObjectTypeChanged(ObjectType_SS previousValue, ObjectType_SS newValue)
    {
        // TODO Slice 6.8: show only the held model matching newValue.
        foreach (var item in _itemCatalog)
            item.model.SetActive(item.type == newValue);
    }
}
