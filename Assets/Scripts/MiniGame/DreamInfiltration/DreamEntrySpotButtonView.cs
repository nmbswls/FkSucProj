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

        public void BindFromData(DreamInfiltrationSpot spot, string rolledThemeDisplayName, int index, System.Action<int> onClicked)
        {
            DreamUISpriteUtil.EnsureWhiteSprite(GetComponent<Image>());
            var rt = (RectTransform)transform;
            rt.anchorMin = rt.anchorMax = new Vector2(spot.AnchorX, spot.AnchorY);
            rt.anchoredPosition = Vector2.zero;
            gameObject.name = $"Spot_{spot.SpotId}";
            if (label != null)
            {
                label.text = $"{spot.DisplayName}\n<size=75%><#cccccc>{rolledThemeDisplayName}</size>";
            }

            if (_button == null) _button = GetComponent<Button>();
            if (_button == null) return;
            _button.onClick.RemoveAllListeners();
            var captured = index;
            _button.onClick.AddListener(() => onClicked?.Invoke(captured));
        }

        public void BindCharacterEntry(
            int entryId,
            string characterName,
            int visibleIndex,
            System.Action<int> onClicked)
        {
            var image = GetComponent<Image>();
            DreamUISpriteUtil.EnsureWhiteSprite(image);
            if (image != null)
            {
                image.color = new Color(0.24f, 0.42f, 0.38f, 0.94f);
            }

            var column = visibleIndex / 4;
            var row = visibleIndex % 4;
            var rt = (RectTransform)transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.88f - column * 0.15f, 0.78f - row * 0.17f);
            rt.anchoredPosition = Vector2.zero;
            gameObject.name = $"CharacterDream_{entryId}";

            if (label != null)
            {
                label.text = $"{characterName}的梦境\n<size=75%><#d4eee5>角色梦境</size>";
            }

            if (_button == null) _button = GetComponent<Button>();
            if (_button == null) return;
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => onClicked?.Invoke(entryId));
        }

        public void BindAbstractGroupEntry(
            AbstractGroup groupCfg,
            AbstractGroupStage stageCfg,
            System.Action onClicked)
        {
            var image = GetComponent<Image>();
            DreamUISpriteUtil.EnsureWhiteSprite(image);
            if (image != null)
            {
                image.color = new Color(0.42f, 0.30f, 0.48f, 0.94f);
            }

            var rt = (RectTransform)transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.12f, 0.18f);
            rt.anchoredPosition = Vector2.zero;
            gameObject.name = $"AbstractGroup_{groupCfg.GroupId}_{stageCfg.Stage}";

            if (label != null)
            {
                var preview = string.IsNullOrEmpty(stageCfg.RewardPreviewDesc)
                    ? stageCfg.DisplayName
                    : stageCfg.RewardPreviewDesc;
                label.text =
                    $"小团体·{groupCfg.DisplayName}\n" +
                    $"<size=75%><#e8d8f5>阶段{stageCfg.Stage} · {stageCfg.DisplayName}</size>\n" +
                    $"<size=70%><#cbb8d8>{preview}</size>";
            }

            if (_button == null) _button = GetComponent<Button>();
            if (_button == null) return;
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => onClicked?.Invoke());
        }
    }
}
