using UnityEngine;

public class DroneAI : MonoBehaviour
{
    public Transform player;
    public float minHeight;
    public float speed;
    private float currentSpeed;
    public float stoppingDistance;
    public float followDelay = 1f;   // seconds to wait before following
    public float collisionRadius;
    private float targetHeight;
    private float delayTimer;

    private void Update()
    {
        SetHeight();
        transform.LookAt(player.position);
        Vector3 targetPosition = player.position;
        targetPosition.y = targetHeight;

        float distance = Vector3.Distance(transform.position, targetPosition);
        CheckCollision();
        if (distance > stoppingDistance)
        {
            // Player is out of range, so wait before chasing
            delayTimer += Time.deltaTime;

            if (delayTimer >= followDelay)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position, targetPosition, currentSpeed * Time.deltaTime);
            }
        }
        else
        {
            // Close enough, so reset the delay for next time
            delayTimer = 0f;
        }
    }

    private void SetHeight()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit))
        {
            targetHeight = hit.point.y + minHeight;
        }
    }

    private void CheckCollision()
    {

        if(Physics.CheckSphere(transform.position, collisionRadius)){
            currentSpeed = 0f;
            transform.position += new Vector3(0f, speed * Time.deltaTime, 0f);
        }
        else
        {
            currentSpeed = speed;
        }
    }
}