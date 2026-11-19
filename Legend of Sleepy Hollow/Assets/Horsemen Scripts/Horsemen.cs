using UnityEngine;
using Unity.Netcode;
using System;

public class Horsemen : MonoBehaviour
{

    [SerializeField]
    private float speed = 10f;
    private float direction = -1f;
    [SerializeField] bool move = false;


    public event EventDelegate OnKillevent;
    public delegate void EventDelegate(ulong clientID);

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent<Vehicle>(out Vehicle vehicle))
        {
            print("got hit");
           ulong clientID = vehicle.GetuID();

            OnKillevent?.Invoke(clientID);



        }

    }



    void Update()
    {
        if (move)
        {
            Vector3 newPos = transform.position += new Vector3(0, 0, direction) * speed * Time.deltaTime;
            transform.position = newPos;
        }
    }


}
