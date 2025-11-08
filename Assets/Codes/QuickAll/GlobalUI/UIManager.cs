using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager I { get; private set; }
        Stack<IPanel> stack = new Stack<IPanel>();
        float absorbUntilTime = 0f;

        void Awake()
        {
            if (I != null) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Push(IPanel panel)
        {
            stack.Push(panel);
            panel.OnOpen();
            if (panel.ConsumeScrollWhenOpen) absorbUntilTime = Time.unscaledTime + 0.15f;
        }

        public void Pop(IPanel panel = null)
        {
            if (stack.Count == 0) return;
            var top = stack.Peek();
            if (panel != null && !ReferenceEquals(panel, top)) return; // 仅允许弹出顶部
            top.OnClose();
            stack.Pop();
            absorbUntilTime = Time.unscaledTime + 0.15f; // 关闭动画期间也吸附
        }

        public IPanel Top => stack.Count > 0 ? stack.Peek() : null;

        public void RouteScroll(float delta, Vector2 mousePos, System.Action<float> passToCamera)
        {
            var top = Top;
            if (top == null || !top.IsAlive) { passToCamera?.Invoke(delta); return; }

            bool absorbTime = Time.unscaledTime < absorbUntilTime;
            if (top.Modal || absorbTime) { top.OnInputScroll(delta); return; }

            var rect = top.HotRect;
            bool inHot = rect.HasValue && rect.Value.Contains(mousePos);
            if (inHot) { top.OnInputScroll(delta); return; }

            // 非模态且不在热区，允许放行到相机
            passToCamera?.Invoke(delta);
        }

        public void RouteCancel(System.Action passToSystemMenu = null)
        {
            var top = Top;
            if (top == null || !top.IsAlive) { passToSystemMenu?.Invoke(); return; }
            top.OnInputCancel();
        }
    }
}