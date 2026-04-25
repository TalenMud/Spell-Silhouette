using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

// Handles emission on/off based on pinch gesture.
// Position tracking is handled by PinchPointTracker on the same GameObject.
public class PinchParticleEmitter : MonoBehaviour
{
    [SerializeField] ParticleSystem particles;
    [SerializeField] Handedness handedness = Handedness.Right;
    [SerializeField] float pinchThreshold = 0.7f;

    [Header("Particle Appearance")]
    [SerializeField] float startSize = 0.018f;
    [SerializeField] float startLifetime = 0.4f;
    [SerializeField] float startSpeed = 0.08f;
    [SerializeField] float emissionRate = 80f;
    [SerializeField] Color particleColor = new Color(0.1f, 0.4f, 1f, 1f);

    XRHandSubsystem handSubsystem;
    bool isPinching;

    void Awake()
    {
        if (particles == null)
            particles = GetComponentInChildren<ParticleSystem>(true);

        ApplyParticleSettings();
    }

    void ApplyParticleSettings()
    {
        if (particles == null) return;

        var main = particles.main;
        main.startSize = startSize;
        main.startLifetime = startLifetime;
        main.startSpeed = startSpeed;
        main.startColor = particleColor;
        main.maxParticles = 500;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = particles.emission;
        emission.rateOverTime = emissionRate;
        emission.enabled = false;

        var shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.005f;
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
        if (handSubsystem == null) return;

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

        var emission = particles.emission;
        emission.enabled = pinching;

        if (pinching)
            particles.Play();
        else
            particles.Stop(false, ParticleSystemStopBehavior.StopEmitting);
    }
}
