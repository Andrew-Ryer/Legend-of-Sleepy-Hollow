using UnityEngine;
using Unity.Netcode;

public abstract class Interactable_SS : NetworkBehaviour
{
    public abstract bool CanInteract(ObjectType_SS heldType);

    public void ServerInteract(PlayerHeldItem_SS heldItem)
    {
        if (!IsServer) return;

        //validate CanInteract before calling the subclass behavior.
        Debug.Log($"Requesting server interact for {heldItem.ObjectType_SS}");
        Interact(heldItem);
    }

    public void ServerLetGo(PlayerHeldItem_SS heldItem)
    {
        if (!IsServer) return;

        //validate CanInteract before calling the subclass behavior.
        Debug.Log($"Requesting server let go for {heldItem.ObjectType_SS}");
        LetGo(heldItem);
    }

    protected abstract void Interact(PlayerHeldItem_SS heldItem);
    protected abstract void LetGo(PlayerHeldItem_SS heldItem);
}
