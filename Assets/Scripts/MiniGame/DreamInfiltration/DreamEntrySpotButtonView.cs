using cfg.demo;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.MiniGame.Dream
{
    // 挂在入口「点位按钮」Prefab 模板上；仅负责单实例展示与点击，布局样式在编辑器中配置
    public sealed class DreamEntrySpotButtonView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (label == null)
                label = GetComponentInChildren<TextMeshProUGUI>(true);
        }

        public void BindFromData(DreamEntrySpotDef spot, DreamThemeWeight rolled, int index, System.Action<int> onClicked)
        {
            DreamUISpriteUtil.EnsureWhiteSprite(GetComponent<Image>());
            var rt = (RectTransform)transform;
            rt.anchorMin = rt.anchorMax = new Vector2(spot.Anchor01.x, spot.Anchor01.y);
            rt.anchoredPosition = Vector2.zero;
            gameObject.name = $"Spot_{spot.SpotId}";
            if (label != null)
            {
                label.text = $"{spot.DisplayName}\n<size=75%><#cccccc>{rolled.ThemeDisplayName}</size>";
            }

            if (_button == null) _button = GetComponent<Button>();
            if (_button == null) return;
            _button.onClick.RemoveAllListeners();
            var captured = index;
            _button.onClick.AddListener(() => onClicked?.Invoke(captured));
        }
    }
}
