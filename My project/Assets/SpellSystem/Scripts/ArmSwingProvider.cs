using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;

namespace SpellSystem
{
    [AddComponentMenu("XR/Locomotion/Arm Swing Provider")]
    public class ArmSwingProvider : LocomotionProvider
    {
        [Tooltip("Constant movement speed in m/s once swinging is detected.")]
        [SerializeField] float moveSpeed = 3f;

        [Tooltip("Minimum average hand speed (m/s) before movement starts.")]
        [SerializeField] float swingDeadzone = 0.3f;

        [Tooltip("How quickly speed ramps up/down. Higher = snappier response.")]
        [SerializeField] float smoothing = 8f;

        XRHandSubsystem handSubsystem;
        Vector3 leftWristPrev, rightWristPrev;
        bool leftInit, rightInit;
        float smoothedSpeed;

        readonly XROriginMovement transformation = new XROriginMovement();

        protected override void OnEnable()
        {
            base.OnEnable();
            var subsystems = new List<XRHandSubsystem>();
            SubsystemManager.GetSubsystems(subsystems);
            handSubsystem = subsystems.Count > 0 ? subsystems[0] : null;
            leftInit = rightInit = false;
            smoothedSpeed = 0f;
        }

        void Update()
        {
            var xrOrigin = mediator != null ? mediator.xrOrigin : null;
            if (xrOrigin == null || handSubsystem == null) return;

            if (PinchParticleEmitter.IsAnyHandPinching)
            {
                smoothedSpeed = 0f;
                TryEndLocomotion();
                return;
            }

            float leftSpeed = GetWristSpeed(handSubsystem.leftHand, ref leftWristPrev, ref leftInit);
            float rightSpeed = GetWristSpeed(handSubsystem.rightHand, ref rightWristPrev, ref rightInit);

            float rawSwing = (leftSpeed + rightSpeed) * 0.5f;
            float targetSpeed = rawSwing > swingDeadzone ? moveSpeed : 0f;
            smoothedSpeed = Mathf.Lerp(smoothedSpeed, targetSpeed, smoothing * Time.deltaTime);

            if (smoothedSpeed < 0.001f)
            {
                TryEndLocomotion();
                return;
            }

            var cam = xrOrigin.Camera;
            if (cam == null) return;

            var headForward = cam.transform.forward;
            headForward.y = 0f;
            if (headForward.sqrMagnitude < 0.001f) return;
            headForward.Normalize();

            TryStartLocomotionImmediately();
            if (locomotionState != LocomotionState.Moving) return;

            transformation.motion = headForward * smoothedSpeed * Time.deltaTime;
            TryQueueTransformation(transformation);
        }

        float GetWristSpeed(XRHand hand, ref Vector3 prev, ref bool initialized)
        {
            if (!hand.isTracked) { initialized = false; return 0f; }
            if (!hand.GetJoint(XRHandJointID.Wrist).TryGetPose(out var pose)) return 0f;

            var pos = pose.position;
            float speed = initialized ? Vector3.Distance(pos, prev) / Time.deltaTime : 0f;
            prev = pos;
            initialized = true;
            return speed;
        }
    }
}
