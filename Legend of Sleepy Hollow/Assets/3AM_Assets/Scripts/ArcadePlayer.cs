using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class ArcadePlayer : NetworkBehaviour
{
    [Header("DefaultVariables")]
    [SerializeField] private float speedMultiplier = 2f;
    [SerializeField] private float speedReduction = 2f;
    private float defaultSpeed = 5f;

    private InputSystem_Actions inputSystemActions;

    private Vector2 moveInput;

    private bool runHeld = false;
    private bool sneakHeld = false;
    private bool flashlightActive = false;
    
    [Header("PlayerProperties")]
    [SerializeField] private float speed;
    [SerializeField] private float interactRange = 3f;
    
    [SerializeField] private Camera playerCam;

    [SerializeField] private Light flashlight;

    private void Awake()
    {
        inputSystemActions = new InputSystem_Actions();

        flashlight.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!IsOwner)
        {
            return;
        }
        
        if (sneakHeld)
        {
            speed = defaultSpeed / speedReduction;
        }
        else if (runHeld)
        {
            speed = defaultSpeed * speedMultiplier;
        }
        else
        {
            speed = defaultSpeed;
        }
        
        flashlight.gameObject.SetActive(flashlightActive);

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

            inputSystemActions.ArcadePlayer.Sneak.performed += OnSneak;
            inputSystemActions.ArcadePlayer.Sneak.canceled += OnSneak;

            inputSystemActions.ArcadePlayer.Flash.performed += OnFlash;
        }
    }

    public override void OnNetworkDespawn()
    {
        inputSystemActions.ArcadePlayer.Move.performed -= OnMove;
        inputSystemActions.ArcadePlayer.Move.canceled -= OnMove;
        
        inputSystemActions.ArcadePlayer.Run.performed -= OnRun;
        inputSystemActions.ArcadePlayer.Run.canceled -= OnRun;
        
        inputSystemActions.ArcadePlayer.Interact.performed -= OnInteract;

        inputSystemActions.ArcadePlayer.Sneak.performed -= OnSneak;
        inputSystemActions.ArcadePlayer.Sneak.canceled -= OnSneak;

        inputSystemActions.ArcadePlayer.Flash.performed -= OnFlash;

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

    private void OnSneak(InputAction.CallbackContext ctx)
    {
        sneakHeld = ctx.ReadValueAsButton();
    }
    
    private void OnFlash(InputAction.CallbackContext ctx)
    {
        flashlightActive = !flashlightActive;
    }
}