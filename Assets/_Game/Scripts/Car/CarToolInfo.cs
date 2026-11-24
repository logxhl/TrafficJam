/// <summary>
/// Thông tin tạm thời khi làm việc với nhiều xe cùng lúc (group logic, tool...).
/// </summary>
[System.Serializable]
public class CarToolInfo
{
    public int group;           // Nhóm của xe
    public CarControl car;      // Tham chiếu trực tiếp đến component CarControl
    public CarArea area;        // Xe đang ở khu vực nào
    public string areaName;     // Tên khu vực (ví dụ “ParkingLot”, “Garage”)
    public CarInfo carInfo;     // Dữ liệu chi tiết của xe (màu, hướng, kích thước,...)

    public CarToolInfo() { }
}