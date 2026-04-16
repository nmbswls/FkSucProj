
using System;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{ 

    public class ItemCountChooseBox : PanelWithInput
    {


        public static ItemCountChooseBox Show(long maxVal, long initVal = 1,  Action<long> confirmCallback = null, Action cancelCallback = null)
        {

            var panel = UIManager.Instance.ShowPanel("ItemCountChooseBox") as ItemCountChooseBox;
            if(panel == null)
            {
                Debug.LogError("ItemCountChooseBox err");
                return null;
            }

            panel.RefreshData(maxVal, initVal, confirmCallback, cancelCallback);
            return panel;
        }

        public RectTransform Mask;

        public TMP_InputField inputField;
        public Slider slider;

        public Button btnMin;
        public Button btnMax;
        public Button btnMinus;
        public Button btnPlus;
        public Button btnConfirm;
        public Button btnCancel;

        // 回调
        private Action<long> onConfirm;
        private Action onCancel;

        private long minValue = 1;
        private long maxValue = 1;
        private int step = 1;
        private long quantity;


        private bool _updating;

        private void Awake()
        {
            
        }

        public void RefreshData(long maxVal, long initVal = 1, Action<long> confirmCallback = null, Action cancelCallback = null)
        {
            minValue = 1;
            maxValue = maxVal;

            quantity = Math.Clamp(initVal, minValue, maxValue);

            onConfirm = confirmCallback;
            onCancel = cancelCallback;

            RefreshUI();

        }

        public override void Show()
        {
            base.Show();

            // 首次刷新
            RefreshUI();
        }

        private void OnEnable()
        {
            // 绑定事件
            btnMinus.onClick.AddListener(() => ChangeBy(-step));
            btnPlus.onClick.AddListener(() => ChangeBy(+step));
            //btnMin.onClick.AddListener(() => SetQuantity(minValue));
            btnMax.onClick.AddListener(() => SetQuantity(maxValue));

            inputField.onEndEdit.AddListener(OnInputEndEdit);
            slider.onValueChanged.AddListener(OnSliderChanged);

            btnConfirm.onClick.AddListener(() =>
            {
                onConfirm?.Invoke(quantity);
                Close();
            });
            btnCancel.onClick.AddListener(() =>
            {
                onCancel?.Invoke();
                Close();
            });
        }

        private void OnDisable()
        {
            // 清理事件（避免重复绑定）
            btnMinus.onClick.RemoveAllListeners();
            btnPlus.onClick.RemoveAllListeners();
            //btnMin.onClick.RemoveAllListeners();
            btnMax.onClick.RemoveAllListeners();
            btnConfirm.onClick.RemoveAllListeners();
            btnCancel.onClick.RemoveAllListeners();
            inputField.onEndEdit.RemoveAllListeners();
            slider.onValueChanged.RemoveAllListeners();
        }

        private void OnInputEndEdit(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                SetQuantity(minValue);
                return;
            }
            if (int.TryParse(text, out var val))
            {
                SetQuantity(val);
            }
            else
            {
                // 非数字，回到当前合法值
                RefreshUI();
            }
        }

        private void OnSliderChanged(float val)
        {
            SetQuantity(Mathf.RoundToInt(val));
        }


        private void ChangeBy(int delta)
        {
            SetQuantity(quantity + delta);
        }

        private void SetQuantity(long val)
        {
            val = Math.Clamp(val, minValue, maxValue);
            if (val == quantity) { RefreshButtons(); return; }
            quantity = val;
            RefreshUI();
        }

        private void RefreshUI()
        {
            if (_updating) return;
            _updating = true;

            // 初始化控件
            slider.minValue = minValue;
            slider.maxValue = maxValue;
            slider.wholeNumbers = true;

            // 同步控件
            if (inputField.text != quantity.ToString())
                inputField.text = quantity.ToString();

            if (Mathf.Abs(slider.value - quantity) > 0.001f)
                slider.value = quantity;

            RefreshButtons();

            _updating = false;
        }

        private void RefreshButtons()
        {
            btnMinus.interactable = quantity > minValue;
            btnPlus.interactable = quantity < maxValue;
            //btnMin.interactable = quantity != minValue;
            btnMax.interactable = quantity != maxValue;
        }

        public void Close()
        {
            minValue = 1;
            maxValue = 1;

            Hide();
        }
        
    }
}
