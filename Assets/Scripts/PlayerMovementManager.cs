using UnityEngine;

public class PlayerMovementManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Vector2 moveVector;
    public bool sprinting;
    public bool jump;

    private void Awake()
    {
        PlayerInputManager.OnMoveInput += UpdateMoveVector;
        PlayerInputManager.OnJumpInput += Jump;
        PlayerInputManager.OnSprintInput += ToggleSprint;
    }

    private void UpdateMoveVector(Vector2 moveVector_)
    {
        moveVector = moveVector_;
    }

    private void Jump()
    {
        jump = true;
    }

    private void ToggleSprint()
    {
        sprinting = !sprinting;
    }


}
