using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

public class PinchParticleEmitter : MonoBehaviour
{
    [SerializeField] ParticleSystem particles;
    [SerializeField] Handedness handedness = Handedness.Right;
    [SerializeField] float pinchThreshold = 0.7f;

    [Header("Particle Appearance")]
    [SerializeField] float startSize = 0.02f;
    [SerializeField] float startLifetime = 0.35f;
    [SerializeField] float startSpeed = 0.06f;
    [SerializeField] float emissionRate = 70f;
    [ColorUsage(false, true)]
    [SerializeField] Color particleColor = new Color(0.3f, 1.0f, 3.0f, 1f);

    public Handedness Handedness => handedness;
    public static bool IsAnyHandPinching => s_pinchCount > 0;
    static int s_pinchCount;

    public event Action OnPinchStart;
    public event Action OnPinchEnd;

    XRHandSubsystem handSubsystem;
    bool isPinching;

    void Awake()
    {
        if (particles == null)
            particles = GetComponentInChildren<ParticleSystem>(true);

        if (particles == null) return;

        ApplyParticleSettings();
        ApplyGlowMaterial();
    }

    void ApplyParticleSettings()
    {
        var main = particles.main;
        main.startSize = startSize;
        main.startLifetime = startLifetime;
        main.startSpeed = startSpeed;
        main.startColor = particleColor;
        main.maxParticles = 200;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = particles.emission;
        emission.rateOverTime = emissionRate;
        emission.enabled = false;

        var shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.006f;
    }

    void ApplyGlowMaterial()
    {
        var rend = particles.GetComponent<ParticleSystemRenderer>();
        if (rend == null) return;

        var existing = rend.sharedMaterial;
        if (existing != null && existing.name != "Default-Particle" && existing.name != "Default-ParticleSystem")
            return;

        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                  ?? Shader.Find("Particles/Additive");
        if (shader == null) return;

        var mat = new Material(shader) { name = "PinchGlow_Runtime" };
        mat.SetFloat("_Surface", 1);
        mat.SetFloat("_Blend", 2);
        mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        mat.SetFloat("_ZWrite", 0);
        mat.renderQueue = 3000;
        rend.material = mat;
    }

    void OnEnable()
    {
        var subsystems = new List<XRHandSubsystem>();
        SubsystemManager.GetSubsystems(subsystems);
        if (subsystems.Count > 0)
            handSubsystem = subsystems[0];
    }

    void Update()
    {
        if (handSubsystem == null || particles == null) return;

        var hand = handedness == Handedness.Right ? handSubsystem.rightHand : handSubsystem.leftHand;
        if (!hand.isTracked) { SetPinching(false); return; }

        if (!hand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out var indexPose)) return;
        if (!hand.GetJoint(XRHandJointID.ThumbTip).TryGetPose(out var thumbPose)) return;

        var gestures = handedness == Handedness.Right
            ? handSubsystem.rightHandCommonGestures
            : handSubsystem.leftHandCommonGestures;

        bool pinching;
        if (gestures.TryGetPinchValue(out float pinchValue))
            pinching = pinchValue >= pinchThreshold;
        else
            pinching = Vector3.Distance(indexPose.position, thumbPose.position) < 0.025f;

        SetPinching(pinching);
    }

    void SetPinching(bool pinching)
    {
        if (pinching == isPinching) return;
        isPinching = pinching;
        s_pinchCount = Mathf.Max(0, s_pinchCount + (pinching ? 1 : -1));

        var emission = particles.emission;
        emission.enabled = pinching;

        if (pinching)
        {
            particles.Play();
            OnPinchStart?.Invoke();
        }
        else
        {
            particles.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            OnPinchEnd?.Invoke();
        }
    }
}
