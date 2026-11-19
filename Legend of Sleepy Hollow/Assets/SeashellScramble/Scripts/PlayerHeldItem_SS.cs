using UnityEngine;
using System.Collections.Generic;
using System;
using Unity.Netcode;
using UnityEditor.Experimental.GraphView;


public class PlayerHeldItem_SS : NetworkBehaviour
{
    [SerializeField] Transform transformHandHeld;
    [SerializeField] Transform transformCartHeld;
    // [SerializeField] int speed = 5;

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
            DropItem();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        
        _heldObjectType.OnValueChanged -= HandleObjectTypeChanged;
    }

     
    public void SetHeldItem(ObjectType_SS objectType)
    {
        if (!IsServer) return;

        Debug.Log("set held item");

        _heldObjectType.Value = objectType;
        Debug.Log(_heldObjectType.Value);
                  
    }


    public void DropItem()
    {
        if (!IsOwner) return;
        
        Debug.Log("drop held item");
        Debug.Log(_heldObjectType.Value);

        RequestDropRpc();
    }

    void DropShell()
    {
        Debug.Log("Drop shell");
        if (_heldObjectType.Value == ObjectType_SS.None) return;

        ItemCatalogEntry matchingEntry = _itemCatalog.Find(item => item.type == _heldObjectType.Value);

        NetworkObject.InstantiateAndSpawn(matchingEntry.prefab.gameObject, NetworkManager, position: transformHandHeld.position);
    
        ClearItem();
    }

    void DropCart()
    {
        Debug.Log("Drop cart");
        if (_heldObjectType.Value == ObjectType_SS.None) return;

        ItemCatalogEntry matchingEntry = _itemCatalog.Find(item => item.type == _heldObjectType.Value);

        NetworkObject.InstantiateAndSpawn(matchingEntry.prefab.gameObject, NetworkManager, position: transformCartHeld.position);
    
        ClearItem();
    }
    

    void ClearItem()
    {
        if (!IsServer) return;

        _heldObjectType.Value = ObjectType_SS.None;
    }



    void HandleObjectTypeChanged(ObjectType_SS previousValue, ObjectType_SS newValue)
    {
        //show only the held model matching newValue.
        Debug.Log("object changed");
        foreach (var item in _itemCatalog)
            item.model.SetActive(item.type == newValue);
    }

    public ObjectType_SS ReturnHeldObject()
    {
        Debug.Log(_heldObjectType.Value);
        return _heldObjectType.Value;
    }


    [Rpc(SendTo.Server)]
    void RequestDropRpc()
    {
        Debug.Log("requesting rpc drop");

        if (_heldObjectType.Value == ObjectType_SS.None) return;

        if (_heldObjectType.Value != ObjectType_SS.Cart)
            DropShell();

        if (_heldObjectType.Value == ObjectType_SS.Cart)
            DropCart();
    }
    
}
