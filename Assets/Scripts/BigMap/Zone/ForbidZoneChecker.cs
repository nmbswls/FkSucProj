using System;
using System.Collections;
using System.Collections.Generic;
using cfg.demo;
using UnityEngine;

namespace My
{

    public class ForbidZoneChecker : MonoBehaviour
    {
        // 至少有一个 Collider 需在 Zone 层，以便 OverlapPoint 能扫到并关联本组件；再用 InnerCol/OuterCol.OverlapPoint 判定内外圈（可不与 Zone 层一致）。
        public Collider2D InnerCol;
        public Collider2D OuterCol;
        
        public List<CommonCheckCond> EnableCondition = new();
    }
}


