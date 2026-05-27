using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Rigidbody based low-poly arcade drift controller.
/// The car object should face +Z, with the Rigidbody on the same root object.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public sealed class CarController : MonoBehaviour
{
    public enum DriveState
    {
        Normal,
        Drift
    }

    [Header("Input")]
    [SerializeField] private bool readPlayerInput = true;
    [SerializeField, Min(1f)] private float inputSmoothing = 10f;

    [Header("Motor")]
    [SerializeField, Min(0f)] private float acceleration = 18f;
    [SerializeField, Min(0f)] private float reverseAcceleration = 10f;
    [SerializeField, Min(0f)] private float maxForwardSpeed = 42f;
    [SerializeField, Min(0f)] private float maxReverseSpeed = 12f;

    [Header("Steering")]
    [SerializeField, Min(0f)] private float steeringAcceleration = 3.4f;
    [SerializeField, Min(0f)] private float fullSteerSpeed = 16f;
    [SerializeField, Range(0f, 1f)] private float lowSpeedSteerFactor = 0.35f;

    [Header("Braking")]
    [SerializeField, Min(0f)] private float brakeDeceleration = 28f;
    [SerializeField, Range(0f, 1f)] private float brakeSteerMultiplier = 0.75f;

    [Header("Drift")]
    [SerializeField, Min(0f)] private float driftStartSpeed = 12f;
    [SerializeField, Range(0f, 1f)] private float driftSteerThreshold = 0.55f;
    [SerializeField, Min(0f)] private float normalLateralGrip = 9f;
    [SerializeField, Min(0f)] private float driftLateralGrip = 2.1f;
    [SerializeField, Min(0f)] private float driftSteeringMultiplier = 1.25f;
    [SerializeField, Min(0f)] private float driftReleaseDelay = 0.35f;

    [Header("Stability")]
    [SerializeField] private Vector3 centerOfMassOffset = new Vector3(0f, -0.45f, 0f);
    [SerializeField, Min(0f)] private float downforce = 4f;

    private Rigidbody body;
    private float throttleInput;
    private float steeringInput;
    private float brakeInput;
    private bool handbrakeInput;
    private float driftHoldTimer;

    public DriveState CurrentState { get; private set; } = DriveState.Normal;
    public bool IsDrifting => CurrentState == DriveState.Drift;
    public float SpeedKmh => PlanarVelocity.magnitude * 3.6f;
    public float ForwardSpeed => Vector3.Dot(body.linearVelocity, transform.forward);

    private Vector3 PlanarVelocity => Vector3.ProjectOnPlane(body.linearVelocity, transform.up);

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.centerOfMass = centerOfMassOffset;
        body.maxAngularVelocity = 8f;
    }

    private void Update()
    {
        if (readPlayerInput)
        {
            ReadInput();
        }
    }

    private void FixedUpdate()
    {
        UpdateDriveState();
        ApplyMotor();
        ApplyBrakes();
        ApplySteering();
        ApplyLateralGrip();
        ApplyDownforce();
    }

    public void SetInput(float throttle, float steering, float brake, bool handbrake)
    {
        readPlayerInput = false;
        throttleInput = Mathf.Clamp(throttle, -1f, 1f);
        steeringInput = Mathf.Clamp(steering, -1f, 1f);
        brakeInput = Mathf.Clamp01(brake);
        handbrakeInput = handbrake;
    }

    public void EnablePlayerInput()
    {
        readPlayerInput = true;
    }

    public void ResetCar(Vector3 position, Quaternion rotation)
    {
        if (body == null)
        {
            body = GetComponent<Rigidbody>();
        }

        throttleInput = 0f;
        steeringInput = 0f;
        brakeInput = 0f;
        handbrakeInput = false;
        driftHoldTimer = 0f;
        CurrentState = DriveState.Normal;

        body.position = position;
        body.rotation = rotation;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
    }

    private void ReadInput()
    {
        float targetThrottle = 0f;
        float targetSteering = 0f;
        float targetBrake = 0f;
        bool targetHandbrake = false;

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            {
                targetThrottle += 1f;
            }

            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            {
                targetThrottle -= 1f;
            }

            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                targetSteering -= 1f;
            }

            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                targetSteering += 1f;
            }

            targetHandbrake = keyboard.spaceKey.isPressed;
            targetBrake = targetHandbrake || keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed ? 1f : 0f;
        }

        Gamepad gamepad = Gamepad.current;
        if (gamepad != null)
        {
            targetSteering = Mathf.Abs(gamepad.leftStick.x.ReadValue()) > Mathf.Abs(targetSteering)
                ? gamepad.leftStick.x.ReadValue()
                : targetSteering;
            targetThrottle += gamepad.rightTrigger.ReadValue();
            targetBrake = Mathf.Max(targetBrake, gamepad.leftTrigger.ReadValue());
            targetHandbrake |= gamepad.buttonSouth.isPressed;
        }

        throttleInput = Mathf.MoveTowards(throttleInput, Mathf.Clamp(targetThrottle, -1f, 1f), inputSmoothing * Time.deltaTime);
        steeringInput = Mathf.MoveTowards(steeringInput, Mathf.Clamp(targetSteering, -1f, 1f), inputSmoothing * Time.deltaTime);
        brakeInput = Mathf.MoveTowards(brakeInput, Mathf.Clamp01(targetBrake), inputSmoothing * Time.deltaTime);
        handbrakeInput = targetHandbrake;
    }

    private void UpdateDriveState()
    {
        float speed = PlanarVelocity.magnitude;
        bool highSpeedTurn = speed >= driftStartSpeed && Mathf.Abs(steeringInput) >= driftSteerThreshold;
        bool wantsDrift = handbrakeInput || highSpeedTurn;

        if (wantsDrift)
        {
            driftHoldTimer = driftReleaseDelay;
        }
        else
        {
            driftHoldTimer = Mathf.Max(0f, driftHoldTimer - Time.fixedDeltaTime);
        }

        CurrentState = driftHoldTimer > 0f ? DriveState.Drift : DriveState.Normal;
    }

    private void ApplyMotor()
    {
        float forwardSpeed = ForwardSpeed;

        if (throttleInput > 0f && forwardSpeed < maxForwardSpeed)
        {
            body.AddForce(transform.forward * (throttleInput * acceleration), ForceMode.Acceleration);
        }
        else if (throttleInput < 0f && forwardSpeed > -maxReverseSpeed)
        {
            body.AddForce(transform.forward * (throttleInput * reverseAcceleration), ForceMode.Acceleration);
        }
    }

    private void ApplyBrakes()
    {
        if (brakeInput <= 0f)
        {
            return;
        }

        Vector3 planarVelocity = PlanarVelocity;
        if (planarVelocity.sqrMagnitude < 0.01f)
        {
            return;
        }

        body.AddForce(-planarVelocity.normalized * (brakeInput * brakeDeceleration), ForceMode.Acceleration);
    }

    private void ApplySteering()
    {
        float speed = PlanarVelocity.magnitude;
        float speedFactor = Mathf.Lerp(lowSpeedSteerFactor, 1f, Mathf.InverseLerp(0f, fullSteerSpeed, speed));
        float brakeFactor = Mathf.Lerp(1f, brakeSteerMultiplier, brakeInput);
        float driftFactor = IsDrifting ? driftSteeringMultiplier : 1f;
        float yawAcceleration = steeringInput * steeringAcceleration * speedFactor * brakeFactor * driftFactor;

        body.AddTorque(transform.up * yawAcceleration, ForceMode.Acceleration);
    }

    private void ApplyLateralGrip()
    {
        Vector3 lateralVelocity = transform.right * Vector3.Dot(body.linearVelocity, transform.right);
        float grip = IsDrifting ? driftLateralGrip : normalLateralGrip;

        body.AddForce(-lateralVelocity * grip, ForceMode.Acceleration);
    }

    private void ApplyDownforce()
    {
        float speed = PlanarVelocity.magnitude;
        body.AddForce(-transform.up * (downforce * speed), ForceMode.Acceleration);
    }
}
