using System;
using cfg.demo;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    public sealed class BedroomDeploySavePointMarkerView : MonoBehaviour
    {
        const float MarkerSize = 28f;

        [SerializeField] Image icon;
        [SerializeField] TextMeshProUGUI label;

        SavePoint _savePoint;
        Button _button;
        RectTransform _rt;

        public SavePoint BoundSavePoint => _savePoint;
        public event Action<SavePoint> Clicked;

        void Awake()
        {
            _rt = transform as RectTransform;
            _button = GetComponent<Button>();
            if (icon == null && _button != null)
                icon = _button.targetGraphic as Image;
            if (label == null)
                label = GetComponentInChildren<TextMeshProUGUI>(true);

            if (_button != null)
            {
                BedroomDeployMapRowView.ApplySimpleRowButton(_button);
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(OnClick);
            }
        }

        public void Bind(SavePoint sp, Vector2 anchoredNorm01, bool selected)
        {
            _savePoint = sp;
            if (_rt != null)
            {
                var p = new Vector2(Mathf.Clamp01(anchoredNorm01.x), Mathf.Clamp01(anchoredNorm01.y));
                _rt.anchorMin = p;
                _rt.anchorMax = p;
                _rt.pivot = new Vector2(0.5f, 0.5f);
                _rt.anchoredPosition = Vector2.zero;
                _rt.sizeDelta = new Vector2(MarkerSize, MarkerSize);
            }

            if (label != null)
                label.text = sp != null ? sp.DisplayName : string.Empty;

            SetSelected(selected);
        }

        public void SetSelected(bool selected)
        {
            if (icon != null)
                icon.color = selected ? BedroomDeployUiColors.MarkerSelected : BedroomDeployUiColors.MarkerNormal;
        }

        void OnClick()
        {
            if (_savePoint != null)
                Clicked?.Invoke(_savePoint);
        }
    }
}
