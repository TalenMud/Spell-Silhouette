using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class DungeonPropScatterer : EditorWindow
{
    const string PropsFolder = "Assets/Decrepit Dungeon LITE/Prefabs/Props";
    const string ScatterParentName = "_ScatteredProps";

    static readonly string[] DefaultWallPrefabs   = { "Torch", "Chain_A", "Chain_B", "Chain_C", "Chain_D", "Chain_E" };
    static readonly string[] DefaultFloorPrefabs  = { "Barrel", "Bucket", "Candles", "Crate_A", "Crate_B", "Crate_C", "Stool_A", "Table_A" };
    static readonly string[] DefaultCeilingPrefabs = { "Hanging_Cage" };

    [System.Serializable]
    class Section
    {
        public bool enabled = true;
        public bool foldout = true;
        public List<GameObject> prefabs = new List<GameObject>();
        public int density = 20;
        public Vector2 scaleRange = new Vector2(0.9f, 1.1f);
        public bool randomYRotation = true;
        public float minSpacing = 0.4f;
    }

    [SerializeField] Section _walls   = new Section { density = 12, minSpacing = 1.2f };
    [SerializeField] Section _floors  = new Section { density = 25, minSpacing = 0.5f };
    [SerializeField] Section _ceiling = new Section { density = 4,  minSpacing = 1.5f };

    [SerializeField] Vector2 _wallHeightRange = new Vector2(1.4f, 2.2f);
    [SerializeField] Transform _outputParentOverride;
    [SerializeField] bool _defaultsLoaded;
    Vector2 _scroll;

    [MenuItem("Tools/Dungeon Prop Scatterer")]
    public static void Open()
    {
        var w = GetWindow<DungeonPropScatterer>("Prop Scatterer");
        w.minSize = new Vector2(340, 400);
        w.Show();
    }

    void OnEnable()
    {
        if (!_defaultsLoaded) LoadDefaultPrefabs();
    }

    void LoadDefaultPrefabs()
    {
        _walls.prefabs   = LoadByName(DefaultWallPrefabs);
        _floors.prefabs  = LoadByName(DefaultFloorPrefabs);
        _ceiling.prefabs = LoadByName(DefaultCeilingPrefabs);
        _defaultsLoaded = true;
    }

    static List<GameObject> LoadByName(string[] names)
    {
        var list = new List<GameObject>(names.Length);
        foreach (var n in names)
        {
            var path = $"{PropsFolder}/{n}.prefab";
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go != null) list.Add(go);
            else Debug.LogWarning($"[PropScatterer] Missing prefab: {path}");
        }
        return list;
    }

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        EditorGUILayout.LabelField("Dungeon Prop Scatterer", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Select a GameObject in the scene whose bounds enclose the room you want to populate, then run a scatter.",
            MessageType.Info);

        var target = Selection.activeGameObject;
        EditorGUILayout.LabelField("Selected:", target != null ? target.name : "<none>");
        _outputParentOverride = (Transform)EditorGUILayout.ObjectField(
            new GUIContent("Output Parent", $"Optional. Defaults to a child named '{ScatterParentName}' under the selection."),
            _outputParentOverride, typeof(Transform), true);

        EditorGUILayout.Space();

        DrawSection("Walls",   _walls,   "_walls",   wallSection: true);
        DrawSection("Floors",  _floors,  "_floors",  wallSection: false);
        DrawSection("Ceiling", _ceiling, "_ceiling", wallSection: false);

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(target == null))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Scatter Walls"))   ScatterWalls(target);
                if (GUILayout.Button("Scatter Floors"))  ScatterFloors(target);
                if (GUILayout.Button("Scatter Ceiling")) ScatterCeiling(target);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Scatter All", GUILayout.Height(28)))
                {
                    if (_floors.enabled)  ScatterFloors(target);
                    if (_walls.enabled)   ScatterWalls(target);
                    if (_ceiling.enabled) ScatterCeiling(target);
                }
                if (GUILayout.Button("Clear Last Scatter", GUILayout.Height(28))) ClearScatter(target);
            }
        }

        EditorGUILayout.EndScrollView();
    }

    void DrawSection(string label, Section s, string fieldName, bool wallSection)
    {
        s.foldout = EditorGUILayout.BeginFoldoutHeaderGroup(s.foldout, label);
        if (s.foldout)
        {
            EditorGUI.indentLevel++;
            s.enabled = EditorGUILayout.Toggle("Enabled", s.enabled);

            var so = new SerializedObject(this);
            var prop = so.FindProperty(fieldName).FindPropertyRelative("prefabs");
            EditorGUILayout.PropertyField(prop, new GUIContent("Prefabs"), true);
            so.ApplyModifiedProperties();

            s.density = Mathf.Max(0, EditorGUILayout.IntField(
                new GUIContent("Density", "Number of placement attempts. Failures (overlap, no surface) are skipped."),
                s.density));
            s.scaleRange = EditorGUILayout.Vector2Field(
                new GUIContent("Scale Range (min,max)"), s.scaleRange);
            s.randomYRotation = EditorGUILayout.Toggle("Random Y Rotation", s.randomYRotation);
            s.minSpacing = EditorGUILayout.FloatField(
                new GUIContent("Min Spacing (m)"), s.minSpacing);

            if (wallSection)
            {
                _wallHeightRange = EditorGUILayout.Vector2Field(
                    new GUIContent("Wall Height Range", "Vertical mount range relative to the bottom of the bounds."),
                    _wallHeightRange);
            }

            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
    }

    // ---------- Scatter passes ----------

    bool TryGetBounds(GameObject root, out Bounds bounds)
    {
        bounds = new Bounds();
        bool any = false;
        foreach (var r in root.GetComponentsInChildren<Renderer>())
        {
            if (any) bounds.Encapsulate(r.bounds);
            else { bounds = r.bounds; any = true; }
        }
        if (!any)
        {
            foreach (var c in root.GetComponentsInChildren<Collider>())
            {
                if (any) bounds.Encapsulate(c.bounds);
                else { bounds = c.bounds; any = true; }
            }
        }
        return any;
    }

    Transform GetOrCreateOutputParent(GameObject target)
    {
        if (_outputParentOverride != null) return _outputParentOverride;
        var existing = target.transform.Find(ScatterParentName);
        if (existing != null) return existing;
        var go = new GameObject(ScatterParentName);
        Undo.RegisterCreatedObjectUndo(go, "Create Scatter Parent");
        go.transform.SetParent(target.transform, false);
        return go.transform;
    }

    void ScatterFloors(GameObject target)
    {
        if (!_floors.enabled || _floors.prefabs.Count == 0) return;
        if (!TryGetBounds(target, out var bounds)) { Debug.LogWarning("[PropScatterer] No renderers/colliders under selection."); return; }

        var parent = GetOrCreateOutputParent(target);
        var placed = new List<Vector3>();
        int spawned = 0;
        Vector3? clusterAnchor = null;

        for (int i = 0; i < _floors.density; i++)
        {
            Vector3 sample;
            if (clusterAnchor.HasValue && Random.value < 0.6f)
            {
                var c = Random.insideUnitCircle * 1.2f;
                sample = clusterAnchor.Value + new Vector3(c.x, 0, c.y);
                sample.x = Mathf.Clamp(sample.x, bounds.min.x, bounds.max.x);
                sample.z = Mathf.Clamp(sample.z, bounds.min.z, bounds.max.z);
            }
            else
            {
                sample = new Vector3(
                    Random.Range(bounds.min.x, bounds.max.x),
                    0,
                    Random.Range(bounds.min.z, bounds.max.z));
            }

            var origin = new Vector3(sample.x, bounds.max.y + 0.1f, sample.z);
            if (!Physics.Raycast(origin, Vector3.down, out var hit, bounds.size.y + 0.5f)) continue;
            if (hit.normal.y < 0.7f) continue;
            if (IsInsideOutputParent(hit.collider, parent)) continue;
            if (TooClose(placed, hit.point, _floors.minSpacing)) continue;

            SpawnPrefab(_floors, hit.point, Quaternion.identity, parent);
            placed.Add(hit.point);
            if (!clusterAnchor.HasValue || Random.value < 0.15f) clusterAnchor = hit.point;
            spawned++;
        }

        FinalizeScatter(target, $"Floors: spawned {spawned}/{_floors.density}");
    }

    void ScatterWalls(GameObject target)
    {
        if (!_walls.enabled || _walls.prefabs.Count == 0) return;
        if (!TryGetBounds(target, out var bounds)) { Debug.LogWarning("[PropScatterer] No renderers/colliders under selection."); return; }

        var parent = GetOrCreateOutputParent(target);
        var placed = new List<Vector3>();
        int spawned = 0;

        Vector3[] dirs = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
        float maxRay = Mathf.Max(bounds.size.x, bounds.size.z) + 1f;

        for (int i = 0; i < _walls.density; i++)
        {
            float y = bounds.min.y + Random.Range(_wallHeightRange.x, _wallHeightRange.y);
            if (y < bounds.min.y || y > bounds.max.y) continue;

            var origin = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                y,
                Random.Range(bounds.min.z, bounds.max.z));

            // Shuffle direction order for variety
            for (int s = dirs.Length - 1; s > 0; s--)
            {
                int j = Random.Range(0, s + 1);
                (dirs[s], dirs[j]) = (dirs[j], dirs[s]);
            }

            RaycastHit hit = default;
            bool found = false;
            foreach (var d in dirs)
            {
                if (!Physics.Raycast(origin, d, out hit, maxRay)) continue;
                if (Mathf.Abs(hit.normal.y) > 0.3f) continue;
                if (IsInsideOutputParent(hit.collider, parent)) continue;
                found = true;
                break;
            }
            if (!found) continue;
            if (TooClose(placed, hit.point, _walls.minSpacing)) continue;

            var pos = hit.point + hit.normal * 0.005f;
            var rot = Quaternion.LookRotation(hit.normal, Vector3.up);
            if (_walls.randomYRotation) rot *= Quaternion.Euler(0, Random.Range(-15f, 15f), 0);

            SpawnPrefab(_walls, pos, rot, parent, applyYRotation: false);
            placed.Add(hit.point);
            spawned++;
        }

        FinalizeScatter(target, $"Walls: spawned {spawned}/{_walls.density}");
    }

    void ScatterCeiling(GameObject target)
    {
        if (!_ceiling.enabled || _ceiling.prefabs.Count == 0) return;
        if (!TryGetBounds(target, out var bounds)) { Debug.LogWarning("[PropScatterer] No renderers/colliders under selection."); return; }

        var parent = GetOrCreateOutputParent(target);
        var placed = new List<Vector3>();
        int spawned = 0;

        for (int i = 0; i < _ceiling.density; i++)
        {
            var origin = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                bounds.min.y - 0.1f,
                Random.Range(bounds.min.z, bounds.max.z));
            if (!Physics.Raycast(origin, Vector3.up, out var hit, bounds.size.y + 0.5f)) continue;
            if (hit.normal.y > -0.7f) continue;
            if (IsInsideOutputParent(hit.collider, parent)) continue;
            if (TooClose(placed, hit.point, _ceiling.minSpacing)) continue;

            SpawnPrefab(_ceiling, hit.point, Quaternion.identity, parent);
            placed.Add(hit.point);
            spawned++;
        }

        FinalizeScatter(target, $"Ceiling: spawned {spawned}/{_ceiling.density}");
    }

    void ClearScatter(GameObject target)
    {
        if (target == null) return;
        var parent = _outputParentOverride != null
            ? _outputParentOverride
            : target.transform.Find(ScatterParentName);
        if (parent == null) return;

        for (int i = parent.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(parent.GetChild(i).gameObject);
        Debug.Log($"[PropScatterer] Cleared children of '{parent.name}'.");
    }

    // ---------- Helpers ----------

    void SpawnPrefab(Section s, Vector3 pos, Quaternion rot, Transform parent, bool applyYRotation = true)
    {
        var prefab = s.prefabs[Random.Range(0, s.prefabs.Count)];
        if (prefab == null) return;

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        Undo.RegisterCreatedObjectUndo(go, "Scatter Prop");

        go.transform.position = pos;
        if (applyYRotation && s.randomYRotation)
            rot = rot * Quaternion.Euler(0, Random.Range(0f, 360f), 0);
        go.transform.rotation = rot;

        float min = Mathf.Min(s.scaleRange.x, s.scaleRange.y);
        float max = Mathf.Max(s.scaleRange.x, s.scaleRange.y);
        float scl = Random.Range(min, max);
        go.transform.localScale = prefab.transform.localScale * scl;
    }

    static bool TooClose(List<Vector3> placed, Vector3 p, float minSpacing)
    {
        float sq = minSpacing * minSpacing;
        for (int i = 0; i < placed.Count; i++)
            if ((placed[i] - p).sqrMagnitude < sq) return true;
        return false;
    }

    static bool IsInsideOutputParent(Collider c, Transform outputParent)
    {
        if (c == null || outputParent == null) return false;
        var t = c.transform;
        while (t != null)
        {
            if (t == outputParent) return true;
            t = t.parent;
        }
        return false;
    }

    void FinalizeScatter(GameObject target, string message)
    {
        EditorSceneManager.MarkSceneDirty(target.scene);
        Debug.Log($"[PropScatterer] {message}");
    }
}
