using UnityEngine;
using Unity.Netcode;

public class ShellScoring_SS : NetworkBehaviour
{
    // public int shellPoints;
    public int totalPoints;


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
    }
    
    public void OnCollision(PlayerHeldItem_SS _heldItem)
    {
        Debug.Log(_heldItem.ObjectType_SS);
        
        if (_heldItem.ObjectType_SS == ObjectType_SS.SandDollar)
        {
            totalPoints += 50;
        }

        if (_heldItem.ObjectType_SS == ObjectType_SS.Scallop)
        {
            totalPoints += 35;
        }

        if (_heldItem.ObjectType_SS == ObjectType_SS.Sundial)
        {
            totalPoints += 20;
        }

        if (_heldItem.ObjectType_SS == ObjectType_SS.Clam)
        {
            totalPoints += 10;
        }

        Debug.Log(totalPoints);
    }
}
