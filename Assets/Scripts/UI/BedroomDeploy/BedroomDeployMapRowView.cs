using System;
using cfg.demo;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    public sealed class BedroomDeployMapRowView : MonoBehaviour
    {
        [SerializeField] Image bg;
        [SerializeField] TextMeshProUGUI title;

        MapAreaInfo _map;
        Button _button;

        public event Action<MapAreaInfo> Clicked;

        void Awake()
        {
            _button = GetComponent<Button>();
            if (bg == null && _button != null)
                bg = _button.targetGraphic as Image;
            if (title == null)
                title = GetComponentInChildren<TextMeshProUGUI>(true);

            if (_button != null)
            {
                ApplySimpleRowButton(_button);
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(OnClick);
            }
        }

        public void Bind(MapAreaInfo map, bool selected)
        {
            _map = map;
            if (title != null)
                title.text = map != null ? map.Name : string.Empty;
            SetSelected(selected);
        }

        public void SetSelected(bool selected)
        {
            if (bg != null)
                bg.color = selected ? BedroomDeployUiColors.RowBgSelected : BedroomDeployUiColors.RowBgNormal;
        }

        void OnClick()
        {
            if (_map != null)
                Clicked?.Invoke(_map);
        }

        public static void ApplySimpleRowButton(Button row)
        {
            if (row == null) return;
            row.transition = Selectable.Transition.None;
            var c = row.colors;
            c.fadeDuration = 0f;
            c.colorMultiplier = 1f;
            c.highlightedColor = c.normalColor;
            c.pressedColor = c.normalColor;
            c.selectedColor = c.normalColor;
            row.colors = c;
        }
    }
}
