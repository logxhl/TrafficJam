// Assets/_Game/Level/Editor/MapLayoutTool.cs
#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class MapLayoutTool : EditorWindow
{
    [MenuItem("Bus Frenzy/Map Layout Tool")]
    public static void Open() => GetWindow<MapLayoutTool>("Map Layout");

    LevelData level;
    PrefabLibrary library;

    // Palette
    int selectedIndex = 0;
    string[] keys = Array.Empty<string>();
    float brushRotZ = 0f;
    bool snapGrid = true;
    float gridSize = 0.5f;

    // Parents
    Transform parkingParent, conesParent, garagesParent;

    // Modes
    enum PlaceMode { Car, Cone, Garage }
    PlaceMode mode = PlaceMode.Car;

    // Car brush
    Directions brushFacing = Directions.Up;
    bool brushFlipY = false;

    void OnEnable()  => SceneView.duringSceneGui += OnSceneGUI;
    void OnDisable() => SceneView.duringSceneGui -= OnSceneGUI;

    void OnGUI()
    {
        if (EditorApplication.isPlaying)
        {
            var controller = GameController.Instance;
            var builder = controller != null ? controller.runtimeLevelBuilder : null;
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField("LevelData", builder != null ? builder.levelData_Current : null,
                    typeof(LevelData), false);

            EditorGUILayout.HelpBox("Chỉnh vị trí xe trong Scene rồi lưu vào LevelData của màn đang chạy.", MessageType.Info);
            using (new EditorGUI.DisabledScope(builder == null || builder.levelData_Current == null))
                if (GUILayout.Button("Save Car Positions to LevelData"))
                    builder.SaveCarPositionsToLevelData();
            return;
        }

        level   = (LevelData)EditorGUILayout.ObjectField("LevelData", level, typeof(LevelData), false);
        library = (PrefabLibrary)EditorGUILayout.ObjectField("PrefabLibrary", library, typeof(PrefabLibrary), false);

        // prepare keys
        if (library != null)
        {
            keys = library.entries.Where(e => !string.IsNullOrEmpty(e.key)).Select(e => e.key).ToArray();
            selectedIndex = Mathf.Clamp(selectedIndex, 0, Math.Max(0, keys.Length - 1));
            if (keys.Length == 0) EditorGUILayout.HelpBox("PrefabLibrary chưa có entry.", MessageType.Warning);
        }

        EditorGUILayout.Space(6);
        using (new EditorGUILayout.VerticalScope("box"))
        {
            GUILayout.Label("Parents in Scene", EditorStyles.boldLabel);
            parkingParent = (Transform)EditorGUILayout.ObjectField("Parking", parkingParent, typeof(Transform), true);
            conesParent   = (Transform)EditorGUILayout.ObjectField("Cones", conesParent, typeof(Transform), true);
            garagesParent = (Transform)EditorGUILayout.ObjectField("Garages", garagesParent, typeof(Transform), true);

            if (GUILayout.Button("Auto-Find Parents By Name"))
            {
                parkingParent = GameObject.Find(level?.parkingParentName ?? "_PARKING")?.transform;
                conesParent   = GameObject.Find(level?.conesParentName   ?? "_CONES")?.transform;
                garagesParent = GameObject.Find(level?.garagesParentName ?? "_GARAGES")?.transform;
            }
        }

        EditorGUILayout.Space(6);
        using (new EditorGUILayout.VerticalScope("box"))
        {
            GUILayout.Label("Palette", EditorStyles.boldLabel);
            mode = (PlaceMode)EditorGUILayout.EnumPopup("Mode", mode);
            if (keys.Length > 0) selectedIndex = EditorGUILayout.Popup("Key", selectedIndex, keys);

            brushRotZ = EditorGUILayout.FloatField("Rotation Z", brushRotZ);

            using (new EditorGUI.DisabledScope(mode != PlaceMode.Car))
            {
                brushFacing = (Directions)EditorGUILayout.EnumPopup("Facing", brushFacing);
                brushFlipY = EditorGUILayout.ToggleLeft("Flip Y (F)", brushFlipY);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                snapGrid = EditorGUILayout.ToggleLeft("Snap Grid (G)", snapGrid, GUILayout.Width(140));
                gridSize = EditorGUILayout.FloatField("Grid Size", gridSize);
            }

            EditorGUILayout.HelpBox("Click = đặt | Shift+Click = xóa | R = xoay 90° | Q/E = đổi hướng | F = Flip Y", MessageType.None);
        }

        EditorGUILayout.Space(6);
        using (new EditorGUILayout.HorizontalScope())
        {
            GUI.enabled = level != null && library != null;
            if (GUILayout.Button("Build To Scene")) BuildToScene();
            if (GUILayout.Button("Bake From Scene")) BakeFromScene();
            GUI.enabled = true;
        }

        if (GUILayout.Button("Validate")) Validate();

        EditorGUILayout.Space(4);
        if (level != null)
        {
            level.levelName = EditorGUILayout.TextField("Level Name", level.levelName);
            level.targetGuestsLeft = EditorGUILayout.IntField("Target Guests Left", level.targetGuestsLeft);
            EditorUtility.SetDirty(level);
        }
    }

    void OnSceneGUI(SceneView sv)
    {
        if (EditorApplication.isPlaying || library == null || keys.Length == 0) return;
        var e = Event.current; if (e == null) return;

        // hotkeys
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.R) { brushRotZ = Mathf.Repeat(brushRotZ + 90f, 360f); Repaint(); }
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.G) { snapGrid = !snapGrid; Repaint(); }
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.F) { brushFlipY = !brushFlipY; Repaint(); }
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Q) { CycleFacing(-1); Repaint(); }
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.E) { CycleFacing(+1); Repaint(); }

        // mouse ray → plane
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        var plane = new Plane(Vector3.forward, Vector3.zero);
        if (!plane.Raycast(ray, out float d)) return;
        Vector3 pos = ray.GetPoint(d);
        if (snapGrid) { pos.x = Mathf.Round(pos.x / gridSize) * gridSize; pos.y = Mathf.Round(pos.y / gridSize) * gridSize; }

        // preview
        Handles.color = Color.white;
        Handles.DrawWireDisc(pos, Vector3.forward, gridSize * 0.5f);

        // place / erase
        if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0)
        {
            bool erase = e.shift;
            HandlePlacement(pos, brushRotZ, erase);
            e.Use();
        }
    }

    void HandlePlacement(Vector3 pos, float rotZ, bool erase)
    {
        string key = (keys.Length > 0) ? keys[selectedIndex] : null;
        if (string.IsNullOrEmpty(key)) return;

        Transform parent = mode switch
        {
            PlaceMode.Car    => parkingParent,
            PlaceMode.Cone   => conesParent,
            PlaceMode.Garage => garagesParent,
            _ => null
        };
        if (parent == null) { Debug.LogWarning("Parent chưa gán cho mode: " + mode); return; }

        if (erase)
        {
            var nearest = parent.Cast<Transform>().OrderBy(t => Vector2.Distance(pos, t.position)).FirstOrDefault();
            if (nearest != null && Vector2.Distance(pos, nearest.position) <= gridSize * 0.75f)
                Undo.DestroyObjectImmediate(nearest.gameObject);
            return;
        }

        var prefab = library.Get(key);
        if (prefab == null) { Debug.LogWarning("Missing prefab for key: " + key); return; }

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        Undo.RegisterCreatedObjectUndo(go, "Place " + key);
        go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0,0,rotZ));
        go.name = key;

        // nếu là Car → gán hướng vào CarControl, set flipY
        if (mode == PlaceMode.Car)
        {
            var cc = go.GetComponent<CarControl>();
            if (cc != null)
            {
                if (cc.carInfo != null) cc.carInfo.directions_Car = brushFacing;
                try { cc.SetupVisuals(); } catch { }
                if (cc.spriteRenderer_CarBody != null) cc.spriteRenderer_CarBody.flipY = brushFlipY;
            }
        }

        Selection.activeGameObject = go;
    }

    void BuildToScene()
    {
        if (level == null || library == null) return;

        // ensure parents
        parkingParent = EnsureParent(level.parkingParentName);
        conesParent   = EnsureParent(level.conesParentName);
        garagesParent = EnsureParent(level.garagesParentName);

        ClearChildren(parkingParent);
        ClearChildren(conesParent);
        ClearChildren(garagesParent);

        foreach (var c in level.cars)
            PlaceFromKey(c.key, parkingParent, c.position, c.facing);

        foreach (var co in level.cones)
            PlaceFromKey(co.key, conesParent, co.position);

        foreach (var g in level.garages)
            PlaceFromKey(g.key, garagesParent, g.position);
    }

    void BakeFromScene()
    {
        if (level == null) { Debug.LogError("Chưa chọn LevelData"); return; }
        level.cars.Clear(); level.cones.Clear(); level.garages.Clear();

        if (parkingParent != null)
        {
            foreach (Transform t in parkingParent)
            {
                var cp = new LevelData.CarPlacement
                {
                    key = t.name,
                    position = t.position,
                };

                var cc = t.GetComponent<CarControl>();
                if (cc != null)
                {
                    cp.facing = (cc.carInfo != null) ? cc.carInfo.directions_Car : Directions.Up;
                }

                level.cars.Add(cp);
            }
        }

        if (conesParent != null)
        {
            foreach (Transform t in conesParent)
            {
                level.cones.Add(new LevelData.ConePlacement
                {
                    key = t.name, position = t.position
                });
            }
        }

        if (garagesParent != null)
        {
            foreach (Transform t in garagesParent)
            {
                level.garages.Add(new LevelData.GaragePlacement
                {
                    key = t.name, position = t.position
                });
            }
        }

        EditorUtility.SetDirty(level);
        AssetDatabase.SaveAssets();
        Debug.Log("Bake xong vào LevelData: " + level.name);
    }

    void Validate()
    {
        if (level == null) return;
        int missing = 0;
        foreach (var k in level.cars.Select(c => c.key)
                 .Concat(level.cones.Select(c => c.key))
                 .Concat(level.garages.Select(g => g.key)))
        {
            if (string.IsNullOrEmpty(k)) { missing++; }
            else if (library != null && library.Get(k) == null) Debug.LogWarning("Key không có trong PrefabLibrary: " + k);
        }
        if (missing > 0) Debug.LogWarning($"{missing} items chưa có key."); else Debug.Log("Validate OK.");
    }

    Transform EnsureParent(string name)
    {
        var go = GameObject.Find(name);
        if (go == null) go = new GameObject(name);
        return go.transform;
    }

    void PlaceFromKey(string key, Transform parent, Vector3 pos, Directions? facing = null)
    {
        var pf = library.Get(key);
        if (pf == null) { Debug.LogWarning("Missing prefab for key: " + key); return; }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(pf, parent);
        Undo.RegisterCreatedObjectUndo(go, "Build " + key);
        go.name = key;

        var cc = go.GetComponent<CarControl>();
        if (cc != null)
        {
            if (facing.HasValue && cc.carInfo != null) cc.carInfo.directions_Car = facing.Value;
            try { cc.SetupVisuals(); } catch { }
        }
    }

    static void ClearChildren(Transform t)
    {
        if (t == null) return;
        for (int i = t.childCount - 1; i >= 0; i--) Undo.DestroyObjectImmediate(t.GetChild(i).gameObject);
    }

    void CycleFacing(int dir)
    {
        int n = Enum.GetValues(typeof(Directions)).Length;
        int v = ((int)brushFacing + dir) % n;
        if (v < 0) v += n;
        brushFacing = (Directions)v;
    }
}
#endif
