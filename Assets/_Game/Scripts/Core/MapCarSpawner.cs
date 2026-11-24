// MapCarSpawner.cs
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class MapCarSpawner : MonoBehaviour
{
    [Header("Config")]
    public MapCarSpawnConfig spawnConfig;
    public bool spawnOnStart = true;
    [Tooltip("Lọc layer/Collider để tránh chồng lên – nên để layer của xe")]
    public LayerMask overlapMask;

    [Header("Tham chiếu (tuỳ chọn)")]
    public ParkingLotManager lotManager; // nếu null sẽ tự tìm

    // cache
    BoxCollider2D box;

    void Awake()
    {
        box = GetComponent<BoxCollider2D>();
        if (!box.isTrigger) box.isTrigger = true;
        if (!lotManager) lotManager = GameController.Instance.parkingLotManager;
    }

    void Start()
    {
        if (spawnOnStart) SpawnAll();
    }

    public void SpawnAll()
    {
        if (spawnConfig == null || spawnConfig.carPacks.Count == 0) return;

        // Vùng spawn (hình chữ nhật) – giống ảnh: tụ vào một cụm giữa bãi
        Vector2 center = (Vector2)transform.position + box.offset;
        Vector2 size = box.size;

        var spawned = new List<Transform>(64);

        foreach (var pack in spawnConfig.carPacks)
        {
            if (!pack.prefab) continue;

            for (int i = 0; i < pack.count; i++)
            {
                // Tìm vị trí hợp lệ (không chồng)
                Vector2 pos;
                const int MAX_TRY = 40;
                int tries = 0;
                float spacing = pack.minSpacing;

                do
                {
                    pos = new Vector2(
                        Random.Range(center.x - size.x * 0.5f, center.x + size.x * 0.5f),
                        Random.Range(center.y - size.y * 0.5f, center.y + size.y * 0.5f)
                    );
                    tries++;
                }
                while ((Physics2D.OverlapCircle(pos, spacing * 0.5f, overlapMask) != null
                        || TooCloseToOthers(pos, spawned, spacing))
                       && tries < MAX_TRY);

                // Nếu thử nhiều lần vẫn chưa có chỗ, nới nhẹ spacing để chắc chắn spawn được
                if (tries >= MAX_TRY)
                {
                    pos = new Vector2(
                        Random.Range(center.x - size.x * 0.5f, center.x + size.x * 0.5f),
                        Random.Range(center.y - size.y * 0.5f, center.y + size.y * 0.5f)
                    );
                }

                // Tạo xe
                var car = Instantiate(pack.prefab, pos, Quaternion.identity, transform);

                // Chọn hướng ngẫu nhiên trong danh sách cho phép
                if (spawnConfig.allowedDirections != null && spawnConfig.allowedDirections.Length > 0)
                {
                    car.carInfo.directions_Car = spawnConfig.allowedDirections[
                        Random.Range(0, spawnConfig.allowedDirections.Length)];
                }

                // Trạng thái ban đầu như ảnh (đứng yên, hiện mũi tên)
                car.isMoving = false;
                car.isParking = false;
                car.isParked = false;
                car.isFulled = false;
                car.SetupVisuals();

                spawned.Add(car.transform);

                // Đăng ký vào ParkingLotManager để các hệ thống khác còn biết
                if (lotManager)
                {
                    lotManager.listCarControl_CarInParkingLot.Add(car);
                }
            }
        }
    }

    bool TooCloseToOthers(Vector2 pos, List<Transform> list, float minSpacing)
    {
        float sqr = minSpacing * minSpacing;
        for (int i = 0; i < list.Count; i++)
        {
            if (((Vector2)list[i].position - pos).sqrMagnitude < sqr) return true;
        }
        return false;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        var b = GetComponent<BoxCollider2D>();
        if (!b) return;
        Gizmos.color = new Color(0, 0.7f, 1f, 0.15f);
        Vector3 c = b.transform.position + (Vector3)b.offset;
        Vector3 s = new Vector3(b.size.x, b.size.y, 0);
        Gizmos.DrawCube(c, s);
        Gizmos.color = new Color(0, 0.7f, 1f, 0.8f);
        Gizmos.DrawWireCube(c, s);
    }
#endif
}
