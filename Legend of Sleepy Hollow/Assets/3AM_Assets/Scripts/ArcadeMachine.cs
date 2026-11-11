using UnityEngine;

public class ArcadeMachine : MonoBehaviour
{
    [Header("ArcadeMachineProperties")]
    [SerializeField] private bool machineRepaired = false;

    public void RepairMachine(bool repaired)
    {
        machineRepaired = repaired;
    }
}