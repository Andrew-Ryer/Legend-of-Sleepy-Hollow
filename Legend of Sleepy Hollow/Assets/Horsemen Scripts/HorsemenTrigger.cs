using UnityEngine;
using System.Collections.Generic;

public class HorsemenTrigger : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private List<GameObject> paths = new List<GameObject>();
    [SerializeField] private float offset = 70f;
    [SerializeField] private float destroyTriggerOffset;
    private bool alreadyTriggered = false;

    private void Awake()
    {
        
    }
    void Start()
    {
        

    }


    private void OnTriggerEnter(Collider other)
    {

        if(other.gameObject.TryGetComponent<Vehicle>(out Vehicle vehicle) && alreadyTriggered == false)
        {
            int randomIndex = Random.Range(0, paths.Count);
            GameObject randomPath = paths[randomIndex];
            Instantiate(randomPath, transform.parent.position + (new Vector3(0f,0f,-offset)), Quaternion.identity);
            Debug.Log(randomIndex);
            alreadyTriggered = true;
        }
    }
}
