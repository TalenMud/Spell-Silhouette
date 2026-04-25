using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

// Positions this GameObject at the midpoint between the index tip and thumb tip each frame.
// Place particle systems or effects as children of this object.
public class PinchPointTracker : MonoBehaviour
{
    [SerializeField] public Handedness handedness = Handedness.Right;

    XRHandSubsystem handSubsystem;

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
        if (!hand.isTracked) return;

        if (!hand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out var indexPose)) return;
        if (!hand.GetJoint(XRHandJointID.ThumbTip).TryGetPose(out var thumbPose)) return;

        transform.position = (indexPose.position + thumbPose.position) * 0.5f;
    }
}
