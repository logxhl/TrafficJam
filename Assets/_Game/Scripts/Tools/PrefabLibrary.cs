// Assets/_Game/Level/PrefabLibrary.cs
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PrefabLibrary", menuName = "BusFrenzy/Prefab Library")]
public class PrefabLibrary : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string key;        // ví dụ: "Car_Small", "Car_Medium", "Cone", "ParkingSlot", "VIPSlot"
        public GameObject prefab;
    }

    public List<Entry> entries = new();

    private Dictionary<string, GameObject> _cache;
    public GameObject Get(string key)
    {
        _cache ??= BuildCache();
        return _cache.TryGetValue(key, out var p) ? p : null;
    }

    private Dictionary<string, GameObject> BuildCache()
    {
        var dict = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in entries)
            if (!string.IsNullOrEmpty(e.key) && e.prefab != null)
                dict[e.key] = e.prefab;
        return dict;
    }
}