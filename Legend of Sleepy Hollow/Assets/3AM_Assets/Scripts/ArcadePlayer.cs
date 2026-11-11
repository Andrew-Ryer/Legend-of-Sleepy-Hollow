using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class ArcadePlayer : NetworkBehaviour
{
    [Header("DefaultVariables")]
    [SerializeField] private float speedMultiplier = 2f;
    private float defaultSpeed = 5f;

    private InputSystem_Actions inputSystemActions;

    private Vector2 moveInput;

    private bool runHeld = false;
    
    [Header("PlayerProperties")]
    [SerializeField] private float speed;
    [SerializeField] private float interactRange = 3f;
    
    [SerializeField] private Camera playerCam;

    private void Awake()
    {
        inputSystemActions = new InputSystem_Actions();
    }

    private void Update()
    {
        if (!IsOwner)
        {
            return;
        }

        if (runHeld)
        {
            speed = defaultSpeed * speedMultiplier;
        }
        else
        {
            speed = defaultSpeed;
        }

        Vector3 movePosition = transform.right * moveInput.x + transform.forward * moveInput.y;

        transform.position += movePosition * speed * Time.deltaTime;

        ArcadeMachine [] machines = FindObjectsByType<ArcadeMachine>(FindObjectsSortMode.None);

        foreach (ArcadeMachine machine in machines)
        {
            float repairDistance = Vector3.Distance(transform.position, machine.transform.position);

            if (repairDistance <= interactRange)
            {                
                Debug.Log($"You can fix {machine.name}.");

                break;
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        playerCam.gameObject.SetActive(IsOwner);

        if (IsOwner)
        {
            inputSystemActions.Enable();

            inputSystemActions.ArcadePlayer.Move.performed += OnMove;
            inputSystemActions.ArcadePlayer.Move.canceled += OnMove;

            inputSystemActions.ArcadePlayer.Run.performed += OnRun;
            inputSystemActions.ArcadePlayer.Run.canceled += OnRun;

            inputSystemActions.ArcadePlayer.Interact.performed += OnInteract;
        }
    }

    public override void OnNetworkDespawn()
    {
        inputSystemActions.ArcadePlayer.Move.performed -= OnMove;
        inputSystemActions.ArcadePlayer.Move.canceled -= OnMove;
        
        inputSystemActions.ArcadePlayer.Run.performed -= OnRun;
        inputSystemActions.ArcadePlayer.Run.canceled -= OnRun;
        
        inputSystemActions.ArcadePlayer.Interact.performed -= OnInteract;

        inputSystemActions.Disable();
    }

    private void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
    }

    private void OnRun(InputAction.CallbackContext ctx)
    {
        runHeld = ctx.ReadValueAsButton();
    }

    private void OnInteract(InputAction.CallbackContext ctx)
    {
        ArcadeMachine [] machines = FindObjectsByType<ArcadeMachine>(FindObjectsSortMode.None);

        foreach (ArcadeMachine machine in machines)
        {
            float repairDistance = Vector3.Distance(transform.position, machine.transform.position);

            if (repairDistance <= interactRange)
            {
                machine.RepairMachine(true);
                
                Debug.Log($"You have fixed {machine.name}.");

                break;
            }
        }
    }
}