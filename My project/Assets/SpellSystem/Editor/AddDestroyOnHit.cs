using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AddDestroyOnHit
{
    [MenuItem("Tools/Setup ShootingTarget")]
    static void Setup()
    {
        var target = GameObject.Find("ShootingTarget");
        if (target == null) { Debug.LogWarning("ShootingTarget not found in scene."); return; }

        var type = Type.GetType("DestroyOnHit, Assembly-CSharp");
        if (type == null) { Debug.LogWarning("DestroyOnHit type not found — make sure the script compiled."); return; }

        if (target.GetComponent(type) == null)
            target.AddComponent(type);

        var doh = target.GetComponent(type);
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/VRTemplateAssets/Audio/Button_22_click.wav");
        if (clip != null)
        {
            var so = new SerializedObject(doh);
            so.FindProperty("hitSound").objectReferenceValue = clip;
            so.ApplyModifiedProperties();
        }

        EditorUtility.SetDirty(target);
        EditorSceneManager.MarkSceneDirty(target.scene);
        Debug.Log("DestroyOnHit added and sound wired on ShootingTarget.");
    }
}
