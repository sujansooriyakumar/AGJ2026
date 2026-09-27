using Unity.Cinemachine;
using UnityEngine;

public class DroneAI : MonoBehaviour
{
    public Transform player;
    public float minHeight;
    public float speed;
    public float currentSpeed;
    public float stoppingDistance;
    public float followDelay = 1f;   
    public float collisionRadius;
    public LayerMask obstacleMask;   
    private float targetHeight;
    private float delayTimer;

    [Header("Rotation")]
    public float rotationSpeed = 180f;  
    public float maxTiltUp = 20f;       
    public float maxTiltDown = 45f;     

    [Header("Detection")]
    public float detectionRadius;
    public float detectionRate = 1.0f;
    public float detectionMeter = 0.0f;

    [Header("Patrol")]
    public float patrolRadius;
    public float patrolPointTolerance = 0.5f; 
    private Vector3 patrolCenter;
    private Vector3 patrolPoint;
    private Vector3 targetPosition;
    public bool playerSpotted;


    private void Start()
    {
        patrolCenter = transform.position;
        patrolPoint = GetRandomPatrolPoint();
    }

    private void Update()
    {
        SetHeight();
        PlayerCheck();
        CheckCollision();
        currentSpeed = speed * (1 + (detectionMeter / 100f));
        if (playerSpotted)
        {
            ChasePlayer();
            detectionMeter += detectionRate * Time.deltaTime;
            detectionMeter = Mathf.Clamp(detectionMeter, 0f, 100f);
        }
        else
        {
            Patrol();
            detectionMeter -= detectionRate * Time.deltaTime;
            detectionMeter = Mathf.Clamp(detectionMeter, 0f, 100f);
        }
    }

    private void ChasePlayer()
    {
        targetPosition = player.position;
        targetPosition.y = targetHeight;
        RotateTowards(player.position);

        float distance = Vector3.Distance(transform.position, targetPosition);

        if (distance > stoppingDistance)
        {
            delayTimer += Time.deltaTime;

            if (delayTimer >= followDelay)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position, targetPosition, currentSpeed * Time.deltaTime);
            }
        }
        else
        {
            delayTimer = 0f;
        }
    }

    private void Patrol()
    {
        targetPosition = patrolPoint;
        targetPosition.y = targetHeight;

        Vector3 flatDrone = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 flatTarget = new Vector3(targetPosition.x, 0f, targetPosition.z);

        if (Vector3.Distance(flatDrone, flatTarget) <= patrolPointTolerance)
        {
            patrolPoint = GetRandomPatrolPoint();
            return;
        }

        RotateTowards(targetPosition);
        transform.position = Vector3.MoveTowards(
            transform.position, targetPosition, currentSpeed * Time.deltaTime);
    }

    private void RotateTowards(Vector3 lookTarget)
    {
        Vector3 direction = lookTarget - transform.position;
        if (direction.sqrMagnitude < 0.001f) return; 

        float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;


        float horizontalDistance = new Vector2(direction.x, direction.z).magnitude;
        float pitch = -Mathf.Atan2(direction.y, horizontalDistance) * Mathf.Rad2Deg;

        pitch = Mathf.Clamp(pitch, -maxTiltUp, maxTiltDown);

        Quaternion targetRotation = Quaternion.Euler(pitch, yaw, 0f);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private Vector3 GetRandomPatrolPoint()
    {
        Vector2 randomCirclePoint = Random.insideUnitCircle * patrolRadius;

        return new Vector3(
            patrolCenter.x + randomCirclePoint.x,
            patrolCenter.y,
            patrolCenter.z + randomCirclePoint.y);
    }

    private void PlayerCheck()
    {
        bool inRange = Vector3.Distance(transform.position, player.position) <= detectionRadius;

        // player just left range; go back to patrolling
        if (playerSpotted && !inRange)
        {
            patrolPoint = GetRandomPatrolPoint();
            delayTimer = 0f;
        }

        playerSpotted = inRange;
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
        if (Physics.CheckSphere(transform.position, collisionRadius, obstacleMask))
        {
            currentSpeed = 0f;
            transform.position += new Vector3(0f, speed * Time.deltaTime, 0f);
        }
        else
        {
            currentSpeed = speed;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(Application.isPlaying ? patrolCenter : transform.position, patrolRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, collisionRadius);
    }
}