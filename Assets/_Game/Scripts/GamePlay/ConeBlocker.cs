// ConeBlocker.cs

using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

/// <summary>
/// Cone chặn xe trước mặt cho tới khi count = 0.
/// Mỗi lần có 1 xe ĐỦ khách cùng màu rời bãi -> trừ count theo số khách đã lên.
/// Khi count <= 0: ẩn cone + gỡ chặn.
/// </summary>
public class ConeBlocker : MonoBehaviour
{
    [Header("----- Config -----")]
    public ObjectColor coneColor = ObjectColor.Red;
    [Tooltip("------ Tổng số khách cùng màu cần 'tiêu thụ' để mở khóa cone ------")]
    public int initialCount = 24;

    [Header("------ Refs (optional) -----")]
    public SpriteRenderer spriteRenderer_cone; 
    public TextMeshProUGUI text_CountLabel;          // text hiển thị số trên icon
    public GameObject gfxRoot;                  // phần hiển thị để tween ẩn/hiện
    public Collider2D blockCollider;            // collider đặc (isTrigger = false) để CHẶN xe

    int _remaining;
    bool _active = true;

    void Reset()
    {
        blockCollider = GetComponent<Collider2D>();
        if (blockCollider != null) blockCollider.isTrigger = false;
    }

    void OnEnable()
    {
        
        _remaining = initialCount;
        UpdateLabel();

        // bắt sự kiện xe đủ khách rời bãi
        CarControl.OnCarDepartedFull += OnCarDepartedFull;
    }

    void OnDisable()
    {
        CarControl.OnCarDepartedFull -= OnCarDepartedFull;
    }

    void OnValidate()
    {
        if (Application.isPlaying && text_CountLabel != null)
            text_CountLabel.text = _remaining.ToString();
    }
    
    public void Setup(ConeConfig config, int count)
    {
        coneColor = config.objectColor_cone;
        spriteRenderer_cone.sprite = config.sprite_Cone;
        initialCount = count;
        _remaining = count;
        _active = true;

        UpdateLabel();
    }

    void OnCarDepartedFull(ObjectColor carColor, int boarded)
    {
        if (!_active) return;
        if (carColor != coneColor) return;

        // Trừ đúng số khách màu này đã lên xe (thường = sức chứa xe khi đã "đủ")
        _remaining -= Mathf.Max(0, boarded);
        UpdateLabel();

        if (_remaining <= 0)
            OpenAndVanish();
    }

    void UpdateLabel()
    {
        if (text_CountLabel != null)
            text_CountLabel.text = Mathf.Max(0, _remaining).ToString();
    }

    void OpenAndVanish()
    {
        _active = false;

        // gỡ chặn
        if (blockCollider != null) blockCollider.enabled = false;

        // hiệu ứng biến mất nhẹ
        if (gfxRoot != null)
        {
            gfxRoot.transform.DOKill();
            gfxRoot.transform.DOScale(0f, 0.25f).SetEase(Ease.InBack)
                   .OnComplete(() => gameObject.SetActive(false));
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    // Cho phép cộng dồn count (nếu bạn muốn đặt thêm cone cùng vị trí)
    public void AddQuota(int extra)
    {
        if (!_active)
        {
            // mở lại nếu đã tắt mà ta muốn cộng quota
            _active = true;
            if (blockCollider != null) blockCollider.enabled = true;
            if (gfxRoot != null) gfxRoot.SetActive(true);
        }
        _remaining += Mathf.Max(0, extra);
        UpdateLabel();
    }
}
