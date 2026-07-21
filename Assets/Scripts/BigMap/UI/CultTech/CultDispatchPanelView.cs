using System;
using System.Collections.Generic;
using cfg.demo;
using My.Player;
using My.Saving;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.CultTech
{
    // 当前唯一任务为永久增幅锚点；任务定义来自 Luban，效果由 DemonCultSystem 结算。
    public sealed class CultDispatchPanelView : MonoBehaviour
    {
        public enum EDispatchSource
        {
            FromAnchor,
            FromSecretUnit,
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
        string _regionKey;
        string _logicAreaId;
        string _anchorId;
        string _unitId;

        void Awake()
        {
            ResolveRefs();
            if (unitPickTemplate != null) unitPickTemplate.gameObject.SetActive(false);
            if (anchorPickTemplate != null) anchorPickTemplate.gameObject.SetActive(false);
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveAllListeners();
                confirmButton.onClick.AddListener(OnConfirm);
                var label = confirmButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null) label.text = "确认增幅";
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

        public void OpenFromAnchor(string regionKey, string logicAreaId, string anchorId, string anchorDisplayName)
        {
            ResolveRefs();
            _source = EDispatchSource.FromAnchor;
            _regionKey = regionKey ?? string.Empty;
            _logicAreaId = logicAreaId ?? string.Empty;
            _anchorId = anchorId ?? string.Empty;
            _unitId = string.Empty;
            gameObject.SetActive(true);
            if (titleText != null) titleText.text = "派遣秘会";
            if (subtitleText != null) subtitleText.text = "永久任务 · 增幅锚点";
            SetMissionDescription();
            if (targetValueText != null)
            {
                var label = string.IsNullOrEmpty(anchorDisplayName) ? _anchorId : anchorDisplayName;
                targetValueText.text = $"{label}\n{_logicAreaId} / {_regionKey}";
            }
            if (unitValueText != null) unitValueText.text = "请选择空闲秘会";
            if (statusText != null) statusText.text = string.Empty;
            ClearSpawnedPicks();
            var pickCount = RefreshUnitPicks();
            if (anchorPickRoot != null) anchorPickRoot.gameObject.SetActive(false);
            if (unitPickRoot != null) unitPickRoot.gameObject.SetActive(true);
            SetConfirmReady(false);
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
            gameObject.SetActive(true);
            if (titleText != null) titleText.text = "派遣秘会";
            if (subtitleText != null) subtitleText.text = "永久任务 · 增幅锚点";
            SetMissionDescription();
            if (unitValueText != null) unitValueText.text = FormatUnitName(_unitId);
            if (targetValueText != null) targetValueText.text = "请选择目标锚点";
            if (statusText != null) statusText.text = string.Empty;
            ClearSpawnedPicks();
            var pickCount = RefreshAnchorPicks();
            if (unitPickRoot != null) unitPickRoot.gameObject.SetActive(false);
            if (anchorPickRoot != null) anchorPickRoot.gameObject.SetActive(true);
            SetConfirmReady(false);
            if (pickCount == 0 && statusText != null) statusText.text = "当前没有可增幅的已建立锚点";
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        void OnConfirm()
        {
            if (_source == EDispatchSource.FromAnchor && string.IsNullOrEmpty(_unitId))
            {
                if (statusText != null) statusText.text = "请先选择空闲秘会";
                return;
            }
            if (_source == EDispatchSource.FromSecretUnit && string.IsNullOrEmpty(_anchorId))
            {
                if (statusText != null) statusText.text = "请先选择目标锚点";
                return;
            }

            if (_cult == null)
            {
                if (statusText != null) statusText.text = "教团系统尚未初始化";
                return;
            }
            if (!_cult.TryAssignAmplifyAnchorMission(_unitId, _logicAreaId, _anchorId, out var failReason))
            {
                if (statusText != null) statusText.text = FormatFailure(failReason);
                return;
            }

            Hide();
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
                if (label != null) label.text = FormatUnitName(captured);
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    _unitId = captured;
                    if (unitValueText != null) unitValueText.text = FormatUnitName(captured);
                    if (statusText != null) statusText.text = string.Empty;
                    SetConfirmReady(true);
                });
                _spawnedPickButtons.Add(btn);
                count++;
            }
            return count;
        }

        int RefreshAnchorPicks()
        {
            if (anchorPickRoot == null || anchorPickTemplate == null || _cult == null) return 0;
            var count = 0;
            var anchors = _cult.GetAnchors(null, false);
            for (var i = 0; i < anchors.Count; i++)
            {
                var anchor = anchors[i];
                if (anchor == null || !anchor.Established || _cult.IsAnchorAmplified(anchor.LogicAreaId, anchor.AnchorId))
                    continue;
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
                    label.text = $"{capturedName}\n{capturedArea}";
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    _regionKey = capturedRegion;
                    _logicAreaId = capturedArea;
                    _anchorId = capturedAnchor;
                    if (targetValueText != null)
                        targetValueText.text = $"{capturedName}\n{capturedArea} / {capturedRegion}";
                    if (statusText != null) statusText.text = string.Empty;
                    SetConfirmReady(true);
                });
                _spawnedPickButtons.Add(btn);
                count++;
            }
            return count;
        }

        void SetMissionDescription()
        {
            if (missionPlaceholderText == null) return;
            var mission = _cult?.GetMissionByType(ECultMissionType.AmplifyAnchor);
            missionPlaceholderText.text = mission != null
                ? $"任务：{mission.DisplayName}\n{mission.Description}"
                : "任务配置缺失";
        }

        void SetConfirmReady(bool ready)
        {
            if (confirmButton != null) confirmButton.interactable = ready;
        }

        static string FormatFailure(string reason)
        {
            switch (reason)
            {
                case "unit_not_found": return "秘会不存在";
                case "unit_unavailable": return "该秘会已无法派遣";
                case "mission_not_configured": return "增幅任务配置缺失";
                case "anchor_unavailable": return "目标锚点尚未建立或不可用";
                case "anchor_already_amplified": return "目标锚点已经处于增幅状态";
                default: return "无法执行增幅任务";
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

        static string FormatUnitName(string unitId)
        {
            if (string.IsNullOrEmpty(unitId)) return "-";
            if (unitId.StartsWith("secret_unit_", StringComparison.Ordinal))
                return "秘会 · " + unitId.Substring("secret_unit_".Length);
            return unitId;
        }

        static string ResolveAnchorDisplayName(string logicAreaId, string anchorId)
        {
            var table = My.Config.CfgMgr.Cfgs?.TbCultAnchor?.DataList;
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
            var table = My.Config.CfgMgr.Cfgs?.TbCultAnchor?.DataList;
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
                var t = transform.Find("Frame/UnitPickRoot");
                if (t == null) t = transform.Find("UnitPickRoot");
                if (t != null) unitPickRoot = t as RectTransform;
            }
            if (anchorPickRoot == null)
            {
                var t = transform.Find("Frame/AnchorPickRoot");
                if (t == null) t = transform.Find("AnchorPickRoot");
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
