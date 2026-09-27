using UnityEngine;


public class FollowObject : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Axes to Follow")]
    [SerializeField] private bool followX = true;
    [SerializeField] private bool followY = true;
    [SerializeField] private bool followZ = true;

    private Vector3 lastTargetPosition;
    private Transform trackedTarget;

    public Transform Target
    {
        get => target;
        set
        {
            target = value;
            ResetTracking();
        }
    }

    public bool FollowX { get => followX; set => followX = value; }
    public bool FollowY { get => followY; set => followY = value; }
    public bool FollowZ { get => followZ; set => followZ = value; }

    private void OnEnable()
    {
        ResetTracking();
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            trackedTarget = null;
            return;
        }

        if (target != trackedTarget)
        {
            ResetTracking();
            return;
        }

        Vector3 currentTargetPosition = target.position;
        Vector3 delta = currentTargetPosition - lastTargetPosition;

        if (!followX) delta.x = 0f;
        if (!followY) delta.y = 0f;
        if (!followZ) delta.z = 0f;

        transform.position += delta;
        lastTargetPosition = currentTargetPosition;
    }


    public void ResetTracking()
    {
        trackedTarget = target;
        if (target != null)
        {
            lastTargetPosition = target.position;
        }
    }
}