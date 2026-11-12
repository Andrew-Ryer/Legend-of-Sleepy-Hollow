using UnityEngine;

public class ForestObstacle : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("Player Has Hit An Obstacle");
        }
    }
}