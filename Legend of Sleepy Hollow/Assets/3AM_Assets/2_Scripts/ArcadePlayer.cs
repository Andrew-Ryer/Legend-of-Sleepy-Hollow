using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class ArcadePlayer : NetworkBehaviour
{
    [Header("Player Properties")]
    [SerializeField] private Camera playerCamera;

    [SerializeField] private Light playerFlashlight;

    [SerializeField] private float speed;
    private float arcadeMachineInteractionRange = 3f;

    [Header("Other Variables")]
    private InputSystem_Actions inputSystemActions;

    private ArcadeGameManager gameManager;

    private Vector2 moveInput;

    private NetworkVariable<bool> flashlightActive = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private float defaultSpeed = 5f;

    private float messageTimer = 0f;
    private float flashlightMessageTiemr = 0f;

    private int flashlightLife = 15;

    private bool runHeld = false;
    private bool sneakHeld = false;

    public override void OnNetworkSpawn()
    {
        playerCamera.gameObject.SetActive(IsOwner);

        flashlightActive.OnValueChanged += FlashlightUpdate;
        playerFlashlight.gameObject.SetActive(flashlightActive.Value);

        if (!IsOwner)
        {
            return;
        }

        gameManager = FindAnyObjectByType<ArcadeGameManager>();

        gameManager.flashlightLifeTMP.gameObject.SetActive(true);
        gameManager.flashlightLifeTMP.text = $"Flashlight: {flashlightLife}";

        gameManager.flashlightLifeDeadTMP.gameObject.SetActive(false);

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

    public override void OnNetworkDespawn()
    {
        flashlightActive.OnValueChanged -= FlashlightUpdate;

        if (!IsOwner)
        {
            return;
        }

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

    private void Awake()
    {
        inputSystemActions = new InputSystem_Actions();

        playerFlashlight.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!IsOwner)
        {
            return;
        }

        if (messageTimer > 0f)
        {
            messageTimer -= Time.deltaTime;

            if (messageTimer <= 0f)
            {
                gameManager.fixedArcadeMachineTMP.gameObject.SetActive(false);
                gameManager.arcadeMachineAlreadyFixedTMP.gameObject.SetActive(false);
            }
        }

        if (flashlightMessageTiemr > 0f)
        {
            flashlightMessageTiemr -= Time.deltaTime;

            if (flashlightMessageTiemr <= 0f)
            {
                gameManager.flashlightLifeDeadTMP.gameObject.SetActive(false);
            }
        }

        if (gameManager.GameEnded)
        {
            gameManager.availableArcadeMachineTMP.gameObject.SetActive(false);

            return;
        }

        if (sneakHeld)
        {
            speed = defaultSpeed / 2;
        }
        else if (runHeld)
        {
            speed = defaultSpeed * 2;
        }
        else
        {
            speed = defaultSpeed;
        }

        Vector3 movePosition = transform.right * moveInput.x + transform.forward * moveInput.y;

        transform.position += movePosition * speed * Time.deltaTime;

        ArcadeMachine [] machines = FindObjectsByType<ArcadeMachine>(FindObjectsSortMode.None);

        bool nearMachine = false;

        foreach (ArcadeMachine machine in machines)
        {
            float repairDistance = Vector3.Distance(transform.position, machine.transform.position);

            if (repairDistance <= arcadeMachineInteractionRange && !machine.alreadyFixed)
            {
                nearMachine = true;

                break;
            }
        }

        gameManager.availableArcadeMachineTMP.gameObject.SetActive(nearMachine);
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
        if (!gameManager.GameStarted || gameManager.GameEnded)
        {
            return;
        }

        ArcadeMachine [] machines = FindObjectsByType<ArcadeMachine>(FindObjectsSortMode.None);

        ArcadeMachine closestMachine = null;
        float closestDistance = arcadeMachineInteractionRange;

        foreach (ArcadeMachine machine in machines)
        {
            float repairDistance = Vector3.Distance(transform.position, machine.transform.position);

            if (repairDistance <= closestDistance)
            {
                closestMachine = machine;

                closestDistance = repairDistance;
            }
        }

        if (closestMachine == null)
        {
            return;
        }

        gameManager.fixedArcadeMachineTMP.gameObject.SetActive(false);
        gameManager.arcadeMachineAlreadyFixedTMP.gameObject.SetActive(false);

        if (!closestMachine.alreadyFixed)
        {
            RepairArcadeMachineRpc(closestMachine.NetworkObjectId);

            gameManager.fixedArcadeMachineTMP.gameObject.SetActive(true);
        }
        else
        {
            gameManager.arcadeMachineAlreadyFixedTMP.gameObject.SetActive(true);
        }

        messageTimer = 3f;
    }

    [Rpc(SendTo.Server)]
    private void RepairArcadeMachineRpc(ulong machineId)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(machineId, out NetworkObject machineObject))
        {
            return;
        }

        ArcadeMachine machine = machineObject.GetComponent<ArcadeMachine>();

        if (machine == null)
        {
            return;
        }

        ArcadeGameManager gameManager = FindAnyObjectByType<ArcadeGameManager>();

        if (!gameManager.GameStarted || gameManager.GameEnded)
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, machine.transform.position);

        if (distance > arcadeMachineInteractionRange)
        {
            return;
        }

        machine.RepairMachine();
    }

    private void OnSneak(InputAction.CallbackContext ctx)
    {
        sneakHeld = ctx.ReadValueAsButton();
    }

    private void OnFlash(InputAction.CallbackContext ctx)
    {
        if (gameManager.GameEnded)
        {
            return;
        }

        if (flashlightLife <= 0)
        {
            gameManager.flashlightLifeDeadTMP.gameObject.SetActive(true);

            flashlightMessageTiemr = 3f;

            return;
        }

        flashlightActive.Value = !flashlightActive.Value;

        if (flashlightActive.Value)
        {
            flashlightLife--;

            gameManager.flashlightLifeTMP.text = $"Flashlight: {flashlightLife}";

            if (flashlightLife == 0)
            {
                flashlightActive.Value = false;

                gameManager.flashlightLifeDeadTMP.gameObject.SetActive(true);

                flashlightMessageTiemr = 3f;
            }
        }
    }

    private void FlashlightUpdate(bool oldValue, bool newValue)
    {
        playerFlashlight.gameObject.SetActive(newValue);
    }
}