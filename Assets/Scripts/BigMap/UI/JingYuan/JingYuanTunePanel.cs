using System.Collections.Generic;
using cfg.demo;
using My.Config;
using My.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI
{
    public class JingYuanTunePanel : PanelBase, IPlayerProgressionHubPage
    {
        public const string Pid = "JingYuanTunePanel";

        IPlayerProgressionHubHost _progressionHubHost;
        PlayerJingYuanEssenceSystem _system;
        Transform _builtRoot;
        Transform _slotRoot;
        Transform _gridRoot;
        TextMeshProUGUI _titleText;
        TextMeshProUGUI _detailTypeText;
        TextMeshProUGUI _detailLevelText;
        TextMeshProUGUI _detailConcentrationText;
        TextMeshProUGUI _detailShelfLifeText;
        TextMeshProUGUI _detailEffectText;
        TextMeshProUGUI _detailAffixText;
        TextMeshProUGUI _detailLocationText;
        readonly List<Button> _slotButtons = new();
        readonly List<GameObject> _candidateObjects = new();
        int _selectedSlot;

        void Awake()
        {
            if (string.IsNullOrEmpty(panelId)) panelId = Pid;
        }

        public void SetProgressionHubHost(IPlayerProgressionHubHost host) => _progressionHubHost = host;

        public override void Setup(object data = null)
        {
            base.Setup(data);
            BindSystem();
            BuildView();
            RefreshView();
        }

        public override void Show()
        {
            base.Show();
            BindSystem();
            RefreshView();
        }

        public override void Hide()
        {
            UnbindSystem();
            base.Hide();
        }

        void BindSystem()
        {
            var next = MainGameManager.Instance?.gameLogicManager?.playerDataManager?.JingYuanEssenceSystem;
            if (_system == next) return;
            UnbindSystem();
            _system = next;
            if (_system != null) _system.EventOnChanged += RefreshView;
        }

        void UnbindSystem()
        {
            if (_system != null) _system.EventOnChanged -= RefreshView;
            _system = null;
        }

        void BuildView()
        {
            if (_builtRoot != null) return;
            _builtRoot = transform.Find("BuiltRoot");
            if (_builtRoot == null)
            {
                Debug.LogError("JingYuanTunePanel requires BuiltRoot in its prefab.");
                return;
            }

            _titleText = _builtRoot.Find("Title")?.GetComponent<TextMeshProUGUI>();
            _slotRoot = _builtRoot.Find("EquipSlots");
            _gridRoot = _builtRoot.Find("CandidatePanel/Viewport/Content");
            var detailRoot = _builtRoot.Find("DetailPanel");
            _detailTypeText = detailRoot?.Find("Type")?.GetComponent<TextMeshProUGUI>();
            _detailLevelText = detailRoot?.Find("Level")?.GetComponent<TextMeshProUGUI>();
            _detailConcentrationText = detailRoot?.Find("Concentration")?.GetComponent<TextMeshProUGUI>();
            _detailShelfLifeText = detailRoot?.Find("ShelfLife")?.GetComponent<TextMeshProUGUI>();
            _detailEffectText = detailRoot?.Find("MainEffect")?.GetComponent<TextMeshProUGUI>();
            _detailAffixText = detailRoot?.Find("ExtraAffix")?.GetComponent<TextMeshProUGUI>();
            _detailLocationText = detailRoot?.Find("Location")?.GetComponent<TextMeshProUGUI>();
            if (_slotRoot != null && _gridRoot != null && _detailTypeText != null && _detailLevelText != null && _detailConcentrationText != null && _detailShelfLifeText != null && _detailEffectText != null && _detailAffixText != null && _detailLocationText != null)
            {
                return;
            }

            ClearRoot(_builtRoot);
            var root = _builtRoot as RectTransform;
            var background = CreateImage("Background", root, new Color(0.055f, 0.065f, 0.09f, 0.98f));
            Stretch(background.rectTransform);

            _titleText = CreateText("Title", root, "优质精华装备", 28, TextAlignmentOptions.TopLeft);
            SetRect(_titleText.rectTransform, 28, -24, 0, -70, 0, 0);

            var slotLabel = CreateText("SlotLabel", root, "当前装备", 16, TextAlignmentOptions.Left);
            SetRect(slotLabel.rectTransform, 28, -78, 0, -110, 0, 0);
            var slots = CreateObject("EquipSlots", root);
            _slotRoot = slots.transform;
            SetRect(slots.GetComponent<RectTransform>(), 28, -116, -28, -200, 0, 1);
            var slotLayout = slots.AddComponent<HorizontalLayoutGroup>();
            slotLayout.spacing = 12;
            slotLayout.childForceExpandWidth = false;
            slotLayout.childForceExpandHeight = true;

            var candidateLabel = CreateText("CandidateLabel", root, "可装备精华", 16, TextAlignmentOptions.Left);
            SetRect(candidateLabel.rectTransform, 28, 116, 0, 84, 0, 0);
            var gridPanel = CreateImage("CandidatePanel", root, new Color(0.09f, 0.1f, 0.14f, 1f));
            SetRect(gridPanel.rectTransform, 28, 20, -292, 106, 0, 1);
            var scroll = gridPanel.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            var viewport = CreateObject("Viewport", gridPanel.transform);
            Stretch(viewport.GetComponent<RectTransform>());
            viewport.AddComponent<RectMask2D>();
            var content = CreateObject("Content", viewport.transform);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(-16, 0);
            var grid = content.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(170, 76);
            grid.spacing = new Vector2(8, 8);
            grid.padding = new RectOffset(8, 8, 8, 8);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = contentRect;
            _gridRoot = content.transform;

            var detailPanel = CreateImage("DetailPanel", root, new Color(0.09f, 0.1f, 0.14f, 1f));
            SetRect(detailPanel.rectTransform, 12, 20, -28, 106, 0.68f, 1f);
            _detailTypeText = CreateText("Type", detailPanel.transform, "请选择装备槽", 20, TextAlignmentOptions.TopLeft);
            _detailLevelText = CreateText("Level", detailPanel.transform, "等级：", 15, TextAlignmentOptions.Left);
            _detailConcentrationText = CreateText("Concentration", detailPanel.transform, "浓度：", 15, TextAlignmentOptions.Left);
            _detailShelfLifeText = CreateText("ShelfLife", detailPanel.transform, "保质期：", 15, TextAlignmentOptions.Left);
            _detailEffectText = CreateText("MainEffect", detailPanel.transform, "主词条：", 15, TextAlignmentOptions.Left);
            _detailAffixText = CreateText("ExtraAffix", detailPanel.transform, "额外词条：", 15, TextAlignmentOptions.Left);
            _detailLocationText = CreateText("Location", detailPanel.transform, "所在位置：", 15, TextAlignmentOptions.Left);
        }

        void RefreshView()
        {
            if (_builtRoot == null || _system == null) return;
            BuildSlots();
            BuildCandidates();
            RefreshDetail();
        }

        void BuildSlots()
        {
            ClearChildren(_slotRoot, null);
            _slotButtons.Clear();
            for (var i = 0; i < _system.EquippedCapacity; i++)
            {
                var index = i;
                var go = CreateObject($"EquipSlot_{i + 1}", _slotRoot);
                var rect = go.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(180, 72);
                var image = go.AddComponent<Image>();
                image.color = i == _selectedSlot ? new Color(0.25f, 0.38f, 0.52f) : new Color(0.14f, 0.16f, 0.22f);
                var button = go.AddComponent<Button>();
                button.onClick.AddListener(() => { _selectedSlot = index; RefreshView(); });
                _slotButtons.Add(button);
                var essence = i < _system.Equipped.Count ? _system.Equipped[i] : null;
                var label = CreateText("Label", go.transform, essence == null ? $"槽位 {i + 1}\n空" : FormatEssenceName(essence), 15, TextAlignmentOptions.Center);
                Stretch(label.rectTransform, 8);
                ApplyIcon(go.transform, essence, 8, 8, 48, 48);
            }
        }

        void BuildCandidates()
        {
            ClearChildren(_gridRoot, _candidateObjects);
            AddCandidate(null, "卸下精华", "空槽");
            foreach (var essence in _system.Temporary) AddCandidate(essence, "背包", "");
            foreach (var essence in _system.Warehouse) AddCandidate(essence, "仓库", "");
            foreach (var essence in _system.Equipped) AddCandidate(essence, "已装备", "");
        }

        void AddCandidate(PremiumEssenceInstance essence, string location, string suffix)
        {
            var go = CreateObject(essence == null ? "EmptyCandidate" : $"Essence_{essence.InstanceId}", _gridRoot);
            _candidateObjects.Add(go);
            var image = go.AddComponent<Image>();
            image.color = essence == null ? new Color(0.12f, 0.13f, 0.17f) : new Color(0.14f, 0.18f, 0.22f);
            var button = go.AddComponent<Button>();
            button.onClick.AddListener(() => OnCandidateClicked(essence));
            var label = CreateText("Label", go.transform,
                essence == null ? "卸下精华\n空槽" : $"{FormatEssenceName(essence)}\n{location}  浓度 {essence.Concentration}%\n保质期 {essence.RemainingShelfLifeDays} 天{suffix}",
                13, TextAlignmentOptions.Center);
            Stretch(label.rectTransform, 6);
            ApplyIcon(go.transform, essence, 8, 8, 42, 42);
        }

        void OnCandidateClicked(PremiumEssenceInstance essence)
        {
            if (_system == null) return;
            if (essence == null)
            {
                if (_selectedSlot < _system.Equipped.Count) _system.TryUnequip(_system.Equipped[_selectedSlot].InstanceId);
            }
            else
            {
                _system.TryEquip(essence.InstanceId);
            }
            RefreshView();
        }

        void RefreshDetail()
        {
            if (_detailTypeText == null) return;
            var essence = _selectedSlot < _system.Equipped.Count ? _system.Equipped[_selectedSlot] : null;
            if (essence == null)
            {
                _detailTypeText.text = $"装备槽 {_selectedSlot + 1}";
                _detailLevelText.text = "等级：-";
                _detailConcentrationText.text = "浓度：-";
                _detailShelfLifeText.text = "保质期：-";
                _detailEffectText.text = "主词条：-";
                _detailAffixText.text = "额外词条：-";
                _detailLocationText.text = "所在位置：未装备，从下方选择精华";
                return;
            }

            var effect = JingYuanEssenceCatalog.ResolveEffect(essence.TypeId, essence.DropLevel, essence.Concentration);
            var typeInfo = JingYuanEssenceCatalog.GetTypeInfo(essence.TypeId);
            _detailTypeText.text = typeInfo?.DisplayName ?? essence.TypeId.ToString();
            _detailLevelText.text = $"等级：{essence.DropLevel}";
            _detailConcentrationText.text = $"浓度：{essence.Concentration}%";
            _detailShelfLifeText.text = $"保质期：{essence.RemainingShelfLifeDays} 天";
            _detailEffectText.text = effect == null ? "主词条：暂无" : $"主词条：属性 {effect.AttrId} +{effect.AttrValue}";
            _detailAffixText.text = essence.ExtraAffixIds == null || essence.ExtraAffixIds.Count == 0 ? "额外词条：无" : $"额外词条：{string.Join("、", essence.ExtraAffixIds)}";
            _detailLocationText.text = "所在位置：装备槽";
        }

        static string FormatEssenceName(PremiumEssenceInstance essence)
        {
            return JingYuanEssenceCatalog.GetTypeInfo(essence.TypeId)?.DisplayName ?? essence.TypeId.ToString();
        }

        static void ApplyIcon(Transform parent, PremiumEssenceInstance essence, float left, float bottom, float width, float height)
        {
            if (essence == null) return;
            var info = JingYuanEssenceCatalog.GetTypeInfo(essence.TypeId);
            if (info == null || string.IsNullOrEmpty(info.IconPath)) return;
            var sprite = SimpleResManager.Load<Sprite>($"Sprites/{info.IconPath}");
            if (sprite == null) return;
            var image = CreateImage("Icon", parent, Color.white);
            image.sprite = sprite;
            image.preserveAspect = true;
            var rect = image.rectTransform;
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(0, 0);
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(left, bottom);
            rect.sizeDelta = new Vector2(width, height);
        }

        static GameObject CreateObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        static Image CreateImage(string name, Transform parent, Color color)
        {
            var image = CreateObject(name, parent).AddComponent<Image>();
            image.color = color;
            return image;
        }

        static TextMeshProUGUI CreateText(string name, Transform parent, string value, float size, TextAlignmentOptions alignment)
        {
            var text = CreateObject(name, parent).AddComponent<TextMeshProUGUI>();
            var fontSource = parent.GetComponentInParent<TextMeshProUGUI>(true);
            if (fontSource != null) text.font = fontSource.font;
            text.text = value;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.enableWordWrapping = true;
            return text;
        }

        static void Stretch(RectTransform rect, float padding = 0)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }

        static void SetRect(RectTransform rect, float left, float bottom, float right, float top, float anchorMinX, float anchorMaxX)
        {
            rect.anchorMin = new Vector2(anchorMinX, 0);
            rect.anchorMax = new Vector2(anchorMaxX, 1);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(right, top);
        }

        static void ClearRoot(Transform root)
        {
            for (var i = root.childCount - 1; i >= 0; i--) Destroy(root.GetChild(i).gameObject);
        }

        static void ClearChildren(Transform root, List<GameObject> tracked)
        {
            if (root == null) return;
            for (var i = root.childCount - 1; i >= 0; i--) Destroy(root.GetChild(i).gameObject);
            tracked?.Clear();
        }
    }
}
