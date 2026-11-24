using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Config cho từng loại xe — ví dụ màu đỏ, size vừa thì dùng animator nào.
/// </summary>
[System.Serializable]
public class CarInfoConfig
{
    public ObjectColor carColor;                // Màu xe
    public CarSize carSize;                     // Kích thước
    public RuntimeAnimatorController carAnimation; // Animator tương ứng (Idle / Moving)
    public Sprite sprite_GuestInCar;             // sprite thể hiện khách trong xe (nếu có)
    public Sprite sprite_GarageSign;          // biểu tượng gara hiển thị trên mini map
    public List<Sprite> listSprite_CarDirections; // các sprite con thể hiện hướng

    public CarInfoConfig() { }
}