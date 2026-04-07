
using System;
using My.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.Map.View
{


    public class PauseCloseupWindow : PanelBase, IInputConsumer
    {
        public static PauseCloseupWindow Show(string showName, float duration)
        {

            var panel = UIManager.Instance.ShowPanel("PauseCloseupWindow") as PauseCloseupWindow;
            if (panel == null)
            {
                Debug.LogError("PauseCloseupWindow err");
                return null;
            }

            panel.RefreshData(showName, duration);
            return panel;
        }

        public RectTransform Mask;
        public Image ShowPic;

        public float Duration;
        private float _timer;

        private void Update()
        {
            _timer += Time.deltaTime;
            if(_timer > Duration)
            {
                Hide();
            }
        }


        public void RefreshData(string showName, float duration)
        {
            this.Duration = duration;
            RefreshUI();
        }

        public override void Show()
        {
            base.Show();

            _timer = 0;

            LogicTime.ReleasePause("PauseCloseupWindow");
            LogicTime.RequestPause("PauseCloseupWindow");
        }

        protected void RefreshUI()
        {

        }

        private void OnEnable()
        {
            
        }

        private void OnDisable()
        {
            
        }

        
        public override void Hide()
        {
            base.Hide();
            LogicTime.ReleasePause("PauseCloseupWindow");
        }

        public bool OnConfirm()
        {
            return true;
        }

        public bool OnCancel()
        {
            return true;
        }

        public bool OnNavigate(Vector2 dir)
        {
            return true;
        }

        public bool OnHotkey(string keyName)
        {
            return true;
        }

        public bool OnScroll(float deltaY)
        {
            return true;
        }

        public bool OnClick(int button, Vector2 mousePos)
        {
            return true;
        }

        public bool OnHoldStart(string holdKey)
        {
            return true;
        }
        public bool OnHoldUpdate(string holdKey)
        {
            return true;
        }

        public bool OnHoldingEnd(string holdKey)
        {
            return true;
        }
    }
}