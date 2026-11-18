using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

//Alyssa contribution
public class WaterGun_KF : NetworkBehaviour
{
    [Header("Scripting")]
    InteractFire_KF _closestTarget;
    
    [Header("Detection")]
    [SerializeField] float _detectionRadius = 2f;
    [SerializeField] float _detectionAngle = 40f;
    [SerializeField] LayerMask _pickupLayer;
    [SerializeField] public KitchenTimer fireLeft;
    //[SerializeField] public int playerScore = 0;
    public NetworkVariable<int> playerScore = new NetworkVariable<int>();

    [Header("Misc.")]
    [SerializeField] ParticleSystem waterParticles;
    //bool isShooting;
    public override void OnNetworkSpawn()
    {
        fireLeft = FindFirstObjectByType<KitchenTimer>();
    }
    void Update()
    {
        UpdateFlamesTarget();
    }
    
    public void Shoot()
    {
        //makes sure only owner can shoot
        if (!IsOwner) return;
        //Debug.Log("shoot");
        //isShooting = true;

        PlayParticlesRpc();

        if (_closestTarget == null) return;

        if (!_closestTarget.CanExtinguish(ObjectType_KF.Flame)) return;

        if (!_closestTarget.NetworkObject.IsSpawned)
        {
            _closestTarget = null;
            //StopShoot();
            return;
        }
        ExtinguishFireRpc(_closestTarget.NetworkObject);
        _closestTarget = null;
        //StopShoot();
    }

    [Rpc(SendTo.Server)]
    void ExtinguishFireRpc(NetworkObjectReference fireReference, RpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != OwnerClientId)
        {
            return;
        }

        if (!fireReference.TryGet(out NetworkObject fire))
            return;

        fire.Despawn();
        fireLeft.fireCount.Value--;
        playerScore.Value++;
        Debug.Log("Fires Left: " + fireLeft.fireCount.Value);
        Debug.Log("Score: " + playerScore.Value);
    }


    public void StopShoot()
    {
        //isShooting = false;
        //stops water particles from playing 
        StopParticlesRpc();
    }
    
    void UpdateFlamesTarget()
    {
        //finds the closest flame in front of the player 
        InteractFire_KF flameInteract = FindClosestValidFlame();
        if (flameInteract == _closestTarget) return;

        ClearSelection();

        if (flameInteract != null)
        {
            // Debug.Log("found flame");
            _closestTarget = flameInteract;
        }
    }

    InteractFire_KF FindClosestValidFlame()
    {
       //detects flames that are within a certain radius, plays water particles 
       Collider[] candidates = Physics.OverlapSphere(transform.position, _detectionRadius, _pickupLayer);
        InteractFire_KF closestFlame = null;
        float closestDistanceSqr = float.MaxValue;

        foreach(Collider c in candidates)
        {
            if (!c.TryGetComponent(out InteractFire_KF flame)) continue;

            Vector3 directionToInteractable = flame.transform.position - transform.position;

            float angle = Vector3.Angle(transform.forward, directionToInteractable.normalized);
            if (angle > _detectionAngle) continue;

            float distanceSqr = directionToInteractable.sqrMagnitude;
            if (distanceSqr < closestDistanceSqr)
            {
                closestFlame = flame;
                closestDistanceSqr = distanceSqr;
                /*
                if (!waterParticles.isPlaying)
                    waterParticles.Play();
                */
                // Debug.Log("flame within distance");
            }
        }
        // Debug.Log(closestFlame);
        return closestFlame;
    }

    void ClearSelection()
    {
        //clears selection and stops water particles
        _closestTarget =  null;
        //StopShoot();
    }

    [Rpc(SendTo.ClientsAndHost)]
    void PlayParticlesRpc()
    {
        if (!waterParticles.isPlaying)
            waterParticles.Play();
    }

    [Rpc(SendTo.ClientsAndHost)]
    void StopParticlesRpc()
    {
        if (waterParticles.isPlaying)
            waterParticles.Stop();
    }

}
