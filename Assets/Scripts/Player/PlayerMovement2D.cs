using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement2D : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField]
    [Tooltip("World units per second the rigidbody will travel.")]
    private float moveSpeed = 5f;

    [Header("Input")]
    [SerializeField]
    [Tooltip("The project's Input System 'Player/Move' action (WASD + arrows).")]
    private InputActionReference moveActionReference;

    private InputAction moveAction;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;

        // Prefer the serialized reference; guard an unassigned reference safely
        // instead of falling back to a hardcoded GUID.
        if (moveActionReference != null)
            moveAction = moveActionReference.action;
        else
            Debug.LogWarning($"{nameof(PlayerMovement2D)}: {nameof(moveActionReference)} is unassigned in the Inspector. Assign the 'Player/Move' action from Assets/Settings/InputSystem_Actions.inputactions to enable movement.");
    }

    private void OnEnable()
    {
        if (moveAction != null)
            moveAction.Enable();
    }

    private void OnDisable()
    {
        if (moveAction != null)
            moveAction.Disable();
    }

    private void FixedUpdate()
    {
        if (moveAction == null || rb == null)
            return;

        Vector2 input = moveAction.ReadValue<Vector2>();
        if (input.sqrMagnitude > 1f)
            input.Normalize(); // keep diagonal movement at full speed

        rb.linearVelocity = input * moveSpeed;
    }
}