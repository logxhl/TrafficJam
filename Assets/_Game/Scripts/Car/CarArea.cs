/// <summary>
/// Khu vực mà xe đang nằm trong game.
/// Dùng để biết xe đang ở trên bản đồ, trong garage hay trong băng chuyền (nếu có).
/// </summary>
public enum CarArea
{
    OnMap = 0,         // Xe đang ở trên bản đồ chính
    InGarage = 1,      // Xe trong garage
    InConveyorBelt = 2 // Xe trên băng chuyền
}