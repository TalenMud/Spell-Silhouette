using UnityEditor;
using UnityEngine;

public static class AutoBoxCollider
{
    [MenuItem("Tools/Add Box Colliders to Selected Children")]
    static void AddBoxCollidersToChildren()
    {
        GameObject root = Selection.activeGameObject;
        if (root == null)
        {
            Debug.LogWarning("Select a building/room root GameObject first.");
            return;
        }

        int count = 0;
        foreach (MeshFilter mf in root.GetComponentsInChildren<MeshFilter>())
        {
            GameObject go = mf.gameObject;
            if (go.GetComponent<Collider>() != null)
                continue;

            BoxCollider bc = go.AddComponent<BoxCollider>();
            Bounds b = mf.sharedMesh.bounds;
            bc.center = b.center;
            bc.size = b.size;
            count++;
        }

        Debug.Log($"Added {count} BoxColliders under '{root.name}'.");
        EditorUtility.SetDirty(root);
    }

    [MenuItem("Tools/Remove All Colliders from Selected Children")]
    static void RemoveCollidersFromChildren()
    {
        GameObject root = Selection.activeGameObject;
        if (root == null) return;

        int count = 0;
        foreach (Collider c in root.GetComponentsInChildren<Collider>())
        {
            Undo.DestroyObjectImmediate(c);
            count++;
        }

        Debug.Log($"Removed {count} colliders from '{root.name}'.");
    }
}
