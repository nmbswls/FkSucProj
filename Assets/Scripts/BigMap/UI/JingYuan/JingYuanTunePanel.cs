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
        TextMeshProUGUI _detailQualityText;
        TextMeshProUGUI _detailConcentrationText;
        TextMeshProUGUI _detailShelfLifeText;
        TextMeshProUGUI _detailEffectText;
        TextMeshProUGUI _detailAffixText;
        TextMeshProUGUI _detailLocationText;
        TextMeshProUGUI _renewalText;
        Button _renewButton;
        TextMeshProUGUI _tuneStatusText;
        Button _tuneButton;
        Toggle _residueBoostToggle;
        Button _temporaryTabButton;
        Button _warehouseTabButton;
        TextMeshProUGUI _temporaryTabText;
        TextMeshProUGUI _warehouseTabText;
        bool _showWarehouse;
        readonly List<Button> _slotButtons = new();
        readonly List<GameObject> _candidateObjects = new();
        int _selectedSlot;
        long _selectedDonorId;
        string _lastTuneResult;

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
            _gridRoot = _builtRoot.Find("SourcePanel/CandidatePanel/Viewport/Content");
            _temporaryTabButton = _builtRoot.Find("SourcePanel/SourceTabs/TemporaryTab")?.GetComponent<Button>();
            _warehouseTabButton = _builtRoot.Find("SourcePanel/SourceTabs/WarehouseTab")?.GetComponent<Button>();
            _temporaryTabText = _temporaryTabButton?.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
            _warehouseTabText = _warehouseTabButton?.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
            var detailRoot = _builtRoot.Find("DetailPanel");
            _detailTypeText = detailRoot?.Find("Type")?.GetComponent<TextMeshProUGUI>();
            _detailLevelText = detailRoot?.Find("Level")?.GetComponent<TextMeshProUGUI>();
            _detailQualityText = detailRoot?.Find("Quality")?.GetComponent<TextMeshProUGUI>();
            _detailConcentrationText = detailRoot?.Find("Concentration")?.GetComponent<TextMeshProUGUI>();
            _detailShelfLifeText = detailRoot?.Find("ShelfLife")?.GetComponent<TextMeshProUGUI>();
            _detailEffectText = detailRoot?.Find("MainEffect")?.GetComponent<TextMeshProUGUI>();
            _detailAffixText = detailRoot?.Find("ExtraAffix")?.GetComponent<TextMeshProUGUI>();
            _detailLocationText = detailRoot?.Find("Location")?.GetComponent<TextMeshProUGUI>();
            EnsureRenewalControls(detailRoot);
            if (_slotRoot == null || _gridRoot == null || detailRoot == null || _detailTypeText == null || _detailLevelText == null || _detailQualityText == null || _detailConcentrationText == null || _detailShelfLifeText == null || _detailEffectText == null || _detailAffixText == null || _detailLocationText == null || _renewalText == null || _renewButton == null || _tuneStatusText == null || _tuneButton == null || _residueBoostToggle == null)
            {
                Debug.LogError("JingYuanTunePanel prefab is missing required layout nodes.");
                return;
            }
            _temporaryTabButton.onClick.RemoveAllListeners();
            _temporaryTabButton.onClick.AddListener(() => { _showWarehouse = false; RefreshView(); });
            _warehouseTabButton.onClick.RemoveAllListeners();
            _warehouseTabButton.onClick.AddListener(() => { _showWarehouse = true; RefreshView(); });
        }

        void EnsureRenewalControls(Transform detailRoot)
        {
            if (detailRoot == null) return;
            _renewalText = detailRoot.Find("Renewal")?.GetComponent<TextMeshProUGUI>();
            _renewButton = detailRoot.Find("RenewButton")?.GetComponent<Button>();
            if (_renewalText == null)
            {
                _renewalText = CreateText("Renewal", detailRoot, "Renewal: -", 14, TextAlignmentOptions.Left);
                var rect = _renewalText.rectTransform;
                rect.anchorMin = new Vector2(0, 0); rect.anchorMax = new Vector2(1, 0);
                rect.offsetMin = new Vector2(16, 78); rect.offsetMax = new Vector2(-16, 104);
            }
            if (_renewButton == null)
            {
                var go = CreateObject("RenewButton", detailRoot);
                go.AddComponent<Image>().color = new Color(0.22f, 0.42f, 0.32f);
                _renewButton = go.AddComponent<Button>();
                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0, 0); rect.anchorMax = new Vector2(1, 0);
                rect.offsetMin = new Vector2(16, 18); rect.offsetMax = new Vector2(-16, 64);
                var label = CreateText("Label", go.transform, "Renew", 15, TextAlignmentOptions.Center);
                Stretch(label.rectTransform, 4);
            }
            _renewButton.onClick.RemoveAllListeners();
            _renewButton.onClick.AddListener(RenewSelected);

            _tuneStatusText = detailRoot.Find("TuneStatus")?.GetComponent<TextMeshProUGUI>();
            _tuneButton = detailRoot.Find("TuneButton")?.GetComponent<Button>();
            _residueBoostToggle = detailRoot.Find("ResidueBoostToggle")?.GetComponent<Toggle>();
            if (_renewalText == null || _renewButton == null || _tuneStatusText == null || _tuneButton == null || _residueBoostToggle == null)
            {
                Debug.LogError("JingYuanTunePanel prefab is missing renewal or tuning controls.");
                return;
            }
            if (_tuneStatusText == null)
            {
                _tuneStatusText = CreateText("TuneStatus", detailRoot, "Tune donor: -", 13, TextAlignmentOptions.Left);
                var rect = _tuneStatusText.rectTransform;
                rect.anchorMin = new Vector2(0, 0); rect.anchorMax = new Vector2(1, 0);
                rect.offsetMin = new Vector2(16, 136); rect.offsetMax = new Vector2(-16, 160);
            }
            if (_residueBoostToggle == null)
            {
                var go = CreateObject("ResidueBoostToggle", detailRoot);
                _residueBoostToggle = go.AddComponent<Toggle>();
                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0, 0); rect.anchorMax = new Vector2(1, 0);
                rect.offsetMin = new Vector2(16, 106); rect.offsetMax = new Vector2(-16, 132);
                var label = CreateText("Label", go.transform, "Use residue for +20% success", 13, TextAlignmentOptions.Left);
                Stretch(label.rectTransform, 24);
            }
            if (_tuneButton == null)
            {
                var go = CreateObject("TuneButton", detailRoot);
                go.AddComponent<Image>().color = new Color(0.45f, 0.30f, 0.18f);
                _tuneButton = go.AddComponent<Button>();
                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0, 0); rect.anchorMax = new Vector2(1, 0);
                rect.offsetMin = new Vector2(16, 166); rect.offsetMax = new Vector2(-16, 210);
                var label = CreateText("Label", go.transform, "Tune", 15, TextAlignmentOptions.Center);
                Stretch(label.rectTransform, 4);
            }
            _tuneButton.onClick.RemoveAllListeners();
            _tuneButton.onClick.AddListener(TuneSelected);
            _residueBoostToggle.onValueChanged.RemoveAllListeners();
            _residueBoostToggle.onValueChanged.AddListener(_ => RefreshDetail());
        }

        void RenewSelected()
        {
            if (_system == null || _selectedSlot >= _system.Equipped.Count) return;
            _system.TryRenew(_system.Equipped[_selectedSlot].InstanceId);
            RefreshView();
        }

        void TuneSelected()
        {
            if (_system == null || _selectedSlot >= _system.Equipped.Count || _selectedDonorId <= 0) return;
            var targetId = _system.Equipped[_selectedSlot].InstanceId;
            var boosted = _residueBoostToggle != null && _residueBoostToggle.isOn;
            var success = _system.TryTune(targetId, _selectedDonorId, boosted);
            _selectedDonorId = 0;
            _lastTuneResult = success ? "Result: Tune succeeded" : "Result: Tune failed or invalid";
            RefreshView();
        }

        void RefreshView()
        {
            if (_builtRoot == null || _system == null) return;
            BuildSlots();
            BuildCandidates();
            if (_temporaryTabText != null) _temporaryTabText.text = $"随身 {_system.Temporary.Count}/{_system.TemporaryCapacity}";
            if (_warehouseTabText != null) _warehouseTabText.text = $"仓库 {_system.Warehouse.Count}/{_system.WarehouseCapacity}";
            if (_temporaryTabButton != null && _temporaryTabButton.image != null) _temporaryTabButton.image.color = _showWarehouse ? new Color(.16f, .17f, .22f) : new Color(.28f, .22f, .38f);
            if (_warehouseTabButton != null && _warehouseTabButton.image != null) _warehouseTabButton.image.color = _showWarehouse ? new Color(.28f, .22f, .38f) : new Color(.16f, .17f, .22f);
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
            if (_showWarehouse)
            {
                foreach (var essence in _system.Warehouse) AddCandidate(essence, "仓库", "");
            }
            else
            {
                foreach (var essence in _system.Temporary) AddCandidate(essence, "随身", "");
            }
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
            if (essence != null)
            {
                var donorGo = CreateObject("DonorButton", go.transform);
                donorGo.AddComponent<Image>().color = new Color(0.32f, 0.22f, 0.12f, 0.95f);
                var donorButton = donorGo.AddComponent<Button>();
                donorButton.onClick.AddListener(() => SelectDonor(essence));
                var donorRect = donorGo.GetComponent<RectTransform>();
                donorRect.anchorMin = new Vector2(1, 0); donorRect.anchorMax = new Vector2(1, 0);
                donorRect.pivot = new Vector2(1, 0); donorRect.anchoredPosition = new Vector2(-5, 5);
                donorRect.sizeDelta = new Vector2(52, 22);
                var donorLabel = CreateText("Label", donorGo.transform, "Donor", 10, TextAlignmentOptions.Center);
                Stretch(donorLabel.rectTransform, 2);
            }
        }

        void SelectDonor(PremiumEssenceInstance essence)
        {
            _selectedDonorId = essence?.InstanceId ?? 0;
            RefreshDetail();
        }

        void OnCandidateClicked(PremiumEssenceInstance essence)
        {
            if (_system == null) return;
            if (essence == null)
            {
                _detailQualityText.text = "Quality: -";
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
                _renewalText.text = "Renewal: -";
                _renewButton.interactable = false;
                _tuneStatusText.text = "Tune donor: -";
                _tuneButton.interactable = false;
                return;
            }

            var effect = JingYuanEssenceCatalog.ResolveEffect(essence.TypeId, essence.DropLevel, essence.Concentration);
            var typeInfo = JingYuanEssenceCatalog.GetTypeInfo(essence.TypeId);
            _detailTypeText.text = typeInfo?.DisplayName ?? essence.TypeId.ToString();
            _detailQualityText.text = $"Quality: {essence.QualityTier}";
            _detailLevelText.text = $"等级：{essence.DropLevel}";
            _detailConcentrationText.text = $"浓度：{essence.Concentration}%";
            _detailShelfLifeText.text = $"保质期：{essence.RemainingShelfLifeDays} 天";
            _detailEffectText.text = effect == null ? "主词条：暂无" : $"主词条：属性 {effect.AttrId} +{effect.AttrValue}";
            _detailAffixText.text = essence.ExtraAffixIds == null || essence.ExtraAffixIds.Count == 0 ? "额外词条：无" : $"额外词条：{string.Join("、", essence.ExtraAffixIds)}";
            _detailLocationText.text = "所在位置：装备槽";
            var rule = CfgMgr.Cfgs?.TbJingYuanRenewalRule?.GetOrDefault("default");
            var inventory = MainGameManager.Instance?.gameLogicManager?.playerDataManager?.InventorySystem;
            var canRenew = rule != null && essence.RenewalCount < rule.MaxRenewalCount
                && _system.JingYuanResidue >= rule.ResidueCost
                && inventory != null && inventory.CheckHaveItem(rule.DesireCrystalItemId, rule.DesireCrystalCost);
            _renewalText.text = rule == null
                ? "Renewal: not configured"
                : $"Renewal: {essence.RenewalCount}/{rule.MaxRenewalCount}  Cost: Crystal {rule.DesireCrystalCost}, Residue {rule.ResidueCost}";
            _renewButton.interactable = canRenew;
            var donor = FindCandidate(_selectedDonorId);
            _tuneStatusText.text = donor == null
                ? $"Residue: {_system.JingYuanResidue} | Tune donor: -"
                : $"Residue: {_system.JingYuanResidue} | Tune donor: {FormatEssenceName(donor)} {donor.Concentration}% | Success: {_system.GetTuneSuccessRate(essence.InstanceId, donor.InstanceId, _residueBoostToggle.isOn)}%";
            if (!string.IsNullOrEmpty(_lastTuneResult)) _tuneStatusText.text += $" | {_lastTuneResult}";
            var tuneRule = CfgMgr.Cfgs?.TbJingYuanTuneRule?.GetOrDefault("default");
            _tuneButton.interactable = donor != null && donor != essence && donor.TypeId == essence.TypeId
                && tuneRule != null && (!_residueBoostToggle.isOn || _system.JingYuanResidue >= tuneRule.ResidueBoostCost);
        }

        PremiumEssenceInstance FindCandidate(long instanceId)
        {
            if (instanceId <= 0) return null;
            foreach (var item in _system.Temporary) if (item?.InstanceId == instanceId) return item;
            foreach (var item in _system.Warehouse) if (item?.InstanceId == instanceId) return item;
            foreach (var item in _system.Equipped) if (item?.InstanceId == instanceId) return item;
            return null;
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
