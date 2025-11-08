using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    // IPanel.cs
    public interface IPanel
    {
        bool Modal { get; }
        bool ConsumeScrollWhenOpen { get; }
        Rect? HotRect { get; } // 屏幕坐标Rect，左下为(0,0)
        bool IsAlive { get; }
        void OnOpen();
        void OnClose();
        void OnInputScroll(float delta);
        void OnInputCancel();
    }

    // PanelBase.cs
    public abstract class PanelBase : MonoBehaviour, IPanel
    {
        [SerializeField] protected bool modal = false;
        [SerializeField] protected bool consumeScrollWhenOpen = true;
        public bool Modal => modal;
        public bool ConsumeScrollWhenOpen => consumeScrollWhenOpen;
        public virtual Rect? HotRect => null; // ScreenPanel/WorldPanel去实现
        public bool IsAlive { get; protected set; } = true;

        public virtual void OnOpen() { IsAlive = true; gameObject.SetActive(true); }
        public virtual void OnClose() { IsAlive = false; gameObject.SetActive(false); }
        public abstract void OnInputScroll(float delta);
        public abstract void OnInputCancel();
    }

}

