using UnityEngine;
using DG.Tweening;

/// <summary>
/// Hiệu ứng mũi tên chuyển động (loop) — chỉ hướng mà xe sẽ đi.
/// Thường dùng khi hiển thị preview hướng đi.
/// </summary>
public class CarSelfMoveArrow : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteArrow;
    [SerializeField] private float moveDistance = 0.5f;
    [SerializeField] private float moveTime = 0.6f;

    private Tween tween;

    private void OnEnable()
    {
        StartMove();
    }

    private void OnDisable()
    {
        if (tween != null && tween.IsPlaying()) tween.Kill();
    }

    /// <summary>
    /// Mũi tên lặp lại di chuyển lên xuống để tạo cảm giác động.
    /// </summary>
    private void StartMove()
    {
        if (!spriteArrow) spriteArrow = GetComponent<SpriteRenderer>();

        Vector3 start = transform.localPosition;
        Vector3 end = start + Vector3.up * moveDistance;

        tween = DOTween.Sequence()
            .Append(transform.DOLocalMove(end, moveTime).SetEase(Ease.InOutSine))
            .Append(transform.DOLocalMove(start, moveTime).SetEase(Ease.InOutSine))
            .SetLoops(-1);
    }
}