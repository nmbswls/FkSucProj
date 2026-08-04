using System;
using System.Collections.Generic;
using cfg.demo;
using My;
using My.Config;
using My.Player;
using My.Saving;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.CultTech
{
    // 秘会派遣：任务选择 → 目标选择 →（必要时）单位选择
    public sealed class CultDispatchPanelView : MonoBehaviour
    {
        public enum EDispatchSource
        {
            FromAnchor,
            FromSecretUnit,
        }

        enum EPhase
        {
            SelectMission,
            SelectTarget,
            SelectUnit,
        }

        [SerializeField] TextMeshProUGUI titleText;
        [SerializeField] TextMeshProUGUI subtitleText;
        [SerializeField] TextMeshProUGUI missionPlaceholderText;
        [SerializeField] TextMeshProUGUI targetValueText;
        [SerializeField] TextMeshProUGUI unitValueText;
        [SerializeField] TextMeshProUGUI statusText;
        [SerializeField] RectTransform unitPickRoot;
        [SerializeField] Button unitPickTemplate;
        [SerializeField] RectTransform anchorPickRoot;
        [SerializeField] Button anchorPickTemplate;
        [SerializeField] Button confirmButton;
        [SerializeField] Button cancelButton;

        readonly List<Button> _spawnedPickButtons = new();

        DemonCultSystem _cult;
        EDispatchSource _source;
        EPhase _phase;
        ECultMissionType _missionType = ECultMissionType.None;
        string _regionKey;
        string _logicAreaId;
        string _anchorId;
        string _unitId;
        string _scoutSiteId;

        void Awake()
        {
            ResolveRefs();
            if (unitPickTemplate != null) unitPickTemplate.gameObject.SetActive(false);
            if (anchorPickTemplate != null) anchorPickTemplate.gameObject.SetActive(false);
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveAllListeners();
                confirmButton.onClick.AddListener(OnConfirm);
            }
            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveAllListeners();
                cancelButton.onClick.AddListener(Hide);
            }
        }

        public void Bind(DemonCultSystem cult)
        {
            _cult = cult;
            ResolveRefs();
        }

        // 锚点快捷：增幅或灭迹
        public void OpenFromAnchor(
            string regionKey,
            string logicAreaId,
            string anchorId,
            string anchorDisplayName,
            ECultMissionType missionType)
        {
            ResolveRefs();
            _source = EDispatchSource.FromAnchor;
            _missionType = missionType;
            _regionKey = regionKey ?? string.Empty;
            _logicAreaId = logicAreaId ?? string.Empty;
            _anchorId = anchorId ?? string.Empty;
            _scoutSiteId = string.Empty;
            _unitId = string.Empty;
            _phase = EPhase.SelectUnit;
            gameObject.SetActive(true);
            if (titleText != null) titleText.text = "派遣秘会";
            RefreshMissionTexts();
            if (targetValueText != null)
            {
                var label = string.IsNullOrEmpty(anchorDisplayName) ? _anchorId : anchorDisplayName;
                targetValueText.text = $"{label}\n{_logicAreaId} / {_regionKey}";
            }
            if (unitValueText != null) unitValueText.text = "请选择空闲秘会";
            if (statusText != null) statusText.text = string.Empty;
            ClearSpawnedPicks();
            if (anchorPickRoot != null) anchorPickRoot.gameObject.SetActive(false);
            if (unitPickRoot != null) unitPickRoot.gameObject.SetActive(true);
            var pickCount = RefreshUnitPicks();
            SetConfirmReady(false);
            SetConfirmLabel("确认派遣");
            if (pickCount == 0 && statusText != null) statusText.text = "当前没有空闲秘会";
        }

        public void OpenFromSecretUnit(string unitId)
        {
            ResolveRefs();
            _source = EDispatchSource.FromSecretUnit;
            _unitId = unitId ?? string.Empty;
            _regionKey = string.Empty;
            _logicAreaId = string.Empty;
            _anchorId = string.Empty;
            _scoutSiteId = string.Empty;
            _missionType = ECultMissionType.None;
            _phase = EPhase.SelectMission;
            gameObject.SetActive(true);
            if (titleText != null) titleText.text = "派遣秘会";
            if (subtitleText != null) subtitleText.text = "选择任务类型";
            if (missionPlaceholderText != null) missionPlaceholderText.text = "请选择要执行的任务";
            if (unitValueText != null) unitValueText.text = FormatUnitName(_unitId);
            if (targetValueText != null) targetValueText.text = "先选择任务";
            if (statusText != null) statusText.text = string.Empty;
            ClearSpawnedPicks();
            if (anchorPickRoot != null) anchorPickRoot.gameObject.SetActive(false);
            if (unitPickRoot != null) unitPickRoot.gameObject.SetActive(true);
            var pickCount = RefreshMissionPicks();
            SetConfirmReady(false);
            SetConfirmLabel("确认派遣");
            if (pickCount == 0 && statusText != null) statusText.text = "没有可派遣任务";
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        void OnConfirm()
        {
            if (_cult == null)
            {
                if (statusText != null) statusText.text = "教团系统尚未初始化";
                return;
            }

            if (_source == EDispatchSource.FromSecretUnit && _phase != EPhase.SelectTarget
                && _missionType != ECultMissionType.None && string.IsNullOrEmpty(GetSelectedTargetKey()))
            {
                if (statusText != null) statusText.text = "请先选择目标";
                return;
            }
            if (string.IsNullOrEmpty(_unitId))
            {
                if (statusText != null) statusText.text = "请先选择空闲秘会";
                return;
            }

            bool ok;
            string failReason;
            switch (_missionType)
            {
                case ECultMissionType.AmplifyAnchor:
                    ok = _cult.TryAssignAmplifyAnchorMission(_unitId, _logicAreaId, _anchorId, out failReason);
                    break;
                case ECultMissionType.ScoutGather:
                    ok = _cult.TryAssignScoutGatherMission(_unitId, _scoutSiteId, out failReason);
                    break;
                case ECultMissionType.Preach:
                    ok = _cult.TryAssignPreachMission(_unitId, _regionKey, out failReason);
                    break;
                case ECultMissionType.OfferEssence:
                    ok = _cult.TryAssignOfferEssenceMission(_unitId, _regionKey, out failReason);
                    break;
                case ECultMissionType.CoverTraces:
                    ok = _cult.TryAssignCoverTracesMission(_unitId, _logicAreaId, _anchorId, out failReason);
                    break;
                default:
                    if (statusText != null) statusText.text = "请先选择任务";
                    return;
            }

            if (!ok)
            {
                if (statusText != null) statusText.text = FormatFailure(failReason);
                return;
            }
            Hide();
        }

        string GetSelectedTargetKey()
        {
            return _missionType switch
            {
                ECultMissionType.AmplifyAnchor => _anchorId,
                ECultMissionType.CoverTraces => _anchorId,
                ECultMissionType.ScoutGather => _scoutSiteId,
                ECultMissionType.Preach => _regionKey,
                ECultMissionType.OfferEssence => _regionKey,
                _ => string.Empty,
            };
        }

        int RefreshMissionPicks()
        {
            if (unitPickRoot == null || unitPickTemplate == null || _cult == null) return 0;
            var count = 0;
            var missions = _cult.GetDispatchableMissions();
            for (var i = 0; i < missions.Count; i++)
            {
                var mission = missions[i];
                if (mission == null) continue;
                var captured = mission;
                var btn = Instantiate(unitPickTemplate, unitPickRoot);
                btn.gameObject.SetActive(true);
                btn.name = "MissionPick_" + captured.MissionId;
                var label = btn.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                {
                    var days = captured.DurationDays > 0 ? $"{captured.DurationDays}日" : "永久";
                    label.text = $"{captured.DisplayName}\n{days}";
                }
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnMissionPicked(captured));
                _spawnedPickButtons.Add(btn);
                count++;
            }
            return count;
        }

        void OnMissionPicked(CultMission mission)
        {
            _missionType = mission.MissionType;
            _regionKey = string.Empty;
            _logicAreaId = string.Empty;
            _anchorId = string.Empty;
            _scoutSiteId = string.Empty;
            _phase = EPhase.SelectTarget;
            RefreshMissionTexts();
            if (targetValueText != null) targetValueText.text = "请选择目标";
            if (statusText != null) statusText.text = string.Empty;
            ClearSpawnedPicks();
            if (unitPickRoot != null) unitPickRoot.gameObject.SetActive(false);
            if (anchorPickRoot != null) anchorPickRoot.gameObject.SetActive(true);
            var pickCount = RefreshTargetPicks();
            SetConfirmReady(false);
            if (pickCount == 0 && statusText != null)
                statusText.text = "当前没有可用目标";
        }

        int RefreshTargetPicks()
        {
            return _missionType switch
            {
                ECultMissionType.AmplifyAnchor => RefreshAmplifyAnchorPicks(),
                ECultMissionType.CoverTraces => RefreshCoverAnchorPicks(),
                ECultMissionType.ScoutGather => RefreshScoutSitePicks(),
                ECultMissionType.Preach => RefreshRegionPicks(),
                ECultMissionType.OfferEssence => RefreshRegionPicks(),
                _ => 0,
            };
        }

        int RefreshAmplifyAnchorPicks()
        {
            if (anchorPickRoot == null || anchorPickTemplate == null || _cult == null) return 0;
            var count = 0;
            var anchors = _cult.GetAnchors(null, false);
            for (var i = 0; i < anchors.Count; i++)
            {
                var anchor = anchors[i];
                if (anchor == null || !anchor.Established || _cult.IsAnchorAmplified(anchor.LogicAreaId, anchor.AnchorId))
                    continue;
                count += SpawnAnchorTargetButton(anchor, false);
            }
            return count;
        }

        int RefreshCoverAnchorPicks()
        {
            if (anchorPickRoot == null || anchorPickTemplate == null || _cult == null) return 0;
            var count = 0;
            var day = (MainGameManager.Instance?.gameLogicManager?.SettlementDayIndex ?? 0) + 1;
            var anchors = _cult.GetAnchors(null, false);
            for (var i = 0; i < anchors.Count; i++)
            {
                var anchor = anchors[i];
                if (anchor == null || !anchor.Established) continue;
                if (!anchor.IsWatched && !anchor.IsDisabled(day)) continue;
                if (_cult.IsCoverTracesMissionOnAnchor(anchor.LogicAreaId, anchor.AnchorId)) continue;
                count += SpawnAnchorTargetButton(anchor, true);
            }
            return count;
        }

        int SpawnAnchorTargetButton(CultAnchorInfo anchor, bool cover)
        {
            var cfgName = ResolveAnchorDisplayName(anchor.LogicAreaId, anchor.AnchorId);
            var regionKey = ResolveAnchorRegion(anchor.LogicAreaId, anchor.AnchorId);
            var capturedRegion = regionKey;
            var capturedArea = anchor.LogicAreaId;
            var capturedAnchor = anchor.AnchorId;
            var capturedName = cfgName;
            var btn = Instantiate(anchorPickTemplate, anchorPickRoot);
            btn.gameObject.SetActive(true);
            btn.name = "AnchorPick_" + capturedAnchor;
            var label = btn.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                var threat = cover
                    ? (anchor.IsWatched ? "被盯梢" : "封锁中")
                    : "可增幅";
                label.text = $"{capturedName}\n{capturedArea} · {threat}";
            }
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                _regionKey = capturedRegion;
                _logicAreaId = capturedArea;
                _anchorId = capturedAnchor;
                _scoutSiteId = string.Empty;
                if (targetValueText != null)
                    targetValueText.text = $"{capturedName}\n{capturedArea} / {capturedRegion}";
                if (statusText != null) statusText.text = string.Empty;
                SetConfirmReady(true);
            });
            _spawnedPickButtons.Add(btn);
            return 1;
        }

        int RefreshScoutSitePicks()
        {
            if (anchorPickRoot == null || anchorPickTemplate == null || _cult == null) return 0;
            var count = 0;
            var sites = _cult.GetUnlockedScoutSites();
            for (var i = 0; i < sites.Count; i++)
            {
                var site = sites[i];
                if (site == null) continue;
                var captured = site;
                var btn = Instantiate(anchorPickTemplate, anchorPickRoot);
                btn.gameObject.SetActive(true);
                btn.name = "SitePick_" + captured.SiteId;
                var label = btn.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                {
                    var days = captured.DurationDays > 0 ? captured.DurationDays : (_cult.GetMissionByType(ECultMissionType.ScoutGather)?.DurationDays ?? 3);
                    label.text = $"{captured.DisplayName}\n{captured.RegionKey} · {days}日";
                }
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    _scoutSiteId = captured.SiteId;
                    _regionKey = captured.RegionKey ?? string.Empty;
                    _logicAreaId = captured.LogicAreaId ?? string.Empty;
                    _anchorId = string.Empty;
                    if (targetValueText != null)
                    {
                        var bonus = _cult.GetCultAttributeValue(ECultAttribute.ScoutLootBonusPercent);
                        var baseHint = $"基础：信仰{captured.BaseFaith}/教徒{captured.BaseLinker}"
                            + (string.IsNullOrEmpty(captured.BaseItemId) ? string.Empty : $"/{captured.BaseItemId}x{captured.BaseItemCount}")
                            + (bonus > 0 ? $"（科技+{bonus}%）" : string.Empty);
                        var specialHint = !string.IsNullOrEmpty(captured.SpecialItemId) && captured.SpecialChancePercent > 0
                            ? $"\n特殊：{captured.SpecialItemId}x{captured.SpecialItemCount}（{captured.SpecialChancePercent}%）"
                            : string.Empty;
                        targetValueText.text = $"{captured.DisplayName}\n{captured.RegionKey}\n{baseHint}{specialHint}"
                            + (string.IsNullOrEmpty(captured.Description) ? string.Empty : $"\n{captured.Description}");
                    }
                    if (statusText != null) statusText.text = string.Empty;
                    SetConfirmReady(true);
                });
                _spawnedPickButtons.Add(btn);
                count++;
            }
            return count;
        }

        int RefreshRegionPicks()
        {
            if (anchorPickRoot == null || anchorPickTemplate == null || _cult == null) return 0;
            var count = 0;
            var regions = _cult.GetKnownRegionKeys();
            for (var i = 0; i < regions.Count; i++)
            {
                var region = regions[i];
                if (string.IsNullOrEmpty(region)) continue;
                var captured = region;
                var linkers = _cult.GetLinkerCount(captured);
                var btn = Instantiate(anchorPickTemplate, anchorPickRoot);
                btn.gameObject.SetActive(true);
                btn.name = "RegionPick_" + captured;
                var label = btn.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                    label.text = $"{FormatRegionName(captured)}\n教徒 {linkers}";
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    _regionKey = captured;
                    _logicAreaId = string.Empty;
                    _anchorId = string.Empty;
                    _scoutSiteId = string.Empty;
                    if (targetValueText != null)
                        targetValueText.text = $"{FormatRegionName(captured)}\n教徒 {_cult.GetLinkerCount(captured)}";
                    if (statusText != null) statusText.text = string.Empty;
                    SetConfirmReady(true);
                });
                _spawnedPickButtons.Add(btn);
                count++;
            }
            return count;
        }

        int RefreshUnitPicks()
        {
            if (unitPickRoot == null || unitPickTemplate == null || _cult == null) return 0;
            var count = 0;
            foreach (var unit in _cult.SecretUnits)
            {
                if (unit == null || unit.State != ECultSecretUnitState.Available) continue;
                var captured = unit.UnitId;
                var btn = Instantiate(unitPickTemplate, unitPickRoot);
                btn.gameObject.SetActive(true);
                btn.name = "UnitPick_" + captured;
                var label = btn.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null) label.text = FormatUnitLabel(unit);
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    _unitId = captured;
                    if (unitValueText != null) unitValueText.text = FormatUnitLabel(unit);
                    if (statusText != null) statusText.text = string.Empty;
                    SetConfirmReady(true);
                });
                _spawnedPickButtons.Add(btn);
                count++;
            }
            return count;
        }

        void RefreshMissionTexts()
        {
            var mission = _cult?.GetMissionByType(_missionType);
            if (subtitleText != null)
            {
                if (mission == null) subtitleText.text = "任务";
                else if (mission.DurationDays > 0) subtitleText.text = $"{mission.DisplayName} · {mission.DurationDays}日";
                else subtitleText.text = $"{mission.DisplayName} · 永久";
            }
            if (missionPlaceholderText != null)
            {
                if (mission == null)
                {
                    missionPlaceholderText.text = "任务配置缺失";
                }
                else if (mission.MissionType == ECultMissionType.OfferEssence && _cult != null)
                {
                    var cap = 0;
                    foreach (var unit in _cult.SecretUnits)
                    {
                        if (unit != null && unit.UnitId == _unitId)
                        {
                            cap = Math.Clamp(unit.Capability, 0, 100);
                            break;
                        }
                    }
                    var rule = _cult.GetOfferEssenceRule(cap);
                    var expect = rule != null
                        ? $"能力值 {cap}：预计精元 {rule.JingyuanReward}，优质精华 {rule.PremiumChancePercent}%"
                        : $"能力值 {cap}";
                    missionPlaceholderText.text = $"任务：{mission.DisplayName}\n{mission.Description}\n{expect}";
                }
                else
                {
                    missionPlaceholderText.text = $"任务：{mission.DisplayName}\n{mission.Description}";
                }
            }
        }

        void SetConfirmReady(bool ready)
        {
            if (confirmButton != null) confirmButton.interactable = ready;
        }

        void SetConfirmLabel(string text)
        {
            if (confirmButton == null) return;
            var label = confirmButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null) label.text = text;
        }

        static string FormatFailure(string reason)
        {
            switch (reason)
            {
                case "unit_not_found": return "秘会不存在";
                case "unit_unavailable": return "该秘会已无法派遣";
                case "mission_not_configured": return "任务配置缺失";
                case "anchor_unavailable": return "目标锚点尚未建立或不可用";
                case "anchor_already_amplified": return "目标锚点已经处于增幅状态";
                case "anchor_not_threatened": return "该锚点未被盯梢或封锁";
                case "cover_already_assigned": return "已有秘会在执行灭迹";
                case "site_not_found": return "出征点不存在";
                case "site_locked": return "出征点尚未解锁";
                case "site_occupied": return "已有秘会前往该出征点";
                case "region_required": return "请选择传教地区";
                default: return "无法执行派遣";
            }
        }

        void ClearSpawnedPicks()
        {
            for (var i = 0; i < _spawnedPickButtons.Count; i++)
            {
                if (_spawnedPickButtons[i] != null)
                    Destroy(_spawnedPickButtons[i].gameObject);
            }
            _spawnedPickButtons.Clear();
        }

        string FormatUnitName(string unitId)
        {
            if (_cult != null && !string.IsNullOrEmpty(unitId))
            {
                foreach (var unit in _cult.SecretUnits)
                {
                    if (unit != null && unit.UnitId == unitId)
                        return FormatUnitLabel(unit);
                }
            }
            if (string.IsNullOrEmpty(unitId)) return "-";
            if (unitId.StartsWith("secret_unit_", StringComparison.Ordinal))
                return "秘会 · " + unitId.Substring("secret_unit_".Length);
            return unitId;
        }

        static string FormatUnitLabel(CultSecretUnitInfo unit)
        {
            if (unit == null) return "-";
            var name = unit.UnitId;
            if (!string.IsNullOrEmpty(name) && name.StartsWith("secret_unit_", StringComparison.Ordinal))
                name = "秘会 · " + name.Substring("secret_unit_".Length);
            var cap = Math.Clamp(unit.Capability, 0, 100);
            return $"{name}\n能力值 {cap}";
        }

        static string FormatRegionName(string regionKey)
        {
            if (string.IsNullOrEmpty(regionKey)) return "-";
            return regionKey == "default" ? "默认地区" : regionKey;
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

        void ResolveRefs()
        {
            titleText ??= FindText("Title");
            subtitleText ??= FindText("Subtitle");
            missionPlaceholderText ??= FindText("MissionPlaceholderText");
            targetValueText ??= FindText("TargetValue");
            unitValueText ??= FindText("UnitValue");
            statusText ??= FindText("StatusText");
            if (unitPickRoot == null)
            {
                var t = transform.Find("Frame/UnitPickRoot") ?? transform.Find("UnitPickRoot");
                if (t != null) unitPickRoot = t as RectTransform;
            }
            if (anchorPickRoot == null)
            {
                var t = transform.Find("Frame/AnchorPickRoot") ?? transform.Find("AnchorPickRoot");
                if (t != null) anchorPickRoot = t as RectTransform;
            }
            if (unitPickTemplate == null && unitPickRoot != null)
            {
                var t = unitPickRoot.Find("UnitPickTemplate");
                if (t != null) unitPickTemplate = t.GetComponent<Button>();
            }
            if (anchorPickTemplate == null && anchorPickRoot != null)
            {
                var t = anchorPickRoot.Find("AnchorPickTemplate");
                if (t != null) anchorPickTemplate = t.GetComponent<Button>();
            }
            if (confirmButton == null)
            {
                var t = transform.Find("Frame/ConfirmButton") ?? transform.Find("ConfirmButton");
                if (t != null) confirmButton = t.GetComponent<Button>();
            }
            if (cancelButton == null)
            {
                var t = transform.Find("Frame/CancelButton") ?? transform.Find("CancelButton");
                if (t != null) cancelButton = t.GetComponent<Button>();
            }
        }

        TextMeshProUGUI FindText(string childName)
        {
            var t = transform.Find("Frame/" + childName) ?? transform.Find(childName);
            return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
        }
    }
}
