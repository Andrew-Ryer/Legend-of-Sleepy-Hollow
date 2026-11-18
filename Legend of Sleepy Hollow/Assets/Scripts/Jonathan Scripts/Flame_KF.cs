using UnityEngine;
using Unity.Netcode;

//Alyssa contribution 

public class Flame_KF : InteractFire_KF
{
    [SerializeField] ObjectType_KF _objectType;
    
    public override bool CanExtinguish(ObjectType_KF _objectType)
    {
        return true;
    }
    
}

