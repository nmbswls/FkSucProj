using cfg.demo;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.MiniGame.Dream
{
    // 地图小人 / 角色入口按钮；选中高亮由面板驱动
    public sealed class DreamEntrySpotButtonView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private Image frameImage;

        private Button _button;
        private Image _bodyImage;
        private Color _baseColor = Color.white;
        private bool _selected;

        public DreamEntrySourceKind SourceKind { get; private set; }
        public string PasserbyId { get; private set; } = "";
        public int CharacterEntryId { get; private set; }
        public bool IsAbstractGroup { get; private set; }

        private void Awake()
        {
            Cache();
        }

        void Cache()
        {
            _button = GetComponent<Button>();
            _bodyImage = GetComponent<Image>();
            if (label == null)
                label = GetComponentInChildren<TextMeshProUGUI>(true);
            if (frameImage == null)
            {
                var frameTr = transform.Find("Frame");
                if (frameTr != null) frameImage = frameTr.GetComponent<Image>();
            }
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;
            ApplyVisual();
        }

        void ApplyVisual()
        {
            if (_bodyImage == null) Cache();
            if (_bodyImage != null)
            {
                _bodyImage.color = _selected
                    ? Color.Lerp(_baseColor, Color.white, 0.35f)
                    : _baseColor;
            }
        }

        public void BindPasserbyFigure(
            DreamPasserby cfg,
            DreamPasserbyDailyEntryPersist entry,
            System.Action<DreamEntrySpotButtonView> onSelected)
        {
            Cache();
            SourceKind = DreamEntrySourceKind.PasserbyEntry;
            PasserbyId = cfg?.PasserbyId ?? entry?.PasserbyId ?? "";
            CharacterEntryId = 0;
            IsAbstractGroup = false;

            DreamUISpriteUtil.EnsureWhiteSprite(_bodyImage);
            _baseColor = ColorForGrade(cfg != null ? cfg.Grade : ECommonGrade.Common, cfg?.VisualVariant ?? 0);
            if (frameImage != null)
            {
                frameImage.gameObject.SetActive(false);
            }

            var rt = (RectTransform)transform;
            rt.sizeDelta = new Vector2(56f, 72f);
            rt.anchorMin = rt.anchorMax = new Vector2(
                entry != null ? entry.AnchorX : 0.5f,
                entry != null ? entry.AnchorY : 0.5f);
            rt.anchoredPosition = Vector2.zero;
            gameObject.name = $"Passerby_{PasserbyId}";

            if (label != null)
            {
                label.text = "●";
                label.fontSize = 28f;
                label.alignment = TextAlignmentOptions.Center;
            }

            WireClick(() => onSelected?.Invoke(this));
            SetSelected(false);
        }

        public void BindAbstractGroupFigure(
            AbstractGroup groupCfg,
            AbstractGroupStage stageCfg,
            float anchorX,
            float anchorY,
            System.Action<DreamEntrySpotButtonView> onSelected)
        {
            Cache();
            SourceKind = DreamEntrySourceKind.AbstractGroupEntry;
            PasserbyId = "";
            CharacterEntryId = 0;
            IsAbstractGroup = true;

            DreamUISpriteUtil.EnsureWhiteSprite(_bodyImage);
            _baseColor = new Color(0.42f, 0.30f, 0.48f, 0.96f);
            EnsureFrame();
            if (frameImage != null)
            {
                frameImage.gameObject.SetActive(true);
                DreamUISpriteUtil.EnsureWhiteSprite(frameImage);
                frameImage.color = new Color(0.85f, 0.72f, 0.95f, 0.95f);
            }

            var rt = (RectTransform)transform;
            rt.sizeDelta = new Vector2(96f, 120f);
            rt.anchorMin = rt.anchorMax = new Vector2(anchorX, anchorY);
            rt.anchoredPosition = Vector2.zero;
            gameObject.name = $"AbstractGroup_{groupCfg?.GroupId}_{stageCfg?.Stage}";

            if (label != null)
            {
                label.text = "团";
                label.fontSize = 34f;
                label.alignment = TextAlignmentOptions.Center;
            }

            WireClick(() => onSelected?.Invoke(this));
            SetSelected(false);
        }

        public void BindCharacterEntry(
            int entryId,
            string characterName,
            int visibleIndex,
            System.Action<DreamEntrySpotButtonView> onSelected)
        {
            Cache();
            SourceKind = DreamEntrySourceKind.CharacterEntry;
            PasserbyId = "";
            CharacterEntryId = entryId;
            IsAbstractGroup = false;

            DreamUISpriteUtil.EnsureWhiteSprite(_bodyImage);
            _baseColor = new Color(0.24f, 0.42f, 0.38f, 0.94f);
            if (frameImage != null) frameImage.gameObject.SetActive(false);

            var column = visibleIndex / 4;
            var row = visibleIndex % 4;
            var rt = (RectTransform)transform;
            rt.sizeDelta = new Vector2(140f, 64f);
            rt.anchorMin = rt.anchorMax = new Vector2(0.88f - column * 0.15f, 0.78f - row * 0.17f);
            rt.anchoredPosition = Vector2.zero;
            gameObject.name = $"CharacterDream_{entryId}";

            if (label != null)
            {
                label.text = DreamEntryRewardSemantics.BuildCharacterDetail(characterName)
                    .Split('\n')[0];
                label.fontSize = 18f;
                label.alignment = TextAlignmentOptions.Center;
            }

            WireClick(() => onSelected?.Invoke(this));
            SetSelected(false);
        }

        void EnsureFrame()
        {
            if (frameImage != null) return;
            var go = new GameObject("Frame", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            go.transform.SetAsFirstSibling();
            var frt = (RectTransform)go.transform;
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = Vector2.one;
            frt.offsetMin = new Vector2(-6f, -6f);
            frt.offsetMax = new Vector2(6f, 6f);
            frameImage = go.GetComponent<Image>();
            frameImage.raycastTarget = false;
        }

        void WireClick(System.Action action)
        {
            if (_button == null) _button = GetComponent<Button>();
            if (_button == null) return;
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => action?.Invoke());
        }

        static Color ColorForGrade(ECommonGrade grade, int visualVariant)
        {
            // 小黑人底色 + 品级微差
            var baseTone = visualVariant switch
            {
                1 => new Color(0.14f, 0.14f, 0.16f, 0.96f),
                2 => new Color(0.18f, 0.16f, 0.14f, 0.96f),
                _ => new Color(0.12f, 0.12f, 0.14f, 0.96f),
            };
            return grade switch
            {
                ECommonGrade.Uncommon => Color.Lerp(baseTone, new Color(0.25f, 0.4f, 0.3f, 1f), 0.35f),
                ECommonGrade.Rare => Color.Lerp(baseTone, new Color(0.25f, 0.35f, 0.55f, 1f), 0.4f),
                ECommonGrade.Epic => Color.Lerp(baseTone, new Color(0.4f, 0.25f, 0.5f, 1f), 0.45f),
                ECommonGrade.Legendary => Color.Lerp(baseTone, new Color(0.55f, 0.4f, 0.15f, 1f), 0.5f),
                _ => baseTone,
            };
        }
    }
}
