// MapCarSpawnConfig.cs
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MapCarSpawnConfig", menuName = "ParkingLot/Map Car Spawn Config")]
public class MapCarSpawnConfig : ScriptableObject
{
    [Serializable]
    public class CarPack
    {
        public CarControl prefab;
        [Min(0)] public int count = 5;           // số xe của prefab này
        [Min(0.1f)] public float minSpacing = 0.8f; // khoảng cách tối thiểu giữa các xe
    }

    [Header("List các loại xe sẽ spawn")]
    public List<CarPack> carPacks = new();

    [Header("Hướng spawn khả dụng")]
    public Directions[] allowedDirections = new[]
    {
        Directions.UpLeft, Directions.UpRight,
        Directions.DownLeft, Directions.DownRight
    };
}