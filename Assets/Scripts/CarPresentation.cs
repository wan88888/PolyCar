using UnityEngine;

/// <summary>Visual feedback is separate from the chassis and its collider.</summary>
public sealed class CarPresentation : MonoBehaviour
{
    public Transform visualRoot;
    public Transform[] wheels;
    public Renderer[] brakeLights;
    public Vector3 wheelSpinAxis = Vector3.up;
    public Vector3[] rearContactPoints;
    private Quaternion[] wheelBindRotations;
    public Material trailMaterial;
    public Material smokeMaterial;
    private CarController car;
    private TrailRenderer[] trails;
    private ParticleSystem[] smoke;
    private float wheelAngle;
    private MaterialPropertyBlock block;

    private void Awake()
    {
        if (!enabled) return;
        car = GetComponent<CarController>();
        block = new MaterialPropertyBlock();
        wheelBindRotations = new Quaternion[wheels.Length];
        for (int i=0;i<wheels.Length;i++) wheelBindRotations[i]=wheels[i].localRotation;
        trails = new TrailRenderer[2]; smoke = new ParticleSystem[2];
        for (int i = 0; i < 2; i++)
        {
            var mark = new GameObject("Rear tire marks"); mark.transform.SetParent(transform, false);
            mark.transform.localPosition = rearContactPoints != null && rearContactPoints.Length == 2 ? rearContactPoints[i] : new Vector3(i == 0 ? -0.88f : 0.88f, -0.18f, -1.1f);
            mark.transform.localRotation = Quaternion.Euler(90f,0f,0f);
            var trail = mark.AddComponent<TrailRenderer>();
            trail.alignment = LineAlignment.TransformZ;
            trail.sharedMaterial = trailMaterial; trail.time = 5f; trail.minVertexDistance = 0.1f;
            trail.widthMultiplier = 0.19f; trail.numCornerVertices = 3; trail.numCapVertices = 3;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; trail.receiveShadows = false;
            var markGradient = new Gradient();
            markGradient.SetKeys(new[] { new GradientColorKey(new Color(0.10f,0.11f,0.12f),0f),
                    new GradientColorKey(new Color(0.10f,0.11f,0.12f),1f) },
                new[] { new GradientAlphaKey(0f,0f), new GradientAlphaKey(0.32f,0.06f),
                    new GradientAlphaKey(0.24f,0.5f), new GradientAlphaKey(0f,1f) });
            trail.colorGradient = markGradient;
            trail.emitting = false; trails[i] = trail;
            var puff = new GameObject("Tire smoke"); puff.transform.SetParent(transform, false); puff.transform.localPosition = mark.transform.localPosition;
            var ps = puff.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main; main.startLifetime = 0.85f; main.startSpeed = 0.5f; main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
            main.startColor = new Color(0.78f,0.85f,0.87f,0.35f); main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 64;
            var emission = ps.emission; emission.rateOverTime = 0f;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 0.12f;
            var color = ps.colorOverLifetime; color.enabled = true;
            var gradient = new Gradient(); gradient.SetKeys(new[] {new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[] {new GradientAlphaKey(0.4f,0),new GradientAlphaKey(0,1)}); color.color = gradient;
            var size = ps.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0,0.4f,1,1.8f));
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = smokeMaterial;
            ps.Play(); smoke[i] = ps;
        }
    }

    private void LateUpdate()
    {
        if (car == null || !car.enabled) return;
        wheelAngle += car.ForwardSpeed * Time.deltaTime * 150f;
        for (int i = 0; wheels != null && i < wheels.Length; i++)
            wheels[i].localRotation = Quaternion.AngleAxis(i < 2 ? car.SteeringInput * 24f : 0f, Vector3.up) * wheelBindRotations[i] * Quaternion.AngleAxis(wheelAngle, wheelSpinAxis);
        if (visualRoot != null)
        {
            Quaternion lean = Quaternion.Euler(-car.BrakeInput * 1.8f, 0f, -car.SteeringInput * Mathf.Clamp(car.SpeedKmh / 18f,0f,3f));
            visualRoot.localRotation = Quaternion.Slerp(visualRoot.localRotation, lean, Time.deltaTime * 8f);
        }
        bool sliding = car.IsGrounded && car.IsDrifting && car.LateralSpeed > 0.8f && car.SpeedKmh > 18f;
        for (int i = 0; i < 2; i++)
        {
            trails[i].emitting = sliding;
            var emission = smoke[i].emission; emission.rateOverTime = sliding ? Mathf.Min(24f, car.LateralSpeed * 4f) : 0f;
        }
        foreach (var light in brakeLights)
        {
            block.SetColor("_BaseColor", car.BrakeInput > 0.1f ? new Color(1f,0.08f,0.025f) : new Color(0.35f,0.025f,0.015f));
            light.SetPropertyBlock(block);
        }
    }

    public void ClearTrails()
    {
        if (trails == null) return;
        foreach (var trail in trails) trail.Clear();
        foreach (var ps in smoke) ps.Clear();
    }
}
