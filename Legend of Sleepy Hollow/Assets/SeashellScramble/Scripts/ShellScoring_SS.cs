using UnityEngine;
using Unity.Netcode;

public class ShellScoring_SS : NetworkBehaviour
{
    // public int shellPoints;
    // NetworkVariable<int> totalPoints = new NetworkVariable<int>();
    int totalPoints = 0;
    // NetworkVariable<int> addedPoints = new NetworkVariable<int>();
    // [SerializeField] ItemPickup_SS item;


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // totalPoints.OnValueChanged += HandlePointsChanged;
    }

    public override void OnNetworkDespawn()
    {
        // totalPoints.OnValueChanged -= HandlePointsChanged;
        
        base.OnNetworkDespawn();
    }
    
    public void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;
        
        NetworkObject netObj = other.GetComponentInParent<NetworkObject>();

        if (!netObj.TryGetComponent(out ItemPickup_SS item)) return;
        
        Debug.Log(item.ReturnObjectType());
        
        if (item.ReturnObjectType() == ObjectType_SS.SandDollar)
        {
            totalPoints += 50;
            // totalPoints.Value += 50;
        }

        if (item.ReturnObjectType() == ObjectType_SS.Scallop)
        {
            totalPoints += 35;
            // totalPoints.Value += 35;
        }

        if (item.ReturnObjectType() == ObjectType_SS.Sundial)
        {
            totalPoints += 20;
            // totalPoints.Value += 20;
        }

        if (item.ReturnObjectType() == ObjectType_SS.Clam)
        {
            totalPoints += 10;
            // totalPoints.Value += 10;
        }

        Debug.Log(totalPoints);
        // Debug.Log(totalPoints.Value);
    }

    // public void OnTriggerExit(Collider other)
    // {
    //     NetworkObject netObj = other.GetComponentInParent<NetworkObject>();

    //     if (!netObj.TryGetComponent(out ItemPickup_SS item)) return;
        
    //     Debug.Log(item.ReturnObjectType());
        
    //     if (item.ReturnObjectType() == ObjectType_SS.SandDollar)
    //     {
    //         totalPoints -= 50;
    //         // totalPoints.Value += 50;
    //     }

    //     if (item.ReturnObjectType() == ObjectType_SS.Scallop)
    //     {
    //         totalPoints -= 35;
    //         // totalPoints.Value += 35;
    //     }

    //     if (item.ReturnObjectType() == ObjectType_SS.Sundial)
    //     {
    //         totalPoints -= 20;
    //         // totalPoints.Value += 20;
    //     }

    //     if (item.ReturnObjectType() == ObjectType_SS.Clam)
    //     {
    //         totalPoints -= 10;
    //         // totalPoints.Value += 10;
    //     }

    //     Debug.Log(totalPoints);
    //     // Debug.Log(totalPoints.Value);
    // }
    
    // void HandlePointsChanged(NetworkVariable<int> prevNum, int addedNum)
    // {
    //     totalPoints.Value = prevNum + addedNum;
    // }
}
