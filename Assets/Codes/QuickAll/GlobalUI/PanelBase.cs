using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace My.UI
{
    public enum UILayer { Scene = 0, HUD = 1, Popup = 2, Overlay = 3, System = 4 }

    public interface IPanel
    {
        void Setup(object data = null);
        void Show();
        void Hide();
        void Teardown();
        bool IsVisible { get; }
        string PanelId { get; }
        int Layer { get; }
    }

    public interface IRefreshable { void Refresh(); }

    public interface IInputConsumer
    {
        bool OnConfirm();
        bool OnCancel();
        bool OnNavigate(Vector2 dir);
        bool OnHotkey(int index);

        bool OnScroll(float deltaY);
    }

    public interface IFocusable
    {
        bool CanFocus { get; }
        int FocusPriority { get; }
    }

    public abstract class PanelBase : MonoBehaviour, IPanel, IFocusable
    {
        [SerializeField] protected string panelId;
        [SerializeField] protected UILayer layer = UILayer.HUD;
        [SerializeField] protected CanvasGroup canvasGroup;

        public string PanelId => panelId;
        public int Layer => (int)layer;
        public virtual bool IsVisible { get; protected set; }
        public virtual bool CanFocus => IsVisible;
        public virtual int FocusPriority => 0;

        public virtual void Setup(object data = null) { }
        public virtual void Show()
        {
            IsVisible = true;
            if (canvasGroup)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
            gameObject.SetActive(true);
        }
        public virtual void Hide()
        {
            IsVisible = false;
            if (canvasGroup)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            gameObject.SetActive(false);
        }
        public virtual void Teardown() { }
    }

}

