using StarterAssets;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public float sprintSpeed;
    public int doubleJumpCount;
    public float dashCooldown;
    public float jumpHeight;
    private FirstPersonController firstPersonController;

    public void UpdateStats()
    {
        firstPersonController.SprintSpeed = sprintSpeed;
        firstPersonController.dashCooldown = dashCooldown;
        firstPersonController.JumpHeight = jumpHeight;
        firstPersonController.doubleJumpCount = doubleJumpCount;
    }
   
}
