using System.Collections.Generic;
using UnityEngine;

/// <summary>
    /// ScriptableObject trung tâm lưu tất cả cấu hình liên quan đến xe:
    /// - Hướng và góc quay của xe
    /// - Cấu hình collider theo hướng
    /// - Số slot và vị trí của xe theo kích thước
    /// - Animation theo màu và kích thước
    /// 
    /// Mỗi Level hoặc dự án có thể chỉ cần 1 CarConfig để tất cả xe tra cứu.
    /// </summary>
    [CreateAssetMenu(menuName = "Config/Create Car Config", fileName = "CarConfig")]
    public class CarConfig : ScriptableObject
    {
        // ------------------- CÁC DANH SÁCH CẤU HÌNH -------------------

        [Header("Car Info Configs")]
        public List<CarInfoConfig> carInfoConfigs;     // Config màu + size + animator

        [Header("Car Size Configs")]
        public List<CarSizeConfig> carSizeConfigs;     // Config số slot, vị trí slot

        [Header("Car Directions")]
        public List<CarDirection> carDirections;       // Config hướng, góc quay, collider,...

        // ------------------- CÁC PHƯƠNG THỨC TRA CỨU -------------------

        /// <summary>
        /// Trả về cấu hình xe theo màu và kích thước (để lấy animator hoặc sprite).
        /// </summary>
        public CarInfoConfig GetCarConfig(ObjectColor color, CarSize size)
        {
            foreach (var config in carInfoConfigs)
            {
                if (config.carColor == color && config.carSize == size)
                    return config;
            }
            Debug.LogWarning($"❌ Không tìm thấy CarInfoConfig cho {color} - {size}");
            return null;
        }

        /// <summary>
        /// Trả về số slot mà xe có kích thước này chiếm.
        /// </summary>
        public int GetCarSlot(CarSize size)
        {
            foreach (var cfg in carSizeConfigs)
            {
                if (cfg.carSize == size)
                    return cfg.slot;
            }
            return 1;
        }

        /// <summary>
        /// Trả về danh sách vị trí slot (tương đối) cho kích thước xe này.
        /// </summary>
        public List<Vector2> GetCarSlotPosition(CarSize size)
        {
            foreach (var cfg in carSizeConfigs)
            {
                if (cfg.carSize == size)
                    return cfg.slotPosition;
            }
            return new List<Vector2> { Vector2.zero };
        }

        /// <summary>
        /// Trả về góc quay (angle) của hướng cụ thể.
        /// </summary>
        public Vector3 GetCarAngle(Directions dir)
        {
            foreach (var cd in carDirections)
            {
                if (cd.directions == dir)
                    return cd.angle;
            }
            return Vector3.zero;
        }

        /// <summary>
        /// Trả về sprite mũi tên tương ứng với hướng.
        /// </summary>
        public Sprite GetArrowSprite(Directions dir)
        {
            foreach (var cd in carDirections)
            {
                if (cd.directions == dir)
                    return cd.arrow;
            }
            return null;
        }

        /// <summary>
        /// Trả về danh sách điểm collider (nếu cần).
        /// </summary>
        public List<Vector2> GetCarCollider(Directions dir)
        {
            foreach (var cd in carDirections)
            {
                if (cd.directions == dir && cd.colliderConfigs != null && cd.colliderConfigs.Count > 0)
                {
                    return cd.colliderConfigs[0].points;
                }
            }
            return new List<Vector2>();
        }
    }

    /// <summary>
    /// Cấu hình collider cơ bản (điểm polygon).
    /// </summary>
    [System.Serializable]
    public class ColliderConfig
    {
        public string name;
        public List<Vector2> points = new(); // Các điểm tạo polygon collider
    }