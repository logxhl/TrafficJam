using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dữ liệu về hướng của xe — lưu sprite mũi tên, góc quay, collider theo hướng đó.
/// Mỗi hướng có thể có collider khác nhau (ví dụ: xe nằm ngang / dọc).
/// </summary>
[System.Serializable]
public class CarDirection
{
    public Directions directions;               // Hướng (Up, Down, Left, Right)
    public Vector3 angle;                       // Góc quay tương ứng trong world
    public Sprite arrow;                        // Sprite hiển thị mũi tên hướng đi
    public Sprite question;                     // Sprite dạng “?” nếu bị khóa màu
    public List<ColliderConfig> colliderConfigs; // Cấu hình collider theo hướng

    public CarDirection() { }
}