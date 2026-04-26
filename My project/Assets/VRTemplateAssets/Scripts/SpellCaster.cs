using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

// Attach to any persistent GameObject (e.g. XR Origin).
// Drag one PinchParticleEmitter (the casting hand) into castingHand.
// While pinching, draw a clockwise circle in the air to cast Fireball.
public class SpellCaster : MonoBehaviour
{
    [SerializeField] PinchParticleEmitter castingHand;
    [SerializeField] float minConfidence = 0.65f;
    [SerializeField] float fireballSpeed = 8f;

    readonly List<Vector2> gesturePoints = new List<Vector2>();
    readonly List<SpellTemplate> templates = new List<SpellTemplate>();
    XRHandSubsystem handSubsystem;
    bool isGesturing;

    void Awake()
    {
        // Clockwise circle = Fireball gesture
        templates.Add(new SpellTemplate("Fireball", CirclePoints(64, clockwise: true)));
    }

    void OnEnable()
    {
        if (castingHand != null)
        {
            castingHand.OnPinchStart += BeginGesture;
            castingHand.OnPinchEnd += FinishGesture;
        }

        var subs = new List<XRHandSubsystem>();
        SubsystemManager.GetSubsystems(subs);
        if (subs.Count > 0) handSubsystem = subs[0];
    }

    void OnDisable()
    {
        if (castingHand != null)
        {
            castingHand.OnPinchStart -= BeginGesture;
            castingHand.OnPinchEnd -= FinishGesture;
        }
    }

    void BeginGesture()
    {
        gesturePoints.Clear();
        isGesturing = true;
    }

    void FinishGesture()
    {
        isGesturing = false;
        TryCastSpell();
    }

    void Update()
    {
        if (!isGesturing || handSubsystem == null) return;

        var casting = castingHand != null ? castingHand.Handedness : Handedness.Right;
        var hand = casting == Handedness.Right ? handSubsystem.rightHand : handSubsystem.leftHand;
        if (!hand.isTracked) return;
        if (!hand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out var pose)) return;

        var cam = Camera.main;
        if (cam == null) return;

        // Project to screen space so gesture recogniser gets 2D path regardless of distance
        gesturePoints.Add(cam.WorldToScreenPoint(pose.position));
    }

    void TryCastSpell()
    {
        var result = GestureRecogniser.Recognize(gesturePoints, templates);
        Debug.Log($"[SpellCaster] Gesture: {result.GestureName}  Confidence: {result.Confidence:F2}");

        if (result.Confidence >= minConfidence)
            CastFireball();
    }

    void CastFireball()
    {
        var cam = Camera.main;
        if (cam == null) return;

        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Fireball";
        go.transform.SetPositionAndRotation(
            cam.transform.position + cam.transform.forward * 0.4f,
            cam.transform.rotation);
        go.transform.localScale = Vector3.one * 0.12f;
        Destroy(go.GetComponent<Collider>());

        var rend = go.GetComponent<Renderer>();
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (shader != null)
        {
            var mat = new Material(shader);
            mat.SetColor("_BaseColor", new Color(1f, 0.1f, 0f));
            mat.SetColor("_EmissionColor", new Color(3f, 0.35f, 0f)); // HDR drives bloom
            mat.EnableKeyword("_EMISSION");
            rend.material = mat;
        }

        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.25f, 0f);
        light.intensity = 3f;
        light.range = 4f;

        var proj = go.AddComponent<FireballProjectile>();
        proj.speed = fireballSpeed;
    }

    static List<Vector2> CirclePoints(int n, bool clockwise)
    {
        var pts = new List<Vector2>(n);
        for (int i = 0; i < n; i++)
        {
            float angle = (clockwise ? -1f : 1f) * 2f * Mathf.PI * i / n;
            pts.Add(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 100f);
        }
        return pts;
    }
}
