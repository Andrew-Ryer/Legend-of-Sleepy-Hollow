using UnityEngine;
using Unity.Netcode;

public class Camera : NetworkBehaviour
{
    [SerializeField]
    private float direction = -1f;
    [SerializeField]
    private float speed = 10f;
    
  
    

    
    void Update()
    {
        if (!IsClient) { return; }

       
        
            Vector3 newPos = transform.position += new Vector3(0, 0, direction) * speed * Time.deltaTime;
            transform.position = newPos;
        
    }
}
