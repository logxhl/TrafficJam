using System.Numerics;

public static class Params
{
    public const string IS_LEFT = "isLeft";
    public const string IS_RUN = "isRun";
    public const int INT_SHUFFLEPRICE = 1500;
    public const int INT_SORTPRICE = 1500;
    public const int INT_VIPSLOTPRICE = 1500;
    public const int INT_EXTRASLOTPRICE = 2000;
    public const string MAX_SDK_KEY = "lzy1D43gpRPW9tUi5NWKHgdzaEOHtF7TwA6i9VTFBwSD_dPNImwq28W9Gc2dAx9esZW4uiX14oOUbg-RlCtXlT";
    public const string INTERSTITIAL_AD_KEY = "08a4c2ee787148e7";
    public const string REWARDED_AD_KEY = "cfce02e40fcc205d";
    public const string BANNER_AD_KEY = "867edadc74688fae";
    public const string APP_OPEN_AD_KEY = "ca-app-pub-6409857233709298/8480480070";
}

/// <summary>
/// Các hướng mà xe có thể quay về.
/// </summary>
public enum Directions
{
    Up,
    UpLeft,
    UpRight,
    Right,
    Down,
    DownRight,
    DownLeft,
    Left
}

/// <summary>
/// Màu của xe — dùng để phân loại khách hàng và slot.
/// </summary>
public enum ObjectColor
{
    Black,
    Blue,
    Cyan,
    Green,
    Grey,
    Orange,
    Pink,
    Red,
    Violet,
    Yellow
}

/// <summary>
/// Loại xe đặc biệt (VIP, Frozen, v.v.)
/// </summary>
public enum ToolCarSpecialType
{
    None,
    VIP,
    Frozen,
    Mystery
}

/// <summary>
/// Hướng kết nối (nếu xe có liên kết hoặc ghép)
/// </summary>
public enum ConnectedDirections
{
    None,
    Horizontal,
    Vertical
}