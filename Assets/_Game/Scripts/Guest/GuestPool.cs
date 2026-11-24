using System.Collections.Generic;
using UnityEngine;
public class GuestPool : MonoBehaviour
{
    [System.Serializable]
    public struct ColorPrefab
    {
        public ObjectColor color;
        public Guest prefab;
        public Color tint; // dùng khi xài 1 prefab cho nhiều màu
    }

    [Header("Prefabs theo màu (ít nhất 1 cái)")]
    public List<ColorPrefab> prefabs;

    private readonly Dictionary<ObjectColor, Queue<Guest>> free = new();
    private readonly Dictionary<ObjectColor, ColorPrefab> map = new();

    private void Awake()
    {
        foreach (var cp in prefabs)
        {
            map[cp.color] = cp;
            free[cp.color] = new Queue<Guest>();
        }
    }

    public Guest Get(ObjectColor color, Transform parent = null)
    {
        if (!free.ContainsKey(color)) // nếu màu mới chưa khai báo
        {
            var any = prefabs[0];
            map[color]  = any;
            free[color] = new Queue<Guest>();
        }

        Guest guest;
        if (free[color].Count > 0)
        {
            guest = free[color].Dequeue();
            guest.gameObject.SetActive(true);
        }
        else
        {
            var prefab = map[color].prefab != null ? map[color].prefab : prefabs[0].prefab;
            guest = Instantiate(prefab, parent);
        }

        // init màu (nếu dùng 1 prefab)
        guest.Initialize(this, color, map[color].tint);
        return guest;
    }

    public void Release(Guest g)
    {
        g.gameObject.SetActive(false);
        if (!free.ContainsKey(g.objectColor)) free[g.objectColor] = new Queue<Guest>();
        free[g.objectColor].Enqueue(g);
    }
}