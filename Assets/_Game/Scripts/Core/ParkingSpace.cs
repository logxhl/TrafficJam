// Scripts/Gameplay/ParkingSpace.cs

using System;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Ô đỗ phía trên bãi — nơi xe có thể chạy vào (thường khi ra khỏi bãi).
/// Có thể giới hạn theo màu hoặc để trống.
/// </summary>
public class ParkingSpace : MonoBehaviour
{
    [Header("---- Config ---- ")]
    public bool isRestrictByColor = false;
    public ObjectColor objectColor;           // Màu yêu cầu (nếu có)
    public Transform trans_anchor;            // Điểm xe đứng khi vào slot
    public Transform trans_Canvas;          // Điểm gắn UI (nếu có)

    [Header("---- Runtime ----")]
    public SpriteRenderer spriteRenderer_ParkingVip;
    public bool isVipSlot = false;        // Slot VIP (ưu tiên cho xe VIP)
    public bool isLocked = true;           // Slot có bị khoá (không thể nhận xe) không
    public GameObject obj_Locked;         // Hiện khi slot bị khóa (không thể nhận xe)
    public GameObject obj_Unlocked;
    public bool isOccupied;

    public Vector3 v3_WorldPos => trans_anchor ? trans_anchor.position : transform.position;

    /// <summary>
    /// Kiểm tra slot có thể nhận xe này không.
    /// </summary>
    public bool CanAccept(ObjectColor carColor)
    {
        if (isOccupied) return false;
        if (!isRestrictByColor) return true;
        return carColor == objectColor;
    }

    public void Occupy() => isOccupied = true;
    public void Free() => isOccupied = false;

    private void OnMouseUpAsButton()
    {
        Debug.Log("Mouse Up");
        if (isLocked && !GameController.Instance.isLose && Time.timeScale > 0f &&GameController.Instance.isGameStarted)
        {
            GameController.Instance.uiController.ShowSlotExtraPanel();
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = isOccupied ? Color.red : (isRestrictByColor ? GetColorFromEnum(objectColor) : Color.white);
        Gizmos.DrawWireCube(v3_WorldPos, new Vector3(1f, 0.5f, 0.1f));
    }

    private Color GetColorFromEnum(ObjectColor color)
    {
        switch (color)
        {
            case ObjectColor.Red: return new Color(0.9f, 0.2f, 0.2f);
            case ObjectColor.Black: return new Color(0.1f, 0.1f, 0.1f);
            case ObjectColor.Blue: return new Color(0.2f, 0.4f, 0.9f);
            case ObjectColor.Green: return new Color(0.2f, 0.8f, 0.3f);
            case ObjectColor.Yellow: return new Color(1f, 0.9f, 0.3f);
            case ObjectColor.Violet: return new Color(0.7f, 0.3f, 0.8f);
            case ObjectColor.Orange: return new Color(1f, 0.6f, 0.2f);
            default: return Color.white;
        }
    }
#endif
}