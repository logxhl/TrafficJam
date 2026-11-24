using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SpriteColliderSync : MonoBehaviour
{
    [Header("----- Components -----")]
    public SpriteRenderer spriteRenderer;
    public PolygonCollider2D polygonCollider2D;      // ưu tiên dùng
    
    private Sprite sprite_previous;

    readonly List<Vector2> _pathBuffer = new List<Vector2>(16);

    public void HandleLateUpdate()
    {
        if (spriteRenderer.sprite == sprite_previous) return;
        ApplyCurrentSpriteShape();
        sprite_previous = spriteRenderer.sprite;
    }

    /// Gọi hàm này thủ công sau khi bạn đổi sprite bằng code để chắc chắn cập nhật ngay.
    public void ApplyCurrentSpriteShape()
    {
        var s = spriteRenderer.sprite;
        if (s == null) return;

        // Có physics shape trong Sprite? -> Áp vào PolygonCollider2D
        int shapeCount = s.GetPhysicsShapeCount();
        if (shapeCount > 0)
        {
            if (!polygonCollider2D.enabled) polygonCollider2D.enabled = true;

            polygonCollider2D.pathCount = shapeCount;
            for (int i = 0; i < shapeCount; i++)
            {
                _pathBuffer.Clear();
                s.GetPhysicsShape(i, _pathBuffer);
                polygonCollider2D.SetPath(i, _pathBuffer);
            }
            // đảm bảo offset = 0 vì đường shape đã theo pivot của sprite
            polygonCollider2D.offset = Vector2.zero;
        }
        else
        {
            if (polygonCollider2D) polygonCollider2D.enabled = false;
        }
    }
}
