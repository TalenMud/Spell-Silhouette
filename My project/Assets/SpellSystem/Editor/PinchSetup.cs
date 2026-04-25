using UnityEngine;
using UnityEditor;

public static class PinchSetup
{
    [MenuItem("SpellSystem/Setup Pinch Particles")]
    public static void Setup()
    {
        SetupHand("XR Origin Hands (XR Rig)/Camera Offset/Left Hand/Pinch Point Stabilized", 1);
        SetupHand("XR Origin Hands (XR Rig)/Camera Offset/Right Hand/Pinch Point Stabilized", 2);
        Debug.Log("[SpellSystem] Pinch particle emitters set up on both hands.");
    }

    static void SetupHand(string pinchPointPath, int handednessIndex)
    {
        var pinchPoint = FindByPath(pinchPointPath);
        if (pinchPoint == null) { Debug.LogError($"[SpellSystem] Could not find: {pinchPointPath}"); return; }

        // Create or reuse Pinch Particles child
        var existing = pinchPoint.transform.Find("Pinch Particles");
        GameObject particlesGO;
        if (existing != null)
        {
            particlesGO = existing.gameObject;
        }
        else
        {
            particlesGO = new GameObject("Pinch Particles");
            Undo.RegisterCreatedObjectUndo(particlesGO, "Create Pinch Particles");
            particlesGO.transform.SetParent(pinchPoint.transform, false);
        }

        // Add ParticleSystem if missing
        if (!particlesGO.TryGetComponent<ParticleSystem>(out var ps))
            ps = Undo.AddComponent<ParticleSystem>(particlesGO);

        // Add PinchParticleEmitter if missing
        if (!pinchPoint.TryGetComponent<PinchParticleEmitter>(out var emitter))
            emitter = Undo.AddComponent<PinchParticleEmitter>(pinchPoint);

        // Wire references via SerializedObject
        var so = new SerializedObject(emitter);
        so.FindProperty("handedness").enumValueIndex = handednessIndex;
        so.FindProperty("pinchThreshold").floatValue = 0.7f;
        so.FindProperty("particles").objectReferenceValue = ps;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(pinchPoint);
    }

    static GameObject FindByPath(string path)
    {
        var parts = path.Split('/');
        var root = GameObject.Find(parts[0]);
        if (root == null) return null;
        Transform t = root.transform;
        for (int i = 1; i < parts.Length; i++)
        {
            t = t.Find(parts[i]);
            if (t == null) return null;
        }
        return t.gameObject;
    }
}
