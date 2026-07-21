using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.CultTech
{
    // 教团 Overview：region 棋子行（教徒 / 压力）
    public sealed class CultRegionRowView : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] Image background;
        [SerializeField] Image selectionBar;
        [SerializeField] TextMeshProUGUI nameText;
        [SerializeField] TextMeshProUGUI linkerText;
        [SerializeField] TextMeshProUGUI pressureText;

        string _regionKey;
        Action<string> _onSelected;
        Action _onHeaderAction;

        public string RegionKey => _regionKey;

        void Awake()
        {
            ResolveRefs();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    if (_onHeaderAction != null)
                    {
                        _onHeaderAction.Invoke();
                        return;
                    }
                    if (!string.IsNullOrEmpty(_regionKey))
                        _onSelected?.Invoke(_regionKey);
                });
            }
        }

        public void Bind(string regionKey, string displayName, long linkerCount, long pressure, int pressureLevel, bool selected, Action<string> onSelected)
        {
            ResolveRefs();
            _regionKey = regionKey ?? string.Empty;
            _onSelected = onSelected;
            _onHeaderAction = null;
            if (nameText != null)
                nameText.text = string.IsNullOrEmpty(displayName) ? _regionKey : displayName;
            if (linkerText != null)
                linkerText.text = $"教徒 {linkerCount}";
            if (pressureText != null)
            {
                pressureText.text = $"压力 {pressure} · Lv.{Mathf.Max(0, pressureLevel)}";
                pressureText.color = ResolvePressureColor(pressureLevel);
            }
            SetSelected(selected);
        }

        public void BindSummary(string areaId, string anchorSummary, string intelSummary, Action onIntel)
        {
            ResolveRefs();
            _regionKey = areaId ?? string.Empty;
            _onSelected = null;
            _onHeaderAction = onIntel;
            if (nameText != null) nameText.text = areaId ?? string.Empty;
            if (linkerText != null) linkerText.text = anchorSummary ?? string.Empty;
            if (pressureText != null) pressureText.text = intelSummary ?? string.Empty;
            SetSelected(false);
            if (button != null)
            {
                button.interactable = onIntel != null;
                var label = button.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null) label.text = "情报";
            }
        }

        static Color ResolvePressureColor(int level)
        {
            if (level <= 0) return new Color(0.62f, 0.68f, 0.62f, 1f);
            if (level == 1) return new Color(0.88f, 0.72f, 0.42f, 1f);
            if (level == 2) return new Color(0.92f, 0.48f, 0.36f, 1f);
            return new Color(1f, 0.3f, 0.36f, 1f);
        }

        public void SetSelected(bool selected)
        {
            ResolveRefs();
            if (background != null)
            {
                background.color = selected
                    ? new Color(0.28f, 0.14f, 0.22f, 0.98f)
                    : new Color(0.14f, 0.09f, 0.15f, 0.96f);
            }
            if (selectionBar != null)
                selectionBar.enabled = selected;
        }

        void ResolveRefs()
        {
            button ??= GetComponent<Button>();
            background ??= GetComponent<Image>();
            if (selectionBar == null)
            {
                var t = transform.Find("SelectionBar");
                if (t != null) selectionBar = t.GetComponent<Image>();
            }
            nameText ??= FindText("NameText");
            linkerText ??= FindText("LinkerText");
            pressureText ??= FindText("PressureText");
        }

        TextMeshProUGUI FindText(string childName)
        {
            var t = transform.Find(childName);
            return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
        }
    }
}
