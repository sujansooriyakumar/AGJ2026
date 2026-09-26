using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputManager : MonoBehaviour
{
    public InputAction moveAction;
    public InputAction jumpAction;
    public InputAction sprintAction;

    public static event Action<Vector2> OnMoveInput;
    public static event Action OnJumpInput;
    public static event Action OnSprintInput;

    private void Awake()
    {
        moveAction.performed += OnMovePressed;
        moveAction.canceled += OnMovePressed;

        jumpAction.performed += OnJumpPressed;

        sprintAction.performed += OnSprintPressed;
        sprintAction.canceled += OnSprintPressed;
    }

    private void OnMovePressed(InputAction.CallbackContext c)
    {
        OnMoveInput?.Invoke(c.ReadValue<Vector2>());
    }

    private void OnJumpPressed(InputAction.CallbackContext c)
    {
        OnJumpInput?.Invoke();
    }

    private void OnSprintPressed(InputAction.CallbackContext c)
    {
        OnSprintInput?.Invoke();
    }
}
