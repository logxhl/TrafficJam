#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class IsometricLevelGenerator : EditorWindow
{
    [MenuItem("Bus Frenzy/Isometric Level Generator")]
    public static void Open() => GetWindow<IsometricLevelGenerator>("Isometric Level Generator");

    // Primary isometric basis vectors (derived from Level 2 reference layout)
    public static readonly Vector2 AxisUpRight = new Vector2(1.25f, 1.05f);
    public static readonly Vector2 AxisUpLeft  = new Vector2(-1.25f, 1.05f);

    public static Vector2 UVToWorld(Vector2 center, float u, float v)
    {
        return center + u * AxisUpRight + v * AxisUpLeft;
    }

    public LevelData targetLevel;
    public int targetCarCount = 16;
    public Vector2 clusterCenter = new Vector2(0f, -2.6f);

    private void OnGUI()
    {
        GUILayout.Label("Isometric Level Generator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Tạo level xe xếp sát nhau (Isometric Diamond Grid) tương tự Level 1 & Level 2 mẫu.\n" +
            "Trục UpRight (+1.25, +1.05) và UpLeft (-1.25, +1.05) đảm bảo xe đan chéo, khít nhau và không thưa thớt.",
            MessageType.Info);

        EditorGUILayout.Space(8);
        targetLevel = (LevelData)EditorGUILayout.ObjectField("Target LevelData", targetLevel, typeof(LevelData), false);
        targetCarCount = EditorGUILayout.IntSlider("Target Car Count", targetCarCount, 8, 30);
        clusterCenter = EditorGUILayout.Vector2Field("Cluster Center", clusterCenter);

        EditorGUILayout.Space(8);
        if (GUILayout.Button("Generate Level Vào Target LevelData", GUILayout.Height(30)))
        {
            if (targetLevel == null)
            {
                EditorUtility.DisplayDialog("Lỗi", "Vui lòng chọn Target LevelData!", "OK");
                return;
            }
            GenerateLevel(targetLevel, targetCarCount, clusterCenter);
            EditorUtility.SetDirty(targetLevel);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Thành công", $"Đã tạo {targetLevel.cars.Count} xe khít nhau cho {targetLevel.name}!", "OK");
        }

        EditorGUILayout.Space(12);
        GUI.backgroundColor = new Color(0.4f, 0.9f, 0.5f);
        if (GUILayout.Button("⚡ BATCH GEN TOÀN BỘ LEVEL 3 -> LEVEL 10 ⚡", GUILayout.Height(40)))
        {
            if (EditorUtility.DisplayDialog("Xác nhận", "Bạn có chắc muốn gen lại toàn bộ Level 3 -> Level 10 theo phong cách khít nhau như Level 2?", "Đồng ý", "Hủy"))
            {
                GenerateAllLevels3To10();
            }
        }
        GUI.backgroundColor = Color.white;
    }

    public static void GenerateAllLevels3To10()
    {
        string folder = "Assets/_Game/SO/LevelData";
        for (int lv = 3; lv <= 10; lv++)
        {
            string path = $"{folder}/Level{lv}.asset";
            var data = AssetDatabase.LoadAssetAtPath<LevelData>(path);
            if (data == null)
            {
                Debug.LogWarning($"Không tìm thấy {path}");
                continue;
            }

            switch (lv)
            {
                case 3: GenerateLevel3(data); break;
                case 4: GenerateLevel4(data); break;
                case 5: GenerateLevel5(data); break;
                case 6: GenerateLevel6(data); break;
                case 7: GenerateLevel7(data); break;
                case 8: GenerateLevel8(data); break;
                case 9: GenerateLevel9(data); break;
                case 10: GenerateLevel10(data); break;
            }

            EditorUtility.SetDirty(data);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("✅ Đã gen thành công Level 3 -> Level 10 theo phong cách Isometric xe sát nhau!");
    }

    #region Level Specific Builders

    // LEVEL 3: 10 xe (Diamond khít nhỏ, 5 xe UpLeft, 5 xe UpRight)
    public static void GenerateLevel3(LevelData lvl)
    {
        lvl.levelName = "Level 3 - Compact Diamond";
        lvl.cars.Clear(); lvl.cones.Clear(); lvl.garages.Clear();
        Vector2 center = new Vector2(0f, -2.6f);

        // Right wing (facing UpRight = 2)
        AddCar(lvl, "carsmall",  UVToWorld(center, -0.9f, -1.5f), Directions.UpRight, ObjectColor.Red);
        AddCar(lvl, "carsmall",  UVToWorld(center,  0.1f, -1.5f), Directions.UpRight, ObjectColor.Yellow);
        AddCar(lvl, "carmedium", UVToWorld(center,  1.3f, -1.5f), Directions.UpRight, ObjectColor.Blue);
        AddCar(lvl, "carsmall",  UVToWorld(center, -0.4f, -2.5f), Directions.UpRight, ObjectColor.Yellow);
        AddCar(lvl, "carmedium", UVToWorld(center,  0.8f, -2.5f), Directions.UpRight, ObjectColor.Red);

        // Left wing (facing UpLeft = 1)
        AddCar(lvl, "carsmall",  UVToWorld(center, -1.5f, -0.9f), Directions.UpLeft, ObjectColor.Blue);
        AddCar(lvl, "carsmall",  UVToWorld(center, -1.5f,  0.1f), Directions.UpLeft, ObjectColor.Red);
        AddCar(lvl, "carmedium", UVToWorld(center, -1.5f,  1.3f), Directions.UpLeft, ObjectColor.Yellow);
        AddCar(lvl, "carsmall",  UVToWorld(center, -2.5f, -0.4f), Directions.UpLeft, ObjectColor.Blue);
        AddCar(lvl, "carbig",    UVToWorld(center, -2.5f,  0.9f), Directions.UpLeft, ObjectColor.Red);
    }

    // LEVEL 4: 12 xe (Interlocking Chevron)
    public static void GenerateLevel4(LevelData lvl)
    {
        lvl.levelName = "Level 4 - Interlocking Chevron";
        lvl.cars.Clear(); lvl.cones.Clear(); lvl.garages.Clear();
        Vector2 center = new Vector2(0f, -2.6f);

        // UpRight lanes (6 xe)
        AddCar(lvl, "carsmall",  UVToWorld(center, -1.2f, -1.4f), Directions.UpRight, ObjectColor.Green);
        AddCar(lvl, "carmedium", UVToWorld(center,  0.0f, -1.4f), Directions.UpRight, ObjectColor.Red);
        AddCar(lvl, "carbig",    UVToWorld(center,  1.4f, -1.4f), Directions.UpRight, ObjectColor.Blue);
        AddCar(lvl, "carsmall",  UVToWorld(center, -0.6f, -2.4f), Directions.UpRight, ObjectColor.Yellow);
        AddCar(lvl, "carmedium", UVToWorld(center,  0.6f, -2.4f), Directions.UpRight, ObjectColor.Green);
        AddCar(lvl, "carsmall",  UVToWorld(center,  1.8f, -2.4f), Directions.UpRight, ObjectColor.Red);

        // UpLeft lanes (6 xe)
        AddCar(lvl, "carsmall",  UVToWorld(center, -1.4f, -1.2f), Directions.UpLeft, ObjectColor.Yellow);
        AddCar(lvl, "carmedium", UVToWorld(center, -1.4f,  0.0f), Directions.UpLeft, ObjectColor.Green);
        AddCar(lvl, "carsmall",  UVToWorld(center, -1.4f,  1.3f), Directions.UpLeft, ObjectColor.Blue);
        AddCar(lvl, "carbig",    UVToWorld(center, -2.4f, -0.5f), Directions.UpLeft, ObjectColor.Red);
        AddCar(lvl, "carsmall",  UVToWorld(center, -2.4f,  0.8f), Directions.UpLeft, ObjectColor.Yellow);
        AddCar(lvl, "carmedium", UVToWorld(center, -2.4f,  2.0f), Directions.UpLeft, ObjectColor.Green);

        // Garage
        lvl.garages.Add(new LevelData.GaragePlacement { key = "garagetopright", position = new Vector3(2.2f, -6.0f, 0) });
    }

    // LEVEL 5: 14 xe (Herringbone Cluster + 1 Cone)
    public static void GenerateLevel5(LevelData lvl)
    {
        lvl.levelName = "Level 5 - Herringbone Jam";
        lvl.cars.Clear(); lvl.cones.Clear(); lvl.garages.Clear();
        Vector2 center = new Vector2(0f, -2.6f);

        // UpRight (7 xe)
        AddCar(lvl, "carsmall",  UVToWorld(center, -1.5f, -1.2f), Directions.UpRight, ObjectColor.Red);
        AddCar(lvl, "carmedium", UVToWorld(center, -0.2f, -1.2f), Directions.UpRight, ObjectColor.Cyan);
        AddCar(lvl, "carbig",    UVToWorld(center,  1.2f, -1.2f), Directions.UpRight, ObjectColor.Yellow);
        AddCar(lvl, "carsmall",  UVToWorld(center, -0.8f, -2.2f), Directions.UpRight, ObjectColor.Blue);
        AddCar(lvl, "carsmall",  UVToWorld(center,  0.2f, -2.2f), Directions.UpRight, ObjectColor.Red);
        AddCar(lvl, "carmedium", UVToWorld(center,  1.4f, -2.2f), Directions.UpRight, ObjectColor.Cyan);
        AddCar(lvl, "carsmall",  UVToWorld(center,  0.5f, -3.2f), Directions.UpRight, ObjectColor.Blue);

        // UpLeft (6 xe) + DownRight (1 xe)
        AddCar(lvl, "carsmall",  UVToWorld(center, -1.2f, -1.5f), Directions.UpLeft, ObjectColor.Cyan);
        AddCar(lvl, "carmedium", UVToWorld(center, -1.2f, -0.2f), Directions.UpLeft, ObjectColor.Blue);
        AddCar(lvl, "carsmall",  UVToWorld(center, -1.2f,  1.1f), Directions.UpLeft, ObjectColor.Red);
        AddCar(lvl, "carbig",    UVToWorld(center, -2.2f, -0.8f), Directions.UpLeft, ObjectColor.Yellow);
        AddCar(lvl, "carsmall",  UVToWorld(center, -2.2f,  0.5f), Directions.UpLeft, ObjectColor.Cyan);
        AddCar(lvl, "carmedium", UVToWorld(center, -2.2f,  1.7f), Directions.UpLeft, ObjectColor.Blue);
        AddCar(lvl, "carsmall",  UVToWorld(center, -0.2f, -3.0f), Directions.DownRight, ObjectColor.Red);

        // Cone
        lvl.cones.Add(new LevelData.ConePlacement { key = "cone", position = new Vector3(3.6f, -1.4f, 0), count = 8, coneColor = ObjectColor.Red });
    }

    // LEVEL 6: 16 xe (Dual Corridor Jam + 1 Cone + 1 Garage)
    public static void GenerateLevel6(LevelData lvl)
    {
        lvl.levelName = "Level 6 - Dual Corridor Jam";
        lvl.cars.Clear(); lvl.cones.Clear(); lvl.garages.Clear();
        Vector2 center = new Vector2(0f, -2.6f);

        // Right Wing (8 xe)
        AddCar(lvl, "carsmall",  UVToWorld(center, -1.4f, -1.5f), Directions.UpRight, ObjectColor.Pink);
        AddCar(lvl, "carmedium", UVToWorld(center, -0.1f, -1.5f), Directions.UpRight, ObjectColor.Green);
        AddCar(lvl, "carsmall",  UVToWorld(center,  1.2f, -1.5f), Directions.UpRight, ObjectColor.Yellow);
        AddCar(lvl, "carbig",    UVToWorld(center,  2.4f, -1.5f), Directions.UpRight, ObjectColor.Blue);
        AddCar(lvl, "carsmall",  UVToWorld(center, -0.7f, -2.5f), Directions.UpRight, ObjectColor.Pink);
        AddCar(lvl, "carmedium", UVToWorld(center,  0.5f, -2.5f), Directions.UpRight, ObjectColor.Red);
        AddCar(lvl, "carsmall",  UVToWorld(center,  1.7f, -2.5f), Directions.UpRight, ObjectColor.Green);
        AddCar(lvl, "carmedium", UVToWorld(center,  0.0f, -3.5f), Directions.UpRight, ObjectColor.Yellow);

        // Left Wing (7 xe UpLeft + 1 xe DownRight)
        AddCar(lvl, "carsmall",  UVToWorld(center, -1.5f, -1.4f), Directions.UpLeft, ObjectColor.Green);
        AddCar(lvl, "carsmall",  UVToWorld(center, -1.5f, -0.2f), Directions.UpLeft, ObjectColor.Pink);
        AddCar(lvl, "carmedium", UVToWorld(center, -1.5f,  1.0f), Directions.UpLeft, ObjectColor.Red);
        AddCar(lvl, "carbig",    UVToWorld(center, -1.5f,  2.3f), Directions.UpLeft, ObjectColor.Yellow);
        AddCar(lvl, "carsmall",  UVToWorld(center, -2.5f, -0.7f), Directions.UpLeft, ObjectColor.Blue);
        AddCar(lvl, "carmedium", UVToWorld(center, -2.5f,  0.5f), Directions.UpLeft, ObjectColor.Pink);
        AddCar(lvl, "carbig",    UVToWorld(center, -2.5f,  1.8f), Directions.UpLeft, ObjectColor.Green);
        AddCar(lvl, "carsmall",  UVToWorld(center, -3.2f, -0.3f), Directions.DownRight, ObjectColor.Red);

        // Cone & Garage
        lvl.cones.Add(new LevelData.ConePlacement { key = "cone", position = new Vector3(-3.8f, -0.8f, 0), count = 10, coneColor = ObjectColor.Yellow });
        lvl.garages.Add(new LevelData.GaragePlacement { key = "garageleft", position = new Vector3(-3.8f, -5.5f, 0) });
    }

    // LEVEL 7: 18 xe (Tri-Wing Diamond + 2 Cones + 1 Garage)
    public static void GenerateLevel7(LevelData lvl)
    {
        lvl.levelName = "Level 7 - Tri-Wing Diamond";
        lvl.cars.Clear(); lvl.cones.Clear(); lvl.garages.Clear();
        Vector2 center = new Vector2(0f, -2.6f);

        // Right Wing (9 xe)
        AddCar(lvl, "carsmall",  UVToWorld(center, -1.8f, -1.3f), Directions.UpRight, ObjectColor.Green);
        AddCar(lvl, "carmedium", UVToWorld(center, -0.6f, -1.3f), Directions.UpRight, ObjectColor.Blue);
        AddCar(lvl, "carsmall",  UVToWorld(center,  0.6f, -1.3f), Directions.UpRight, ObjectColor.Red);
        AddCar(lvl, "carbig",    UVToWorld(center,  1.8f, -1.3f), Directions.UpRight, ObjectColor.Orange);
        AddCar(lvl, "carsmall",  UVToWorld(center, -1.1f, -2.3f), Directions.UpRight, ObjectColor.Yellow);
        AddCar(lvl, "carmedium", UVToWorld(center,  0.1f, -2.3f), Directions.UpRight, ObjectColor.Green);
        AddCar(lvl, "carsmall",  UVToWorld(center,  1.2f, -2.3f), Directions.UpRight, ObjectColor.Blue);
        AddCar(lvl, "carmedium", UVToWorld(center, -0.4f, -3.3f), Directions.UpRight, ObjectColor.Orange);
        AddCar(lvl, "carsmall",  UVToWorld(center,  0.8f, -3.3f), Directions.UpRight, ObjectColor.Red);

        // Left Wing (8 xe UpLeft + 1 xe DownRight)
        AddCar(lvl, "carsmall",  UVToWorld(center, -1.3f, -1.8f), Directions.UpLeft, ObjectColor.Orange);
        AddCar(lvl, "carmedium", UVToWorld(center, -1.3f, -0.6f), Directions.UpLeft, ObjectColor.Yellow);
        AddCar(lvl, "carsmall",  UVToWorld(center, -1.3f,  0.6f), Directions.UpLeft, ObjectColor.Green);
        AddCar(lvl, "carbig",    UVToWorld(center, -1.3f,  1.8f), Directions.UpLeft, ObjectColor.Blue);
        AddCar(lvl, "carsmall",  UVToWorld(center, -2.3f, -1.1f), Directions.UpLeft, ObjectColor.Red);
        AddCar(lvl, "carmedium", UVToWorld(center, -2.3f,  0.1f), Directions.UpLeft, ObjectColor.Yellow);
        AddCar(lvl, "carsmall",  UVToWorld(center, -2.3f,  1.2f), Directions.UpLeft, ObjectColor.Orange);
        AddCar(lvl, "carbig",    UVToWorld(center, -3.2f, -0.2f), Directions.UpLeft, ObjectColor.Green);
        AddCar(lvl, "carmedium", UVToWorld(center, -0.2f, -3.8f), Directions.DownRight, ObjectColor.Blue);

        // Cones & Garage
        lvl.cones.Add(new LevelData.ConePlacement { key = "cone", position = new Vector3(-2.0f, 0.6f, 0), count = 8, coneColor = ObjectColor.Green });
        lvl.cones.Add(new LevelData.ConePlacement { key = "cone", position = new Vector3(3.2f, -5.2f, 0), count = 8, coneColor = ObjectColor.Blue });
        lvl.garages.Add(new LevelData.GaragePlacement { key = "garagetopright", position = new Vector3(0.0f, -5.8f, 0) });
    }

    // LEVEL 8: 20 xe (Dense Hourglass Jam + 2 Cones + 1 Garage)
    public static void GenerateLevel8(LevelData lvl)
    {
        lvl.levelName = "Level 8 - Dense Hourglass Jam";
        lvl.cars.Clear(); lvl.cones.Clear(); lvl.garages.Clear();
        Vector2 center = new Vector2(0f, -2.6f);

        // Right (10 xe)
        AddCar(lvl, "carsmall",  UVToWorld(center, -2.0f, -1.2f), Directions.UpRight, ObjectColor.Violet);
        AddCar(lvl, "carmedium", UVToWorld(center, -0.8f, -1.2f), Directions.UpRight, ObjectColor.Yellow);
        AddCar(lvl, "carsmall",  UVToWorld(center,  0.4f, -1.2f), Directions.UpRight, ObjectColor.Red);
        AddCar(lvl, "carbig",    UVToWorld(center,  1.6f, -1.2f), Directions.UpRight, ObjectColor.Green);
        AddCar(lvl, "carsmall",  UVToWorld(center, -1.3f, -2.2f), Directions.UpRight, ObjectColor.Blue);
        AddCar(lvl, "carmedium", UVToWorld(center, -0.1f, -2.2f), Directions.UpRight, ObjectColor.Violet);
        AddCar(lvl, "carmedium", UVToWorld(center,  1.1f, -2.2f), Directions.UpRight, ObjectColor.Yellow);
        AddCar(lvl, "carsmall",  UVToWorld(center,  2.2f, -2.2f), Directions.UpRight, ObjectColor.Red);
        AddCar(lvl, "carbig",    UVToWorld(center, -0.5f, -3.2f), Directions.UpRight, ObjectColor.Green);
        AddCar(lvl, "carsmall",  UVToWorld(center,  0.9f, -3.2f), Directions.UpRight, ObjectColor.Blue);

        // Left (9 xe UpLeft + 1 xe DownRight)
        AddCar(lvl, "carsmall",  UVToWorld(center, -1.2f, -2.0f), Directions.UpLeft, ObjectColor.Yellow);
        AddCar(lvl, "carmedium", UVToWorld(center, -1.2f, -0.8f), Directions.UpLeft, ObjectColor.Violet);
        AddCar(lvl, "carsmall",  UVToWorld(center, -1.2f,  0.4f), Directions.UpLeft, ObjectColor.Red);
        AddCar(lvl, "carbig",    UVToWorld(center, -1.2f,  1.6f), Directions.UpLeft, ObjectColor.Green);
        AddCar(lvl, "carsmall",  UVToWorld(center, -2.2f, -1.3f), Directions.UpLeft, ObjectColor.Blue);
        AddCar(lvl, "carmedium", UVToWorld(center, -2.2f, -0.1f), Directions.UpLeft, ObjectColor.Yellow);
        AddCar(lvl, "carmedium", UVToWorld(center, -2.2f,  1.1f), Directions.UpLeft, ObjectColor.Violet);
        AddCar(lvl, "carsmall",  UVToWorld(center, -2.2f,  2.2f), Directions.UpLeft, ObjectColor.Red);
        AddCar(lvl, "carbig",    UVToWorld(center, -3.2f,  0.2f), Directions.UpLeft, ObjectColor.Green);
        AddCar(lvl, "carsmall",  UVToWorld(center, -2.8f, -2.0f), Directions.DownRight, ObjectColor.Violet);

        // Cones & Garage
        lvl.cones.Add(new LevelData.ConePlacement { key = "cone", position = new Vector3(4.2f, -2.8f, 0), count = 10, coneColor = ObjectColor.Violet });
        lvl.cones.Add(new LevelData.ConePlacement { key = "cone", position = new Vector3(-4.2f, -2.8f, 0), count = 10, coneColor = ObjectColor.Red });
        lvl.garages.Add(new LevelData.GaragePlacement { key = "garageright", position = new Vector3(3.6f, -5.6f, 0) });
    }

    // LEVEL 9: 22 xe (Complex Interlock + 2 Cones + 1 Garage)
    public static void GenerateLevel9(LevelData lvl)
    {
        lvl.levelName = "Level 9 - Complex Interlock";
        lvl.cars.Clear(); lvl.cones.Clear(); lvl.garages.Clear();
        Vector2 center = new Vector2(0f, -2.6f);

        // Right (11 xe)
        AddCar(lvl, "carsmall",  UVToWorld(center, -2.2f, -1.2f), Directions.UpRight, ObjectColor.Pink);
        AddCar(lvl, "carmedium", UVToWorld(center, -1.0f, -1.2f), Directions.UpRight, ObjectColor.Yellow);
        AddCar(lvl, "carsmall",  UVToWorld(center,  0.2f, -1.2f), Directions.UpRight, ObjectColor.Blue);
        AddCar(lvl, "carbig",    UVToWorld(center,  1.5f, -1.2f), Directions.UpRight, ObjectColor.Violet);
        AddCar(lvl, "carsmall",  UVToWorld(center, -1.6f, -2.2f), Directions.UpRight, ObjectColor.Green);
        AddCar(lvl, "carmedium", UVToWorld(center, -0.3f, -2.2f), Directions.UpRight, ObjectColor.Red);
        AddCar(lvl, "carsmall",  UVToWorld(center,  0.9f, -2.2f), Directions.UpRight, ObjectColor.Pink);
        AddCar(lvl, "carmedium", UVToWorld(center,  2.1f, -2.2f), Directions.UpRight, ObjectColor.Yellow);
        AddCar(lvl, "carbig",    UVToWorld(center, -0.9f, -3.2f), Directions.UpRight, ObjectColor.Blue);
        AddCar(lvl, "carsmall",  UVToWorld(center,  0.4f, -3.2f), Directions.UpRight, ObjectColor.Violet);
        AddCar(lvl, "carsmall",  UVToWorld(center,  1.6f, -3.2f), Directions.UpRight, ObjectColor.Green);

        // Left (10 xe UpLeft + 1 xe DownRight)
        AddCar(lvl, "carsmall",  UVToWorld(center, -1.2f, -2.2f), Directions.UpLeft, ObjectColor.Red);
        AddCar(lvl, "carmedium", UVToWorld(center, -1.2f, -1.0f), Directions.UpLeft, ObjectColor.Pink);
        AddCar(lvl, "carsmall",  UVToWorld(center, -1.2f,  0.2f), Directions.UpLeft, ObjectColor.Yellow);
        AddCar(lvl, "carbig",    UVToWorld(center, -1.2f,  1.5f), Directions.UpLeft, ObjectColor.Blue);
        AddCar(lvl, "carsmall",  UVToWorld(center, -2.2f, -1.6f), Directions.UpLeft, ObjectColor.Violet);
        AddCar(lvl, "carmedium", UVToWorld(center, -2.2f, -0.3f), Directions.UpLeft, ObjectColor.Green);
        AddCar(lvl, "carsmall",  UVToWorld(center, -2.2f,  0.9f), Directions.UpLeft, ObjectColor.Red);
        AddCar(lvl, "carmedium", UVToWorld(center, -2.2f,  2.1f), Directions.UpLeft, ObjectColor.Pink);
        AddCar(lvl, "carbig",    UVToWorld(center, -3.2f, -0.8f), Directions.UpLeft, ObjectColor.Yellow);
        AddCar(lvl, "carmedium", UVToWorld(center, -3.2f,  0.6f), Directions.UpLeft, ObjectColor.Blue);
        AddCar(lvl, "carsmall",  UVToWorld(center, -1.8f, -3.2f), Directions.DownRight, ObjectColor.Violet);

        // Cones & Garage
        lvl.cones.Add(new LevelData.ConePlacement { key = "cone", position = new Vector3(4.5f, -1.8f, 0), count = 12, coneColor = ObjectColor.Pink });
        lvl.cones.Add(new LevelData.ConePlacement { key = "cone", position = new Vector3(-1.4f, -6.2f, 0), count = 10, coneColor = ObjectColor.Yellow });
        lvl.garages.Add(new LevelData.GaragePlacement { key = "garagetopleft", position = new Vector3(-3.2f, -5.8f, 0) });
    }

    // LEVEL 10: 26 xe (Grand Master Diamond Jam + 2 Cones + 1 Garage)
    public static void GenerateLevel10(LevelData lvl)
    {
        lvl.levelName = "Level 10 - Grand Master Diamond Jam";
        lvl.cars.Clear(); lvl.cones.Clear(); lvl.garages.Clear();
        Vector2 center = new Vector2(0f, -2.6f);

        // Right (13 xe)
        AddCar(lvl, "carsmall",  UVToWorld(center, -2.4f, -1.2f), Directions.UpRight, ObjectColor.Orange);
        AddCar(lvl, "carmedium", UVToWorld(center, -1.2f, -1.2f), Directions.UpRight, ObjectColor.Violet);
        AddCar(lvl, "carsmall",  UVToWorld(center,  0.0f, -1.2f), Directions.UpRight, ObjectColor.Green);
        AddCar(lvl, "carbig",    UVToWorld(center,  1.3f, -1.2f), Directions.UpRight, ObjectColor.Yellow);
        AddCar(lvl, "carsmall",  UVToWorld(center,  2.6f, -1.2f), Directions.UpRight, ObjectColor.Red);

        AddCar(lvl, "carsmall",  UVToWorld(center, -1.8f, -2.2f), Directions.UpRight, ObjectColor.Blue);
        AddCar(lvl, "carmedium", UVToWorld(center, -0.6f, -2.2f), Directions.UpRight, ObjectColor.Orange);
        AddCar(lvl, "carsmall",  UVToWorld(center,  0.6f, -2.2f), Directions.UpRight, ObjectColor.Violet);
        AddCar(lvl, "carmedium", UVToWorld(center,  1.8f, -2.2f), Directions.UpRight, ObjectColor.Green);

        AddCar(lvl, "carbig",    UVToWorld(center, -1.1f, -3.2f), Directions.UpRight, ObjectColor.Yellow);
        AddCar(lvl, "carsmall",  UVToWorld(center,  0.2f, -3.2f), Directions.UpRight, ObjectColor.Red);
        AddCar(lvl, "carmedium", UVToWorld(center,  1.4f, -3.2f), Directions.UpRight, ObjectColor.Blue);
        AddCar(lvl, "carsmall",  UVToWorld(center,  0.8f, -4.2f), Directions.UpRight, ObjectColor.Orange);

        // Left (11 xe UpLeft + 2 xe DownRight)
        AddCar(lvl, "carsmall",  UVToWorld(center, -1.2f, -2.4f), Directions.UpLeft, ObjectColor.Violet);
        AddCar(lvl, "carmedium", UVToWorld(center, -1.2f, -1.2f), Directions.UpLeft, ObjectColor.Green);
        AddCar(lvl, "carsmall",  UVToWorld(center, -1.2f,  0.0f), Directions.UpLeft, ObjectColor.Yellow);
        AddCar(lvl, "carbig",    UVToWorld(center, -1.2f,  1.3f), Directions.UpLeft, ObjectColor.Red);
        AddCar(lvl, "carsmall",  UVToWorld(center, -1.2f,  2.6f), Directions.UpLeft, ObjectColor.Blue);

        AddCar(lvl, "carsmall",  UVToWorld(center, -2.2f, -1.8f), Directions.UpLeft, ObjectColor.Orange);
        AddCar(lvl, "carmedium", UVToWorld(center, -2.2f, -0.6f), Directions.UpLeft, ObjectColor.Violet);
        AddCar(lvl, "carsmall",  UVToWorld(center, -2.2f,  0.6f), Directions.UpLeft, ObjectColor.Green);
        AddCar(lvl, "carmedium", UVToWorld(center, -2.2f,  1.8f), Directions.UpLeft, ObjectColor.Yellow);

        AddCar(lvl, "carbig",    UVToWorld(center, -3.2f, -1.0f), Directions.UpLeft, ObjectColor.Red);
        AddCar(lvl, "carbig",    UVToWorld(center, -3.2f,  0.6f), Directions.UpLeft, ObjectColor.Blue);

        AddCar(lvl, "carsmall",  UVToWorld(center, -2.0f, -3.5f), Directions.DownRight, ObjectColor.Violet);
        AddCar(lvl, "carmedium", UVToWorld(center, -3.4f, -2.5f), Directions.DownRight, ObjectColor.Orange);

        // Cones & Garage
        lvl.cones.Add(new LevelData.ConePlacement { key = "cone", position = new Vector3(5.1f, -2.0f, 0), count = 12, coneColor = ObjectColor.Orange });
        lvl.cones.Add(new LevelData.ConePlacement { key = "cone", position = new Vector3(-4.4f, -4.2f, 0), count = 12, coneColor = ObjectColor.Violet });
        lvl.garages.Add(new LevelData.GaragePlacement { key = "garagetopright", position = new Vector3(-0.4f, -6.0f, 0) });
    }

    #endregion

    #region Generic Generator Helper

    public static void GenerateLevel(LevelData lvl, int count, Vector2 center)
    {
        lvl.cars.Clear();
        int half = count / 2;
        int remaining = count - half;

        ObjectColor[] colors = { ObjectColor.Red, ObjectColor.Blue, ObjectColor.Yellow, ObjectColor.Green, ObjectColor.Cyan };

        // Generate UpRight wing
        float vLane = -1.2f;
        float uPos = -1.0f;
        for (int i = 0; i < half; i++)
        {
            string key = (i % 3 == 0) ? "carbig" : (i % 2 == 0) ? "carmedium" : "carsmall";
            float step = key == "carbig" ? 1.4f : key == "carmedium" ? 1.2f : 0.95f;
            AddCar(lvl, key, UVToWorld(center, uPos, vLane), Directions.UpRight, colors[i % colors.Length]);
            uPos += step;
            if (uPos > 2.2f)
            {
                uPos = -0.8f;
                vLane -= 1.0f;
            }
        }

        // Generate UpLeft wing
        float uLane = -1.2f;
        float vPos = -1.0f;
        for (int i = 0; i < remaining; i++)
        {
            string key = (i % 3 == 1) ? "carbig" : (i % 2 == 1) ? "carmedium" : "carsmall";
            float step = key == "carbig" ? 1.4f : key == "carmedium" ? 1.2f : 0.95f;
            AddCar(lvl, key, UVToWorld(center, uLane, vPos), Directions.UpLeft, colors[(i + 2) % colors.Length]);
            vPos += step;
            if (vPos > 2.2f)
            {
                vPos = -0.8f;
                uLane -= 1.0f;
            }
        }
    }

    private static void AddCar(LevelData lvl, string key, Vector2 pos, Directions facing, ObjectColor color)
    {
        lvl.cars.Add(new LevelData.CarPlacement
        {
            key = key,
            position = new Vector3((float)Math.Round(pos.x, 2), (float)Math.Round(pos.y, 2), 0f),
            facing = facing,
            ObjectColor_car = color
        });
    }

    #endregion
}
#endif