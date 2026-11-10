using UnityEngine;
using UnityEngine.SceneManagement;
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
    [SerializeField, Min(0f)] private float handbrakeDeceleration = 3f;
    [SerializeField, Range(0f, 1f)] private float brakeSteerMultiplier = 0.75f;

    [Header("Drift")]
    [SerializeField, Min(0f)] private float driftStartSpeed = 12f;
    [SerializeField, Range(0f, 1f)] private float driftSteerThreshold = 0.55f;
    [SerializeField, Min(0f)] private float normalLateralGrip = 9f;
    [SerializeField, Min(0f)] private float driftLateralGrip = 2.1f;
    [SerializeField, Min(0f)] private float driftSteeringMultiplier = 1.25f;
    [SerializeField, Min(0f)] private float driftReleaseDelay = 0.35f;
    [SerializeField, Min(0f)] private float gripRecoveryRate = 3f;

    [Header("Stability")]
    [SerializeField] private Vector3 centerOfMassOffset = new Vector3(0f, -0.45f, 0f);
    [SerializeField, Min(0f)] private float downforce = 4f;

    public bool ControlsEnabled { get; private set; } = true;
    public bool IsGrounded { get; private set; } = true;
    public float SteeringInput => steeringInput;
    public float BrakeInput => Mathf.Max(brakeInput, handbrakeInput ? 0.45f : 0f);
    private Collider vehicleCollider;
    private Rigidbody body;
    private float throttleInput;
    private float steeringInput;
    private float brakeInput;
    private bool handbrakeInput;
    private float driftHoldTimer;
    private float driftBlend;

    public DriveState CurrentState { get; private set; } = DriveState.Normal;
    public bool IsDrifting => CurrentState == DriveState.Drift;
    public float SpeedKmh => PlanarVelocity.magnitude * 3.6f;
    public float ForwardSpeed => Vector3.Dot(body.linearVelocity, transform.forward);
    public float LateralSpeed => Mathf.Abs(Vector3.Dot(body.linearVelocity, transform.right));

    private Vector3 PlanarVelocity => Vector3.ProjectOnPlane(body.linearVelocity, transform.up);

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        vehicleCollider = GetComponent<Collider>();
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.centerOfMass = centerOfMassOffset;
        body.maxAngularVelocity = 8f;
    }

    private void Update()
    {
        if (readPlayerInput && ControlsEnabled)
        {
            ReadInput(Time.deltaTime);
        }
    }

    private void FixedUpdate()
    {
        IsGrounded = vehicleCollider == null || gameObject.scene.GetPhysicsScene().Raycast(
            transform.position + Vector3.up * 0.25f, Vector3.down, out RaycastHit hit, 0.85f, ~0, QueryTriggerInteraction.Ignore)
            && Vector3.Dot(hit.normal, Vector3.up) > 0.4f && Vector3.Dot(transform.up, Vector3.up) > 0.4f;
        if (!IsGrounded)
        {
            CurrentState = DriveState.Normal;
            driftHoldTimer = 0f;
            return;
        }
        UpdateDriveState();
        ApplyMotor();
        ApplyBrakes();
        ApplySteering();
        ApplyLateralGrip();
        ApplyDownforce();
    }

    public void SetControlsEnabled(bool enabled)
    {
        ControlsEnabled = enabled;
        if (!enabled)
        {
            throttleInput = steeringInput = 0f;
            brakeInput = 1f;
            handbrakeInput = false;
            driftHoldTimer = 0f;
            CurrentState = DriveState.Normal;
        }
        else brakeInput = 0f;
    }

    public void SetInput(float throttle, float steering, float brake, bool handbrake)
    {
        readPlayerInput = false;
        if (!ControlsEnabled) return;
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
        driftBlend = 0f;
        CurrentState = DriveState.Normal;

        GetComponent<CarPresentation>()?.ClearTrails();
        body.position = position;
        body.rotation = rotation;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
    }

    private void ReadInput(float deltaTime)
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
            targetBrake = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed ? 1f : 0f;
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

        // Handbrake loosens rear grip; the pedal remains a separate full-strength brake.
        throttleInput = Mathf.MoveTowards(throttleInput, Mathf.Clamp(targetThrottle, -1f, 1f), inputSmoothing * deltaTime);
        steeringInput = Mathf.MoveTowards(steeringInput, Mathf.Clamp(targetSteering, -1f, 1f), inputSmoothing * deltaTime);
        brakeInput = Mathf.MoveTowards(brakeInput, Mathf.Clamp01(targetBrake), inputSmoothing * deltaTime);
        handbrakeInput = targetHandbrake;
    }

    private void UpdateDriveState()
    {
        float speed = PlanarVelocity.magnitude;
        bool highSpeedTurn = speed >= driftStartSpeed && Mathf.Abs(steeringInput) >= driftSteerThreshold;
        // Keep a slide alive through countersteering without requiring another handbrake tap.
        bool balancingSlide = IsDrifting && speed >= 5f && LateralSpeed > 1f && Mathf.Abs(steeringInput) > 0.1f;
        bool wantsDrift = speed >= 1f && (handbrakeInput || highSpeedTurn || balancingSlide);

        if (wantsDrift)
        {
            driftHoldTimer = driftReleaseDelay;
        }
        else
        {
            driftHoldTimer = Mathf.Max(0f, driftHoldTimer - Time.fixedDeltaTime);
        }

        CurrentState = speed >= 1f && (wantsDrift || driftHoldTimer > 0f) ? DriveState.Drift : DriveState.Normal;
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
        float requestedDeceleration = Mathf.Max(brakeInput * brakeDeceleration,
            handbrakeInput ? handbrakeDeceleration : 0f);
        if (requestedDeceleration <= 0f)
        {
            return;
        }

        Vector3 planarVelocity = PlanarVelocity;
        float speed = planarVelocity.magnitude;
        if (speed <= Mathf.Epsilon)
        {
            return;
        }

        // A brake can remove momentum, but must never accelerate through zero.
        float deceleration = Mathf.Min(requestedDeceleration, speed / Time.fixedDeltaTime);
        body.AddForce(-planarVelocity.normalized * deceleration, ForceMode.Acceleration);
    }

    private void ApplySteering()
    {
        float speed = PlanarVelocity.magnitude;
        float speedFactor = Mathf.Lerp(lowSpeedSteerFactor, 1f, Mathf.InverseLerp(0f, fullSteerSpeed, speed));
        float brakeFactor = Mathf.Lerp(1f, brakeSteerMultiplier, brakeInput);
        float driftFactor = IsDrifting ? driftSteeringMultiplier : 1f;
        // Reverse travel reverses yaw; fade to zero at rest instead of spinning in place.
        float travelDirection = Mathf.Clamp(ForwardSpeed, -1f, 1f);
        float yawAcceleration = steeringInput * steeringAcceleration * speedFactor * brakeFactor * driftFactor * travelDirection;

        // Reduce steering further into a large slide, while retaining full countersteer authority.
        if (IsDrifting && ForwardSpeed > 3f)
        {
            float slipAngle = Vector3.SignedAngle(PlanarVelocity, transform.forward, transform.up);
            if (Mathf.Sign(steeringInput) == Mathf.Sign(slipAngle))
                yawAcceleration *= Mathf.Lerp(1f, 0.25f, Mathf.InverseLerp(30f, 60f, Mathf.Abs(slipAngle)));
        }

        body.AddTorque(transform.up * yawAcceleration, ForceMode.Acceleration);
    }

    private void ApplyLateralGrip()
    {
        Vector3 lateralVelocity = transform.right * Vector3.Dot(body.linearVelocity, transform.right);
        driftBlend = Mathf.MoveTowards(driftBlend, IsDrifting ? 1f : 0f,
            Time.fixedDeltaTime * (IsDrifting ? 8f : gripRecoveryRate));
        float grip = Mathf.Lerp(normalLateralGrip, driftLateralGrip, driftBlend);

        body.AddForce(-lateralVelocity * grip, ForceMode.Acceleration);
    }

    private void ApplyDownforce()
    {
        float speed = PlanarVelocity.magnitude;
        body.AddForce(-transform.up * (downforce * speed), ForceMode.Acceleration);
    }
}
