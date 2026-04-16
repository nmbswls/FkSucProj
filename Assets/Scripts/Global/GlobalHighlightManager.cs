using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace My
{
    public static class GlobalHighlightManager
    {
        // 当需要按原因清除高亮时触发
        public static Action<string> OnClearHighlightByReason;

        // 当需要清除所有高亮时触发
        public static Action OnClearAllHighlights;

        public static void ClearByReason(string reason) => OnClearHighlightByReason?.Invoke(reason);
        public static void ClearAll() => OnClearAllHighlights?.Invoke();


    }
}


