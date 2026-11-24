using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Node chuyển hướng. Mỗi node cấu hình: nếu xe đi đến từ hướng A -> đổi sang hướng B.
/// Ví dụ ở WallTop: (Up->Right), (Left->Up) ...
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BoundaryNode : MonoBehaviour
{
    public enum NodeKind
    {
        Wall,        // tường thẳng
        Corner,      // góc (bot-left, bot-right, ...)
        Entrance,    // điểm vào khu “làn parking” (tùy bạn dùng)
        Top,
        LeftDown,
        RightDown
    }

    [Serializable]
    public struct RedirectRule
    {
        public Directions incoming;   // xe đi tới từ hướng nào
        public Directions outgoing;   // đổi sang hướng nào
    }

    [Header("Kind of node")]
    public NodeKind kind = NodeKind.Wall;

    [Header("Redirect rules (incoming -> outgoing)")]
    public List<RedirectRule> rules = new();

    public bool TryGetRedirect(Directions incoming, out Directions outgoing)
    {
        foreach (var r in rules)
        {
            if (r.incoming == incoming)
            {
                outgoing = r.outgoing;
                return true;
            }
        }
        outgoing = incoming;
        return false;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = kind == NodeKind.Corner ? Color.yellow : (kind == NodeKind.Wall ? Color.cyan : new Color(1, .5f, 0));
        Gizmos.DrawWireCube(transform.position, GetComponent<Collider2D>().bounds.size);
    }
#endif
}