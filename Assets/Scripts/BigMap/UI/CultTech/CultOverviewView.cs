using System;
using System.Collections.Generic;
using cfg.demo;
using My.Config;
using My.Player;
using My.Saving;
using My.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.CultTech
{
    // 教团主页签：摘要 + region 列表 + 锚点/秘会明细 + 派遣
    public sealed class CultOverviewView : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI faithValueText;
        [SerializeField] TextMeshProUGUI linkerValueText;
        [SerializeField] TextMeshProUGUI secretValueText;
        [SerializeField] TextMeshProUGUI anchorValueText;
        [SerializeField] TextMeshProUGUI selectedRegionText;
        [SerializeField] TextMeshProUGUI anchorEmptyText;
        [SerializeField] RectTransform regionListRoot;
        [SerializeField] CultRegionRowView regionRowTemplate;
        [SerializeField] RectTransform anchorListRoot;
        [SerializeField] CultAnchorRowView anchorRowTemplate;
        [SerializeField] RectTransform secretListRoot;
        [SerializeField] CultSecretUnitCardView secretCardTemplate;
        [SerializeField] CultDispatchPanelView dispatchPanel;

        TextMeshProUGUI _anchorHeaderText;
        TextMeshProUGUI _secretHeaderText;

        readonly List<CultRegionRowView> _regionRows = new();
        readonly List<CultRegionRowView> _areaRows = new();
        readonly List<CultAnchorRowView> _anchorRows = new();
        readonly List<CultSecretUnitCardView> _secretCards = new();
        readonly List<string> _regionKeys = new();

        DemonCultSystem _cult;
        string _selectedRegionKey = string.Empty;
        Action _cultChangedHandler;

        public void Bind(DemonCultSystem cult)
        {
            UnbindCultEvent();
            _cult = cult;
            ResolveLayout();
            HideTemplates();
            dispatchPanel?.Bind(cult);
            dispatchPanel?.Hide();
            if (_cult != null)
            {
                _cultChangedHandler = Refresh;
                _cult.OnCultChanged += _cultChangedHandler;
            }
            Refresh();
        }

        public void SetVisible(bool visible)
        {
            if (!visible) dispatchPanel?.Hide();
            gameObject.SetActive(visible);
            if (visible) Refresh();
        }

        public void Refresh()
        {
            if (_cult == null)
            {
                ResolveLayout();
                return;
            }

            ResolveLayout();
            HideTemplates();
            if (regionRowTemplate == null || anchorRowTemplate == null || secretCardTemplate == null
                || regionListRoot == null || anchorListRoot == null || secretListRoot == null)
            {
                Debug.LogError("[CultOverviewView] Overview layout missing in CultPanel prefab.");
                return;
            }

            if (faithValueText != null) faithValueText.text = _cult.Faith.ToString();
            if (linkerValueText != null) linkerValueText.text = _cult.GetTotalLinkerCount().ToString();
            if (secretValueText != null)
                secretValueText.text = $"{_cult.AvailableSecretUnitCount}/{_cult.SecretUnitCount}";
            var anchors = _cult.GetAnchors(null, false);
            var established = 0;
            for (var i = 0; i < anchors.Count; i++)
            {
                if (anchors[i] != null && anchors[i].Established) established++;
            }
            if (anchorValueText != null)
                anchorValueText.text = $"{established}/{anchors.Count}";

            RebuildRegionKeys(anchors);
            if (string.IsNullOrEmpty(_selectedRegionKey) && _regionKeys.Count > 0)
                _selectedRegionKey = _regionKeys[0];
            if (!_regionKeys.Contains(_selectedRegionKey) && _regionKeys.Count > 0)
                _selectedRegionKey = _regionKeys[0];

            RefreshRegionRows();
            RefreshAnchorRows(anchors);
            RefreshSecretCards();

            if (_anchorHeaderText != null)
                _anchorHeaderText.text = $"锚点 · {CountAnchorsInSelectedRegion(anchors)}";

            if (selectedRegionText != null)
            {
                selectedRegionText.text = string.IsNullOrEmpty(_selectedRegionKey)
                    ? "未选择区域"
                    : $"当前区域：{FormatRegionName(_selectedRegionKey)}";
            }
        }

        void OnDestroy()
        {
            UnbindCultEvent();
        }

        void UnbindCultEvent()
        {
            if (_cult != null && _cultChangedHandler != null)
                _cult.OnCultChanged -= _cultChangedHandler;
            _cultChangedHandler = null;
        }

        void RebuildRegionKeys(IReadOnlyList<CultAnchorInfo> anchors)
        {
            _regionKeys.Clear();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            void AddKey(string key)
            {
                if (string.IsNullOrEmpty(key) || !seen.Add(key)) return;
                _regionKeys.Add(key);
            }

            if (_cult != null)
            {
                var known = _cult.GetKnownRegionKeys();
                for (var i = 0; i < known.Count; i++)
                    AddKey(known[i]);
            }

            if (anchors != null)
            {
                for (var i = 0; i < anchors.Count; i++)
                {
                    var anchor = anchors[i];
                    if (anchor == null) continue;
                    AddKey(ResolveAnchorRegion(anchor.LogicAreaId, anchor.AnchorId));
                }
            }

            if (_regionKeys.Count == 0)
                AddKey("default");

            _regionKeys.Sort(StringComparer.Ordinal);
        }

        void RefreshRegionRows()
        {
            EnsurePool(_regionRows, regionRowTemplate, regionListRoot, _regionKeys.Count);
            for (var i = 0; i < _regionRows.Count; i++)
            {
                var row = _regionRows[i];
                if (i >= _regionKeys.Count)
                {
                    row.gameObject.SetActive(false);
                    continue;
                }

                var key = _regionKeys[i];
                var selected = key == _selectedRegionKey;
                row.gameObject.SetActive(true);
                row.Bind(
                    key,
                    FormatRegionName(key),
                    _cult.GetLinkerCount(key),
                    _cult.GetChurchPressure(key),
                    _cult.GetChurchPressureLevel(key),
                    selected,
                    OnRegionSelected);
            }
        }

        void RefreshAnchorRows(IReadOnlyList<CultAnchorInfo> anchors)
        {
            var filtered = new List<CultAnchorInfo>();
            if (anchors != null)
            {
                for (var i = 0; i < anchors.Count; i++)
                {
                    var anchor = anchors[i];
                    if (anchor == null) continue;
                    var region = ResolveAnchorRegion(anchor.LogicAreaId, anchor.AnchorId);
                    if (!string.IsNullOrEmpty(_selectedRegionKey) && region != _selectedRegionKey)
                        continue;
                    filtered.Add(anchor);
                }
            }

            if (anchorEmptyText != null)
            {
                anchorEmptyText.gameObject.SetActive(filtered.Count == 0);
                anchorEmptyText.text = "此区域暂无可见锚点";
            }

            var areaGroups = new Dictionary<string, List<CultAnchorInfo>>(StringComparer.Ordinal);
            foreach (var anchor in filtered)
            {
                if (!areaGroups.TryGetValue(anchor.LogicAreaId, out var group))
                {
                    group = new List<CultAnchorInfo>();
                    areaGroups.Add(anchor.LogicAreaId, group);
                }
                group.Add(anchor);
            }

            var areaIds = new List<string>(areaGroups.Keys);
            areaIds.Sort(StringComparer.Ordinal);
            EnsurePool(_areaRows, regionRowTemplate, anchorListRoot, areaIds.Count);
            EnsurePool(_anchorRows, anchorRowTemplate, anchorListRoot, filtered.Count);

            var siblingIndex = 0;
            for (var areaIndex = 0; areaIndex < _areaRows.Count; areaIndex++)
            {
                var areaRow = _areaRows[areaIndex];
                if (areaIndex >= areaIds.Count)
                {
                    areaRow.gameObject.SetActive(false);
                    continue;
                }

                var areaId = areaIds[areaIndex];
                var intel = ResolveIntelSummary(areaId);
                areaRow.gameObject.SetActive(true);
                areaRow.BindSummary(
                    areaId,
                    $"锚点 {areaGroups[areaId].Count}",
                    intel.Summary,
                    () => OnOpenIntelForArea(areaId));
                areaRow.transform.SetSiblingIndex(siblingIndex++);
            }

            for (var i = 0; i < _anchorRows.Count; i++)
            {
                var row = _anchorRows[i];
                if (i >= filtered.Count)
                {
                    row.gameObject.SetActive(false);
                    continue;
                }

                var anchor = filtered[i];
                var region = ResolveAnchorRegion(anchor.LogicAreaId, anchor.AnchorId);
                var display = ResolveAnchorDisplayName(anchor.LogicAreaId, anchor.AnchorId);
                var amplified = _cult.IsAnchorAmplified(anchor.LogicAreaId, anchor.AnchorId);
                row.gameObject.SetActive(true);
                row.Bind(
                    region,
                    anchor.LogicAreaId,
                    anchor.AnchorId,
                    display,
                    FormatAnchorStatus(anchor),
                    amplified,
                    ResolveAnchorAction(anchor, amplified),
                    OnDispatchFromAnchor);
                row.transform.SetSiblingIndex(siblingIndex++);
            }
        }

        void RefreshSecretCards()
        {
            var units = new List<CultSecretUnitInfo>();
            if (_cult != null)
            {
                foreach (var unit in _cult.SecretUnits)
                {
                    if (unit != null) units.Add(unit);
                }
            }

            units.Sort((a, b) => string.CompareOrdinal(a.UnitId, b.UnitId));
            if (_secretHeaderText != null)
                _secretHeaderText.text = units.Count > 0 ? $"秘会 · {units.Count}" : "秘会 · 尚未获得";
            EnsurePool(_secretCards, secretCardTemplate, secretListRoot, units.Count);
            for (var i = 0; i < _secretCards.Count; i++)
            {
                var card = _secretCards[i];
                if (i >= units.Count)
                {
                    card.gameObject.SetActive(false);
                    continue;
                }

                var unit = units[i];
                var canDispatch = unit.State == ECultSecretUnitState.Available;
                var canUpgrade = canDispatch && _cult.GetNextCapabilityLevel(unit) != null;
                card.gameObject.SetActive(true);
                card.Bind(
                    unit.UnitId,
                    unit.State,
                    FormatSecretUnitDetail(unit),
                    canDispatch,
                    OnDispatchFromSecretUnit,
                    canUpgrade,
                    OnUpgradeSecretUnit);
            }
        }

        void OnRegionSelected(string regionKey)
        {
            _selectedRegionKey = regionKey ?? string.Empty;
            Refresh();
        }

        void OnDispatchFromAnchor(
            string regionKey,
            string logicAreaId,
            string anchorId,
            CultAnchorRowView.EAnchorAction action)
        {
            if (dispatchPanel == null) return;
            var display = ResolveAnchorDisplayName(logicAreaId, anchorId);
            var missionType = action == CultAnchorRowView.EAnchorAction.Cover
                ? ECultMissionType.CoverTraces
                : ECultMissionType.AmplifyAnchor;
            dispatchPanel.OpenFromAnchor(regionKey, logicAreaId, anchorId, display, missionType);
        }

        void OnDispatchFromSecretUnit(string unitId)
        {
            dispatchPanel?.OpenFromSecretUnit(unitId);
        }

        void OnUpgradeSecretUnit(string unitId)
        {
            if (_cult == null) return;
            if (!_cult.TryUpgradeSecretUnitCapability(unitId, out var reason))
            {
                Debug.Log($"Secret unit capability upgrade failed: {reason}");
                return;
            }
            Refresh();
        }

        CultAnchorRowView.EAnchorAction ResolveAnchorAction(CultAnchorInfo anchor, bool amplified)
        {
            if (anchor == null || !anchor.Established || _cult == null)
                return CultAnchorRowView.EAnchorAction.None;

            var day = (MainGameManager.Instance?.gameLogicManager?.SettlementDayIndex ?? 0) + 1;
            if ((anchor.IsWatched || anchor.IsDisabled(day))
                && !_cult.IsCoverTracesMissionOnAnchor(anchor.LogicAreaId, anchor.AnchorId))
            {
                return CultAnchorRowView.EAnchorAction.Cover;
            }

            if (!amplified) return CultAnchorRowView.EAnchorAction.Amplify;
            return CultAnchorRowView.EAnchorAction.None;
        }

        void OnOpenIntelForArea(string logicAreaId)
        {
            if (!string.IsNullOrEmpty(logicAreaId))
                RumorIntelShopPanel.OpenForArea(logicAreaId);
        }

        (string Summary, bool CanOpen) ResolveIntelSummary(string logicAreaId)
        {
            var glm = MainGameManager.Instance?.gameLogicManager;
            var rumor = glm?.playerDataManager?.RumorIntel;
            if (rumor == null || string.IsNullOrEmpty(logicAreaId))
                return ("无情报", true);

            var active = 0;
            var purchasable = 0;
            var mapIds = new HashSet<string>(StringComparer.Ordinal) { logicAreaId };
            var overlays = CfgMgr.Cfgs?.TbAreaOverlayStateInfo?.DataList;
            if (overlays != null)
            {
                foreach (var overlay in overlays)
                {
                    if (overlay != null && overlay.VarId == logicAreaId)
                        mapIds.Add(overlay.Id);
                }
            }

            foreach (var mapId in mapIds)
            {
                foreach (var entry in rumor.GetActiveSnapshot(mapId))
                {
                    var def = CfgMgr.Cfgs?.TbRumorIntel?.GetOrDefault(entry.RumorId);
                    if (def?.EffectType == ERumorEffectType.CultInfluence) active++;
                }

                foreach (var def in rumor.ListPurchasableFixed(mapId))
                {
                    if (def?.EffectType == ERumorEffectType.CultInfluence) purchasable++;
                }
            }

            if (active > 0) return ($"待处理情报 {active}", true);
            if (purchasable > 0) return ($"可获取情报 {purchasable}", true);
            return ("暂无教团情报", true);
        }

        string FormatAnchorStatus(CultAnchorInfo anchor)
        {
            if (anchor == null) return "-";
            var amplified = _cult != null && _cult.IsAnchorAmplified(anchor.LogicAreaId, anchor.AnchorId);
            var bonus = amplified ? $" · +{DemonCultSystem.AmplifyAnchorOutputBonusPercent}%" : string.Empty;
            if (anchor.DisabledUntilSettlementDay > 0) return $"封锁至第 {anchor.DisabledUntilSettlementDay} 日{bonus}";
            if (anchor.IsWatched) return $"被盯梢{bonus}";
            if (anchor.Established) return $"已建立{bonus}";
            var need = ResolveEstablishProgress(anchor.LogicAreaId, anchor.AnchorId);
            return need > 0 ? $"进度 {anchor.Progress}/{need}" : $"进度 {anchor.Progress}";
        }

        string FormatSecretUnitDetail(CultSecretUnitInfo unit)
        {
            if (unit == null) return "-";
            var cap = Math.Clamp(unit.Capability, 0, 100);
            var capText = $"能力值 {cap}";
            if (unit.State == ECultSecretUnitState.Available)
            {
                var next = _cult?.GetNextCapabilityLevel(unit);
                if (next != null)
                {
                    var costParts = new List<string>();
                    if (next.CostJingyuan > 0) costParts.Add($"精元{next.CostJingyuan}");
                    if (next.CostFaith > 0) costParts.Add($"信仰{next.CostFaith}");
                    if (!string.IsNullOrEmpty(next.CostItemId) && next.CostItemCount > 0)
                        costParts.Add($"{next.CostItemId}x{next.CostItemCount}");
                    var costText = costParts.Count > 0 ? string.Join("+", costParts) : "免费";
                    return $"空闲 · {capText}\n升级→{next.Capability}（{costText}）";
                }
                return $"空闲 · {capText} · 已满";
            }

            var mission = CfgMgr.Cfgs?.TbCultMission?.GetOrDefault(unit.MissionId);
            var missionName = mission != null && !string.IsNullOrEmpty(mission.DisplayName)
                ? mission.DisplayName
                : string.IsNullOrEmpty(unit.MissionId) ? "任务中" : unit.MissionId;
            var remain = _cult != null ? _cult.GetMissionRemainingDays(unit) : 0;
            var remainText = unit.MissionEndsSettlementDay > 0 ? $" · 剩 {remain} 日" : " · 永久";

            if (mission?.MissionType == ECultMissionType.AmplifyAnchor)
            {
                var anchorName = ResolveAnchorDisplayName(unit.AssignedLogicAreaId, unit.AssignedAnchorId);
                return $"{missionName} · {anchorName} · +{DemonCultSystem.AmplifyAnchorOutputBonusPercent}%";
            }
            if (mission?.MissionType == ECultMissionType.ScoutGather)
            {
                var site = CfgMgr.Cfgs?.TbCultScoutSite?.GetOrDefault(unit.AssignedScoutSiteId);
                var siteName = site != null && !string.IsNullOrEmpty(site.DisplayName)
                    ? site.DisplayName
                    : unit.AssignedScoutSiteId;
                return $"{missionName} · {siteName}{remainText}";
            }
            if (mission?.MissionType == ECultMissionType.Preach)
            {
                var region = FormatRegionName(unit.AssignedRegionKey);
                return $"{missionName} · {region}{remainText}";
            }
            if (mission?.MissionType == ECultMissionType.CoverTraces)
            {
                var anchorName = ResolveAnchorDisplayName(unit.AssignedLogicAreaId, unit.AssignedAnchorId);
                return $"{missionName} · {anchorName}{remainText}";
            }
            if (mission?.MissionType == ECultMissionType.OfferEssence)
            {
                var region = FormatRegionName(unit.AssignedRegionKey);
                var rule = _cult?.GetOfferEssenceRule(unit.Capability);
                var expect = rule != null ? $"预计精元 {rule.JingyuanReward}" : capText;
                return $"{missionName} · {region} · {expect}{remainText}";
            }

            var fallbackRegion = string.IsNullOrEmpty(unit.AssignedRegionKey) ? "-" : unit.AssignedRegionKey;
            return $"{fallbackRegion} · {missionName} · {capText}{remainText}";
        }

        static long ResolveEstablishProgress(string logicAreaId, string anchorId)
        {
            var table = CfgMgr.Cfgs?.TbCultAnchor?.DataList;
            if (table == null) return 0;
            for (var i = 0; i < table.Count; i++)
            {
                var cfg = table[i];
                if (cfg != null && cfg.LogicAreaId == logicAreaId && cfg.AnchorId == anchorId)
                    return cfg.EstablishProgress;
            }
            return 0;
        }

        static string FormatRegionName(string regionKey)
        {
            if (string.IsNullOrEmpty(regionKey)) return "-";
            if (regionKey == "default") return "默认地区";
            return regionKey;
        }

        static string ResolveAnchorDisplayName(string logicAreaId, string anchorId)
        {
            var table = CfgMgr.Cfgs?.TbCultAnchor?.DataList;
            if (table == null) return anchorId;
            for (var i = 0; i < table.Count; i++)
            {
                var cfg = table[i];
                if (cfg != null && cfg.LogicAreaId == logicAreaId && cfg.AnchorId == anchorId)
                    return string.IsNullOrEmpty(cfg.DisplayName) ? anchorId : cfg.DisplayName;
            }
            return anchorId;
        }

        static string ResolveAnchorRegion(string logicAreaId, string anchorId)
        {
            var table = CfgMgr.Cfgs?.TbCultAnchor?.DataList;
            if (table == null) return string.Empty;
            for (var i = 0; i < table.Count; i++)
            {
                var cfg = table[i];
                if (cfg != null && cfg.LogicAreaId == logicAreaId && cfg.AnchorId == anchorId)
                    return cfg.RegionKey ?? string.Empty;
            }
            return string.Empty;
        }

        int CountAnchorsInSelectedRegion(IReadOnlyList<CultAnchorInfo> anchors)
        {
            if (anchors == null) return 0;
            var count = 0;
            for (var i = 0; i < anchors.Count; i++)
            {
                var anchor = anchors[i];
                if (anchor != null && ResolveAnchorRegion(anchor.LogicAreaId, anchor.AnchorId) == _selectedRegionKey)
                    count++;
            }
            return count;
        }

        static void EnsurePool<T>(List<T> pool, T template, RectTransform root, int needed) where T : Component
        {
            if (template == null || root == null) return;
            while (pool.Count < needed)
            {
                var item = Instantiate(template, root);
                item.gameObject.SetActive(true);
                pool.Add(item);
            }
        }

        void HideTemplates()
        {
            if (regionRowTemplate != null) regionRowTemplate.gameObject.SetActive(false);
            if (anchorRowTemplate != null) anchorRowTemplate.gameObject.SetActive(false);
            if (secretCardTemplate != null) secretCardTemplate.gameObject.SetActive(false);
        }

        void ResolveLayout()
        {
            faithValueText ??= FindText("SummaryBar/FaithValue") ?? FindText("FaithValue");
            linkerValueText ??= FindText("SummaryBar/LinkerValue") ?? FindText("LinkerValue");
            secretValueText ??= FindText("SummaryBar/SecretValue") ?? FindText("SecretValue");
            anchorValueText ??= FindText("SummaryBar/AnchorValue") ?? FindText("AnchorValue");
            selectedRegionText ??= FindText("DetailPanel/SelectedRegionText") ?? FindText("SelectedRegionText");
            anchorEmptyText ??= FindText("DetailPanel/AnchorEmptyText") ?? FindText("AnchorEmptyText");
            _anchorHeaderText ??= FindText("Body/DetailPanel/AnchorHeader");
            _secretHeaderText ??= FindText("Body/DetailPanel/SecretHeader");

            if (regionListRoot == null)
            {
                var t = transform.Find("Body/RegionPanel/RegionScroll/Viewport/RegionContent")
                    ?? transform.Find("RegionContent");
                if (t != null) regionListRoot = t as RectTransform;
            }
            if (anchorListRoot == null)
            {
                var t = transform.Find("Body/DetailPanel/AnchorScroll/Viewport/AnchorContent")
                    ?? transform.Find("AnchorContent");
                if (t != null) anchorListRoot = t as RectTransform;
            }
            if (secretListRoot == null)
            {
                var t = transform.Find("Body/DetailPanel/SecretScroll/Viewport/SecretContent")
                    ?? transform.Find("SecretContent");
                if (t != null) secretListRoot = t as RectTransform;
            }

            if (regionRowTemplate == null)
            {
                var t = transform.Find("Body/RegionPanel/RegionRowTemplate")
                    ?? transform.Find("RegionRowTemplate");
                if (t != null) regionRowTemplate = t.GetComponent<CultRegionRowView>();
            }
            if (anchorRowTemplate == null)
            {
                var t = transform.Find("Body/DetailPanel/AnchorRowTemplate")
                    ?? transform.Find("AnchorRowTemplate");
                if (t != null) anchorRowTemplate = t.GetComponent<CultAnchorRowView>();
            }
            if (secretCardTemplate == null)
            {
                var t = transform.Find("Body/DetailPanel/SecretCardTemplate")
                    ?? transform.Find("SecretCardTemplate");
                if (t != null) secretCardTemplate = t.GetComponent<CultSecretUnitCardView>();
            }
            if (dispatchPanel == null)
            {
                var t = transform.Find("DispatchPanel");
                if (t != null) dispatchPanel = t.GetComponent<CultDispatchPanelView>();
            }
        }

        TextMeshProUGUI FindText(string path)
        {
            var t = transform.Find(path);
            return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
        }
    }
}
