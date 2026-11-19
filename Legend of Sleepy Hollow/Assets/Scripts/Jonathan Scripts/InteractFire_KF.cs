using UnityEngine;
using Unity.Netcode;

//Alyssa contribution

public abstract class InteractFire_KF : NetworkBehaviour
{
    public abstract bool CanExtinguish(ObjectType_KF aimedTarget);

}