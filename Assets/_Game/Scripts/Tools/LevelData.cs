// Assets/_Game/Level/LevelData.cs
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "BusFrenzy/Level Data (Lite)")]
public class LevelData : ScriptableObject
{
    [Header("Meta")]
    public string levelName = "Level 1";
    public int targetGuestsLeft = 0;

    [Header("Parents (Build To Scene)")]
    public string parkingParentName = "_PARKING";
    public string conesParentName   = "_CONES";
    public string garagesParentName = "_GARAGES";

    // Dùng đúng enum Directions của dự án bạn
    [Serializable]
    public class CarPlacement
    {
        public string key;
        public Vector3 position;

        public Directions facing = Directions.Up;

        public ObjectColor ObjectColor_car = ObjectColor.Red; // tùy bạn có dùng hay không
    }

    [Serializable]
    public class ConePlacement
    {
        public string key = "Cone";
        public Vector3 position;
        public int count = 0;        // nếu cone có đếm
        public ObjectColor coneColor = ObjectColor.Red; // nếu cone có màu
    }

    [Serializable]
    public class GaragePlacement
    {
        public string key = "Garage";
        public Vector3 position;
        [Tooltip("Bật để dùng Cars bên dưới. Tắt để dùng danh sách xe có sẵn trong prefab.")]
        public bool overridePresetCars;
        [Tooltip("Xe ở đầu danh sách ra cửa trước.")]
        public List<GarageCar> cars = new();
    }

    [Serializable]
    public class GarageCar
    {
        public CarSize size = CarSize.Small4Slots;
        public ObjectColor color = ObjectColor.Red;
    }

    [Header("Placements")]
    public List<CarPlacement> cars = new();
    public List<ConePlacement> cones = new();
    public List<GaragePlacement> garages = new();
}
