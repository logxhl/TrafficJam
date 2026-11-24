using System.Numerics;

/// <summary>
/// Dữ liệu của từng chiếc xe trong game — chứa mọi thông tin để sinh ra xe.
/// </summary>
[System.Serializable]
public class CarInfo
{
    public ObjectColor objectColor_car;        // Màu xe (enum riêng, ví dụ Red, Blue, Green,...)
    public CarSize carSize;             // Kích thước xe
    public Vector3 v3_CarPosition;         // Vị trí xe trong scene
    public Directions directions_Car;     // Hướng xe
    public CarArea carArea;             // Xe đang ở khu vực nào (map / garage / conveyor)
    public bool isMysteryCar;           // Xe bí ẩn (ẩn màu / cần mở khóa)
    public bool isHasKey;                 // Xe có chìa khóa (để mở xe khác)
    public bool isHasLock;                // Xe bị khóa
    public int int_LockId;                  // ID khóa (dùng cho logic key-lock)
    public bool isHasGiftbox;             // Xe có hộp quà (bonus)
    public int int_GroupIndex;              // Nhóm (để phân biệt group puzzle)
    public ToolCarSpecialType toolCarSpecialType; // Loại đặc biệt (nếu có, ví dụ VIP, Frozen,...)
    public int int_ConnectedKey;            // Khóa kết nối (cho xe ghép)
    public ConnectedDirections connectedDirection; // Hướng kết nối (nếu xe liên kết)
    public Vector3 v3_ConnectedPoint;      // Điểm nối giữa các xe
    public bool isConnectedMoveFirst;     // Xe này di chuyển trước hay sau khi ghép

    /// <summary>
    /// Hàm sao chép dữ liệu xe — dùng khi clone hoặc lưu tạm.
    /// </summary>
    public CarInfo Copy()
    {
        return (CarInfo)MemberwiseClone();
    }
}