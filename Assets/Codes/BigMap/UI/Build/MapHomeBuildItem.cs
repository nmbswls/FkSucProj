

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    public class MapHomeBuildItem : MonoBehaviour
    {
        public Image icon;
        public Image background;
        public TextMeshProUGUI title;

        private HomePlaceableObject? data;
        public Action onClick;

        public Color baseColor;
        public Color selectedColor;

        private bool isSelected;

        void Awake()
        {
            var btn = GetComponentInChildren<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => onClick?.Invoke());
            }
        }

        public void Bind(HomePlaceableObject d, bool selected)
        {
            data = d;
            title.text = d.name;
            icon.sprite = d.sprite;
            SetSelected(selected);
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;
            // 改变颜色或样式
            if (background != null)
            {
                background.color = selected ? selectedColor : baseColor;
            }
            // 也可额外改变文字或图标的风格
            if (title != null)
                title.color = selected ? Color.black : Color.white;
            if (icon != null)
                icon.color = selected ? Color.white : new Color(1f, 1f, 1f, 0.85f);
        }
    }
}