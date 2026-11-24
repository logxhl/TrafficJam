using System.Collections.Generic;
using System.Numerics;
using Vector2 = UnityEngine.Vector2;

/// <summary>
/// Cấu hình kích thước xe: mỗi size xe sẽ có số slot chiếm và vị trí tương ứng trên grid.
/// </summary>
[System.Serializable]
public class CarSizeConfig
{
    public CarSize carSize;           // Kích thước xe (Small, Medium, Big)
    public int slot;                  // Số slot xe chiếm
    public List<Vector2> slotPosition; // Danh sách tọa độ (tương đối) mà xe chiếm trong grid

    public CarSizeConfig() { }
}