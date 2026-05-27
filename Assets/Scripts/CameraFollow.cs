using UnityEngine;

public sealed class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField, Min(0f)] private float followDistance = 7f;
    [SerializeField, Min(0f)] private float followHeight = 3.2f;
    [SerializeField] private float lookAhead = 4f;
    [SerializeField, Min(0.01f)] private float positionSmoothTime = 0.16f;
    [SerializeField, Min(0f)] private float rotationSmoothSpeed = 9f;

    private Vector3 followVelocity;

    private void Start()
    {
        if (target == null)
        {
            CarController car = FindFirstObjectByType<CarController>();
            if (car != null)
            {
                target = car.transform;
            }
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desiredPosition = target.position - target.forward * followDistance + Vector3.up * followHeight;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref followVelocity, positionSmoothTime);

        Vector3 lookPoint = target.position + target.forward * lookAhead + Vector3.up * 1.1f;
        Vector3 lookDirection = lookPoint - transform.position;
        if (lookDirection.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Quaternion desiredRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSmoothSpeed * Time.deltaTime);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        followVelocity = Vector3.zero;
    }
}
