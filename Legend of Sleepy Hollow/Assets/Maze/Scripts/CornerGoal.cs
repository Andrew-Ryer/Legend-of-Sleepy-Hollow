using UnityEngine;

public class CornerGoal : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start() { }

    // Update is called once per frame
    void Update() { }

    //TODO: Add functionality to detect when the player reaches a corner and mark it as reached. The last corner reached has to be the corner that is their spawn point.
    // There are triggers on each of the corner spawn points.
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Mark the corner as reached for the player
            Debug.Log($"Player {other.name} reached corner {gameObject.name}");
            // You can add additional logic here to handle the corner being reached, such as updating a player's state or triggering an event.
        }
    }
}
