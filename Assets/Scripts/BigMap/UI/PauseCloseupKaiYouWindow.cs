
using System;
using My.Input;
using My.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.Map.View
{


    public class PauseCloseupKaiYouWindow : PanelBase, IInputConsumer
    {
        public const string ID = "PauseCloseupKaiYouWindow";
        public static PauseCloseupKaiYouWindow Show(long srcEntityId, string showName, float duration)
        {
            var panel = UIManager.Instance.ShowPanel(ID) as PauseCloseupKaiYouWindow;
            if (panel == null)
            {
                Debug.LogError("PauseCloseupWindow err");
                return null;
            }

            panel.RefreshData(duration);
            return panel;
        }

        public RectTransform Mask;
        public Image NormalPic;
        public Image CounterPic;
        public GameObject CounterHint;

        public float Duration;
        public float CounterExtraShow;

        private bool isCounterPeriod = false;
        private bool isCounterSuccess = false;

        private float _timer;

        private bool triggerCounter;

        private void Update()
        {
            _timer += Time.deltaTime;
            if(_timer > Duration + CounterExtraShow)
            {
                HandleInteractFinish();
                return;
            }

            if(_timer > Duration * 0.5f && _timer < Duration * 0.8f)
            {
                isCounterPeriod = true;
            }
            else
            {
                isCounterPeriod = false;
            }

            if(isCounterPeriod && !isCounterSuccess)
            {
                if (!CounterHint.activeSelf)
                {
                    CounterHint.SetActive(true);
                }
            }
            else
            {
                if (CounterHint.activeSelf)
                {
                    CounterHint.SetActive(false);
                }
            }
        }


        public void RefreshData(float duration)
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

            CounterHint.SetActive(false);

            NormalPic.gameObject.SetActive(true);
            CounterPic.gameObject.SetActive(false);

            isCounterPeriod = false;
            isCounterSuccess = false;

            CounterExtraShow = 0;
        }

        private void SwitchToCounterMode()
        {

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


        private void HandleInteractFinish()
        {

            if(isCounterSuccess)
            {

            }

            UIManager.Instance.HidePanel(ID);
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
            if(keyName == QuickPlayerInputBinder.EInputKey.Space.ToString())
            {
                if (isCounterPeriod)
                {
                    isCounterSuccess = true;
                    NormalPic.gameObject.SetActive(false);
                    CounterPic.gameObject.SetActive(true);
                    CounterExtraShow = 3.0f;
                }
            }
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