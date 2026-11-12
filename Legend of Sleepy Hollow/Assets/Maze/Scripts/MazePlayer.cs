using System;
using UnityEngine;

public class MazePlayer : MonoBehaviour
{
    public static MazePlayer Instance { get; private set; }


    [SerializeField]
    private float moveSpeed = 5f;

    [SerializeField]
    private GameInput gameInput;

    // [SerializeField]
    // private LayerMask countersLayerMask;

    // [SerializeField]
    // private Transform kitchenObjectHoldPoint;

    private bool isWalking;
    private Vector3 lastInteractDir;

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("There is more than one Player instance in the scene!");
        }
        Instance = this;
    }

    public void Start()
    {
        // gameInput.OnInteractAction += GameInput_OnInteractAction;
        // gameInput.OnInteractAlternateAction += GameInput_OnInteractAlternateAction;
    }

    // private void GameInput_OnInteractAction(object sender, EventArgs e)
    // {
    //     if (!GameManager.Instance.IsGamePlaying())
    //     {
    //         return;
    //     }
    //     if (selectedCounter != null)
    //     {
    //         selectedCounter.Interact(this);
    //     }
    // }

    // private void GameInput_OnInteractAlternateAction(object sender, EventArgs e)
    // {
    //     if (!GameManager.Instance.IsGamePlaying())
    //     {
    //         return;
    //     }
    //     if (selectedCounter != null)
    //     {
    //         selectedCounter.InteractAlternate(this);
    //     }
    // }

    private void Update()
    {
        HandleMovement();
        // HandleInteractions();
    }

    public bool IsWalking()
    {
        return isWalking;
    }

    // private void HandleInteractions()
    // {
    //     Vector2 inputVector = gameInput.GetMovementVectorNormalized();
    //     Vector3 moveDir = new Vector3(inputVector.x, 0f, inputVector.y);
    //     if (moveDir != Vector3.zero)
    //     {
    //         lastInteractDir = moveDir;
    //     }
    //     float interactDistance = 2f;
    //     if (
    //         Physics.Raycast(
    //             transform.position,
    //             lastInteractDir,
    //             out RaycastHit raycastHit,
    //             interactDistance,
    //             countersLayerMask
    //         )
    //     )
    //     {
    //         if (raycastHit.transform.TryGetComponent(out BaseCounter baseCounter))
    //         {
    //             if (baseCounter != selectedCounter)
    //             {
    //                 SetSelectedCounter(baseCounter);
    //             }
    //         }
    //         else
    //         {
    //             SetSelectedCounter(null);
    //         }
    //     }
    //     else
    //     {
    //         SetSelectedCounter(null);
    //     }
    // }

    private void HandleMovement()
    {
        Vector2 inputVector = gameInput.GetMovementVectorNormalized();
        Vector3 moveDir = new Vector3(inputVector.x, 0f, inputVector.y);
        float moveDistance = moveSpeed * Time.deltaTime;
        float playerRadius = 0.7f;
        float playerHeight = 2f;
        bool canMove = !Physics.CapsuleCast(
            transform.position,
            transform.position + Vector3.up * playerHeight,
            playerRadius,
            moveDir,
            moveDistance
        );

        if (!canMove)
        {
            //Attempt only X movement
            Vector3 moveDirX = new Vector3(moveDir.x, 0f, 0f).normalized;
            canMove =
                (moveDir.x < -.5f || moveDir.x > .5f)
                && !Physics.CapsuleCast(
                    transform.position,
                    transform.position + Vector3.up * playerHeight,
                    playerRadius,
                    moveDirX,
                    moveDistance
                );
            if (canMove)
            {
                moveDir = moveDirX;
            }
            else
            {
                //Attempt only Z movement
                Vector3 moveDirZ = new Vector3(0f, 0f, moveDir.z).normalized;
                canMove =
                    (moveDir.z < -0.5f || moveDir.z > 0.5f)
                    && !Physics.CapsuleCast(
                        transform.position,
                        transform.position + Vector3.up * playerHeight,
                        playerRadius,
                        moveDirZ,
                        moveDistance
                    );
                if (canMove)
                {
                    moveDir = moveDirZ;
                }
            }
        }
        if (canMove)
        {
            transform.position += moveSpeed * Time.deltaTime * moveDir;
        }
        isWalking = moveDir != Vector3.zero;
        float rotateSpeed = 10f;
        transform.forward = Vector3.Slerp(transform.forward, moveDir, Time.deltaTime * rotateSpeed);
    }
}