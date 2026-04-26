using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

// Attach to any persistent GameObject (e.g. XR Origin).
// Drag one PinchParticleEmitter (the casting hand) into castingHand.
// While pinching, draw a clockwise circle in the air to cast Fireball.
public class SpellCaster : MonoBehaviour
{
    [SerializeField] PinchParticleEmitter castingHand;
    [SerializeField] GameObject fireballPrefab;
    [SerializeField] float minConfidence = 0.65f;
    [SerializeField] float fireballSpeed = 8f;

    [Header("Audio")]
    [SerializeField] AudioClip spellCastClip;
    [Range(0f, 1f)] [SerializeField] float spellCastVolume = 1f;

    readonly List<Vector2> gesturePoints = new List<Vector2>();
    readonly List<SpellTemplate> templates = new List<SpellTemplate>();
    XRHandSubsystem handSubsystem;
    AudioSource castAudio;
    bool isGesturing;

    void Awake()
    {
        // Clockwise circle = Fireball gesture
        templates.Add(new SpellTemplate("Fireball", CirclePoints(64, clockwise: true)));

        if (spellCastClip == null)
            spellCastClip = Resources.Load<AudioClip>("Audio/spell_cast");

        castAudio = gameObject.AddComponent<AudioSource>();
        castAudio.playOnAwake = false;
        castAudio.spatialBlend = 0f;
        
        // Straight line = Force Push gesture
        templates.Add(new SpellTemplate("ForcePush", StraightLinePoints(64)));
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
        {
            if (result.GestureName == "ForcePush")
                CastForcePush();
            else
                CastFireball();
        }
    }

    void CastFireball()
    {
        var cam = Camera.main;
        if (cam == null) return;

        if (spellCastClip != null)
            castAudio.PlayOneShot(spellCastClip, spellCastVolume);

        var spawnPos = cam.transform.position + cam.transform.forward * 0.4f;
        var spawnRot = cam.transform.rotation;

        GameObject go;
        if (fireballPrefab != null)
        {
            go = Instantiate(fireballPrefab, spawnPos, spawnRot);
        }
        else
        {
            // Fallback: plain red sphere if no prefab assigned
            go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.SetPositionAndRotation(spawnPos, spawnRot);
            go.transform.localScale = Vector3.one * 0.12f;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader != null)
            {
                var mat = new Material(shader);
                mat.SetColor("_BaseColor", new Color(1f, 0.1f, 0f));
                mat.SetColor("_EmissionColor", new Color(3f, 0.35f, 0f));
                mat.EnableKeyword("_EMISSION");
                go.GetComponent<Renderer>().material = mat;
            }
        }

        go.name = "Fireball";
        var proj = go.AddComponent<FireballProjectile>();
        proj.speed = fireballSpeed;
    }

    void CastForcePush()
    {
        var cam = Camera.main;
        if (cam == null) return;

        var spawnPos = cam.transform.position + cam.transform.forward * 0.4f;
        var spawnRot = cam.transform.rotation;

        var go = new GameObject("ForcePush");
        go.transform.SetPositionAndRotation(spawnPos, spawnRot);

        // Twice the diameter of the fireball fallback sphere (0.12 * 2 = 0.24)
        var col = go.AddComponent<SphereCollider>();
        col.radius = 0.12f;

        var rb = go.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        var proj = go.AddComponent<ForcePushProjectile>();
        proj.speed = fireballSpeed * 1.2f;
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

    static List<Vector2> StraightLinePoints(int n)
    {
        var pts = new List<Vector2>(n);
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / (n - 1);
            pts.Add(new Vector2(Mathf.Lerp(-100f, 100f, t), 0f));
        }
        return pts;
    }
}
