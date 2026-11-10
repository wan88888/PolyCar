using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

public sealed class DrivingRegressionTests
{
    private Scene scene;
    private CarController car;
    private Rigidbody body;
    private GameManager scoring;
    private SimulationMode previousSimulationMode;

    [SetUp]
    public void SetUp()
    {
        scene = EditorSceneManager.NewPreviewScene();
        previousSimulationMode = Physics.simulationMode;
        Physics.simulationMode = SimulationMode.Script;
        var vehicle = new GameObject("Test car");
        SceneManager.MoveGameObjectToScene(vehicle, scene);
        body = vehicle.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.linearDamping = 0f;
        body.angularDamping = 0f;
        car = vehicle.AddComponent<CarController>();
        Call(car, "Awake");
        Set(car, "downforce", 0f);
        car.SetInput(0f, 0f, 0f, false);

        var manager = new GameObject("Test scoring");
        SceneManager.MoveGameObjectToScene(manager, scene);
        scoring = manager.AddComponent<GameManager>();
        Set(scoring, "playerCar", car);
    }

    [TearDown]
    public void TearDown()
    {
        EditorSceneManager.ClosePreviewScene(scene);
        Physics.simulationMode = previousSimulationMode;
    }

    [Test]
    public void ThrottleAcceleratesAndResetStopsTheCar()
    {
        car.SetInput(1f, 0f, 0f, false);
        for (int i = 0; i < 50; i++) Step();
        Assert.That(car.ForwardSpeed, Is.GreaterThan(15f));
        car.ResetCar(Vector3.zero, Quaternion.identity);
        Step();
        Assert.That(body.linearVelocity.magnitude, Is.LessThan(0.001f));
        Assert.That(car.IsDrifting, Is.False);
    }

    [TestCase("StarterCar")]
    [TestCase("DriftCar")]
    [TestCase("RallyCar")]
    [TestCase("SpeedCar")]
    public void CarCanReachDriftSpeedOnTheRoad(string prefabName)
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        SceneManager.MoveGameObjectToScene(ground, scene);
        ground.transform.position = new Vector3(0f, -0.5f, 0f);
        ground.transform.localScale = new Vector3(1000f, 1f, 1000f);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Vehicles/{prefabName}.prefab");
        var vehicle = Object.Instantiate(prefab, new Vector3(0f, 0.7f, 0f), Quaternion.identity);
        SceneManager.MoveGameObjectToScene(vehicle, scene);
        var controller = vehicle.GetComponent<CarController>();
        Call(controller, "Awake");
        controller.SetInput(1f, 0f, 0f, false);
        Physics.SyncTransforms();
        for (int i = 0; i < 250; i++)
        {
            Call(controller, "FixedUpdate");
            scene.GetPhysicsScene().Simulate(Time.fixedDeltaTime);
        }

        Assert.That(controller.ForwardSpeed, Is.GreaterThanOrEqualTo(Get<float>(controller, "driftStartSpeed")), "Each car must reach its configured automatic-drift threshold on level ground.");
        TestContext.WriteLine($"{prefabName} after 5s: {controller.SpeedKmh:F1} km/h");
        controller.SetInput(1f, 0.7f, 0f, false);
        for (int i = 0; i < 20; i++)
        {
            Call(controller, "FixedUpdate");
            scene.GetPhysicsScene().Simulate(Time.fixedDeltaTime);
        }

        Assert.That(controller.IsDrifting, Is.True);
        Assert.That(controller.LateralSpeed, Is.GreaterThan(0.5f));
    }

    [TestCase(0.2f)]
    [TestCase(-0.2f)]
    public void BrakingStopsWithoutReversing(float speed)
    {
        body.linearVelocity = Vector3.forward * speed;
        car.SetInput(0f, 0f, 1f, false);
        Step();
        Assert.That(body.linearVelocity.z * Mathf.Sign(speed), Is.GreaterThanOrEqualTo(-0.0001f));
        Assert.That(body.linearVelocity.magnitude, Is.LessThan(0.01f));
    }

    [TestCase(10f, 1f)]
    [TestCase(-10f, -1f)]
    public void SteeringReversesWhenDrivingBackwards(float speed, float yawSign)
    {
        body.linearVelocity = Vector3.forward * speed;
        car.SetInput(0f, 1f, 0f, false);
        Step();
        Assert.That(body.angularVelocity.y * yawSign, Is.GreaterThan(0f));
    }

    [Test]
    public void StationaryCarDoesNotSpinOrEnterDrift()
    {
        car.SetInput(0f, 1f, 1f, true);
        Step();
        Assert.That(body.angularVelocity.magnitude, Is.LessThan(0.001f));
        Assert.That(car.IsDrifting, Is.False);
    }

    [Test]
    public void HighSpeedTurnEntersDriftEvenWithZeroReleaseDelay()
    {
        Set(car, "driftReleaseDelay", 0f);
        body.linearVelocity = Vector3.forward * 15f;
        car.SetInput(0f, 1f, 0f, false);
        Step();
        Assert.That(car.IsDrifting, Is.True);
    }

    [Test]
    public void DriftReleasesAfterTheHoldTime()
    {
        body.linearVelocity = Vector3.forward * 15f;
        car.SetInput(0f, 1f, 0f, false);
        Step();
        Assert.That(car.IsDrifting, Is.True);
        car.SetInput(0f, 0f, 0f, false);
        Step();
        Assert.That(car.IsDrifting, Is.True);
        for (int i = 0; i < 20; i++) Step();
        Assert.That(car.IsDrifting, Is.False);
    }

    [Test]
    public void GamepadHandbrakeDoesNotApplyTheBrakePedal()
    {
        Gamepad gamepad = InputSystem.AddDevice<Gamepad>();
        try
        {
            InputSystem.QueueStateEvent(gamepad, new GamepadState().WithButton(GamepadButton.South));
            InputSystem.Update();
            // A long smoothing interval isolates the input mapping from frame timing.
            Call(car, "ReadInput", 0.1f);
            Assert.That(Get<bool>(car, "handbrakeInput"), Is.True);
            Assert.That(Get<float>(car, "brakeInput"), Is.Zero);
        }
        finally
        {
            InputSystem.RemoveDevice(gamepad);
        }
    }

    [Test]
    public void KeyboardHandbrakeDoesNotApplyTheBrakePedal()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();
        try
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
            InputSystem.Update();
            Call(car, "ReadInput", 0.1f);
            Assert.That(Get<bool>(car, "handbrakeInput"), Is.True);
            Assert.That(Get<float>(car, "brakeInput"), Is.Zero);
        }
        finally { InputSystem.RemoveDevice(keyboard); }
    }

    [Test]
    public void HeldHandbrakeRetainsMomentumButPedalStillStops()
    {
        body.linearVelocity = Vector3.forward * 20f;
        car.SetInput(0f, 0f, 0f, true);
        for(int i=0;i<100;i++) Step();
        Assert.That(car.ForwardSpeed, Is.InRange(12f,16f), "Two seconds of handbrake must loosen grip without stopping the car.");
        car.SetInput(0f, 0f, 1f, false);
        for(int i=0;i<40;i++) Step();
        Assert.That(body.linearVelocity.magnitude, Is.LessThan(0.01f));
    }

    [TestCase("StarterCar")]
    [TestCase("DriftCar")]
    [TestCase("RallyCar")]
    [TestCase("SpeedCar")]
    public void HandbrakeEntryCanSustainPoweredDriftAndCountersteer(string prefabName)
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        SceneManager.MoveGameObjectToScene(ground, scene);
        ground.transform.position = new Vector3(0,-0.5f,0);
        ground.transform.localScale = new Vector3(1000,1,1000);
        var vehicle = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Vehicles/{prefabName}.prefab"),new Vector3(0,0.7f,0),Quaternion.identity);
        SceneManager.MoveGameObjectToScene(vehicle,scene);
        var controller = vehicle.GetComponent<CarController>();
        var chassis = vehicle.GetComponent<Rigidbody>();
        Call(controller,"Awake"); controller.SetInput(0,0,0,false);
        Physics.SyncTransforms();
        for(int i=0;i<40;i++) { Call(controller,"FixedUpdate"); scene.GetPhysicsScene().Simulate(0.02f); }
        chassis.linearVelocity = vehicle.transform.forward * 20f;
        float slipping=0,minSpeed=1000;
        for(int i=0;i<200;i++)
        {
            controller.SetInput(1f,0.55f,0f,i<15);
            Call(controller,"FixedUpdate");scene.GetPhysicsScene().Simulate(0.02f);
            if(controller.IsDrifting && controller.LateralSpeed>1f) slipping+=0.02f;
            minSpeed=Mathf.Min(minSpeed,controller.SpeedKmh);
        }
        TestContext.WriteLine($"{prefabName}: drift={slipping:F2}s minimum={minSpeed:F1}km/h end={controller.SpeedKmh:F1}km/h");
        Assert.That(slipping,Is.GreaterThan(2.5f));
        Assert.That(minSpeed,Is.GreaterThan(25f));
        float previousYaw=chassis.angularVelocity.y;
        controller.SetInput(1f,-0.55f,0f,false);
        for(int i=0;i<15;i++) { Call(controller,"FixedUpdate");scene.GetPhysicsScene().Simulate(0.02f); }
        Assert.That(chassis.angularVelocity.y,Is.LessThan(previousYaw),"Countersteer must reduce the current yaw.");
        Assert.That(controller.IsDrifting,Is.True,"Countersteering should not snap immediately back to full grip.");
    }

    [Test]
    public void StraightHandbrakingDoesNotEarnDriftScore()
    {
        body.linearVelocity = Vector3.forward * 20f;
        car.SetInput(0f, 0f, 1f, true);
        Call(car, "UpdateDriveState");
        Call(scoring, "UpdateDriftScoring", 2f);
        Assert.That(scoring.TotalDriftScore, Is.Zero);
    }

    [Test]
    public void SustainedSideSlipEarnsScoreAndCombo()
    {
        SetScoringDrift();
        Call(scoring, "UpdateDriftScoring", 2f);
        scoring.FlushActiveDrift();
        Assert.That(scoring.TotalDriftScore, Is.GreaterThan(120));
        Assert.That(scoring.ComboMultiplier, Is.GreaterThan(1f));
    }

    [Test]
    public void CollisionInvalidatesTheWholeDriftUntilItEnds()
    {
        SetScoringDrift();
        Call(scoring, "UpdateDriftScoring", 1f);
        scoring.RegisterObstacleHit(0);
        Call(scoring, "UpdateDriftScoring", 2f);
        scoring.FlushActiveDrift();
        Assert.That(scoring.TotalDriftScore, Is.Zero);
        Assert.That(scoring.ComboMultiplier, Is.EqualTo(1f));

        car.ResetCar(Vector3.zero, Quaternion.identity);
        Call(scoring, "UpdateDriftScoring", 0.02f);
        SetScoringDrift();
        Call(scoring, "UpdateDriftScoring", 2f);
        scoring.FlushActiveDrift();
        Assert.That(scoring.TotalDriftScore, Is.GreaterThan(120));
    }

    private void SetScoringDrift()
    {
        body.linearVelocity = new Vector3(4f, 0f, 20f);
        car.SetInput(0f, 1f, 0f, false);
        Call(car, "UpdateDriveState");
    }

    private void Step()
    {
        Call(car, "FixedUpdate");
        scene.GetPhysicsScene().Simulate(Time.fixedDeltaTime);
    }

    private static void Call(object target, string method, params object[] args)
    {
        target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, args);
    }

    private static void Set(object target, string field, object value)
    {
        target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }

    private static T Get<T>(object target, string field)
    {
        return (T)target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    }
}
