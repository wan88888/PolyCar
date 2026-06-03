using UnityEngine;

public sealed class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField, Min(0f)] private float followDistance = 7f;
    [SerializeField, Min(0f)] private float followHeight = 3.2f;
    [SerializeField] private float lookAhead = 4f;
    [SerializeField, Min(0.01f)] private float positionSmoothTime = 0.16f;
    [SerializeField, Min(0f)] private float rotationSmoothSpeed = 9f;

    [Header("Menu Intro")]
    [SerializeField, Min(0f)] private float menuDistance = 10f;
    [SerializeField, Min(0f)] private float menuHeight = 4.4f;
    [SerializeField, Min(0f)] private float menuOrbitDegrees = 14f;
    [SerializeField, Min(0f)] private float menuOrbitSpeed = 0.35f;

    private Vector3 followVelocity;
    private bool menuMode;

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

        float deltaTime = menuMode ? Time.unscaledDeltaTime : Time.deltaTime;
        float activeDistance = menuMode ? menuDistance : followDistance;
        float activeHeight = menuMode ? menuHeight : followHeight;
        float yaw = menuMode ? Mathf.Sin(Time.unscaledTime * menuOrbitSpeed) * menuOrbitDegrees : 0f;
        Vector3 followDirection = Quaternion.Euler(0f, yaw, 0f) * target.forward;
        Vector3 desiredPosition = target.position - followDirection * activeDistance + Vector3.up * activeHeight;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref followVelocity, positionSmoothTime, Mathf.Infinity, deltaTime);

        Vector3 lookPoint = target.position + target.forward * lookAhead + Vector3.up * (menuMode ? 1.35f : 1.1f);
        Vector3 lookDirection = lookPoint - transform.position;
        if (lookDirection.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Quaternion desiredRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSmoothSpeed * deltaTime);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        followVelocity = Vector3.zero;
    }

    public void SetMenuMode(bool active)
    {
        menuMode = active;
        followVelocity = Vector3.zero;
    }
}
