using System.Collections.Generic;
using System;
using cfg.demo;
using My.Config;
using My.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.HumanTech
{
    public sealed class HumanTechTreeView : MonoBehaviour
    {
        const string TreeId = "human_main";
        [SerializeField] Transform nodeViewContainer;
        [SerializeField] TextMeshProUGUI debugTipText;
        [SerializeField] TextMeshProUGUI detailTitle;
        [SerializeField] TextMeshProUGUI detailBody;
        [SerializeField] TextMeshProUGUI detailStatusHint;
        [SerializeField] RectTransform stageNavigation;
        [SerializeField] TextMeshProUGUI stageTitle;
        [SerializeField] TextMeshProUGUI stageDescription;
        [SerializeField] TextMeshProUGUI stageProgress;
        readonly List<HumanTechNodeView> _generatedNodes = new();
        readonly List<Button> _stageButtons = new();
        HumanCivilizationSystem _progression;
        int _selectedNodeId;
        int _selectedStageLevel;

        public void Bind(HumanCivilizationSystem progression)
        {
            _progression = progression;
            EnsureDetailReferences();
            Rebuild();
        }

        public void Refresh()
        {
            if (_progression == null)
            {
                if (debugTipText != null) debugTipText.text = "Human civilization data unavailable";
                ClearDetail();
                return;
            }

            if (debugTipText != null)
            {
                debugTipText.text = $"Civilization Lv{_progression.GetCivilizationLevel()}  Tech {_progression.GetUnlockedTechCount()}";
            }

            RefreshStageHeader();

            foreach (var node in _generatedNodes)
            {
                node?.Refresh(_progression);
            }

            ShowDetail(_selectedNodeId);
        }

        void Rebuild()
        {
            EnsureContainer();
            EnsureDetailReferences();
            ClearGeneratedContent();
            if (_progression == null || nodeViewContainer == null)
            {
                ClearDetail();
                return;
            }

            var prefab = Resources.Load<GameObject>("UI/Prefabs/PlayerProgressionHubPanelSub/TalentNode_View");
            var table = CfgMgr.Cfgs?.TbHumanTechNode?.DataList;
            if (prefab == null || table == null)
            {
                Debug.LogWarning("[HumanTechTreeView] Node prefab or config is missing");
                ClearDetail();
                return;
            }

            BuildStageNavigation();
            if (_selectedStageLevel <= 0)
            {
                _selectedStageLevel = FindInitialStage(table);
            }

            var positions = new Dictionary<int, Vector2>();
            foreach (var row in table)
            {
                if (!IsNodeInSelectedStage(row))
                {
                    continue;
                }

                var go = Instantiate(prefab, nodeViewContainer);
                go.name = $"HumanTechNode_{row.NodeId}";
                if (go.transform is RectTransform rt)
                {
                    rt.anchorMin = new Vector2(0f, 1f);
                    rt.anchorMax = new Vector2(0f, 1f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(row.PosX, row.PosY);
                }

                var oldTalentView = go.GetComponent<My.UI.Talent.TalentTreeNodeView>();
                if (oldTalentView != null)
                {
                    oldTalentView.enabled = false;
                }

                var nodeView = go.AddComponent<HumanTechNodeView>();
                nodeView.Bind(row.NodeId, this);
                _generatedNodes.Add(nodeView);
                positions[row.NodeId] = new Vector2(row.PosX, row.PosY);
            }

            BuildConnections(table, positions);
            if (!IsNodeInSelectedStage(CfgMgr.Cfgs?.TbHumanTechNode?.GetOrDefault(_selectedNodeId)))
            {
                _selectedNodeId = FindInitialSelection(table);
            }
            Refresh();
        }

        int FindInitialStage(IReadOnlyList<HumanTechNode> table)
        {
            var maxStage = 1;
            foreach (var level in CfgMgr.Cfgs?.TbHumanCivilizationLevel?.DataList ?? new List<HumanCivilizationLevel>())
            {
                if (level != null && level.Level > 0) maxStage = Mathf.Max(maxStage, level.Level);
            }

            var current = Mathf.Clamp(_progression?.GetCivilizationLevel() ?? 1, 1, maxStage);
            foreach (var row in table)
            {
                if (row != null && row.TechTreeId == TreeId
                    && _progression.GetTechNodeVisualState(row.NodeId) == HumanTechNodeVisualState.Unlockable)
                {
                    return row.RequiredCivilizationLevel;
                }
            }
            return current;
        }

        bool IsNodeInSelectedStage(HumanTechNode row)
        {
            return row != null && row.TechTreeId == TreeId && row.RequiredCivilizationLevel == _selectedStageLevel;
        }

        void BuildStageNavigation()
        {
            EnsureStageReferences();
            if (stageNavigation == null) return;

            for (int i = stageNavigation.childCount - 1; i >= 0; i--)
            {
                var child = stageNavigation.GetChild(i);
                if (child.name == "StageTitle" || child.name == "StageDescription" || child.name == "StageProgress")
                {
                    continue;
                }
                Destroy(child.gameObject);
            }
            _stageButtons.Clear();

            var stages = CfgMgr.Cfgs?.TbHumanCivilizationLevel?.DataList;
            if (stages == null) return;

            foreach (var stage in stages)
            {
                if (stage == null || stage.Level <= 0) continue;
                var buttonGo = new GameObject($"CivilizationStage_{stage.Level}", typeof(RectTransform), typeof(Image), typeof(Button));
                buttonGo.transform.SetParent(stageNavigation, false);
                var buttonRect = (RectTransform)buttonGo.transform;
                buttonRect.sizeDelta = new Vector2(0f, 58f);
                var image = buttonGo.GetComponent<Image>();
                var button = buttonGo.GetComponent<Button>();
                var colors = button.colors;
                colors.normalColor = new Color(0.12f, 0.15f, 0.2f, 0.95f);
                colors.highlightedColor = new Color(0.22f, 0.3f, 0.4f, 1f);
                colors.selectedColor = new Color(0.25f, 0.38f, 0.48f, 1f);
                colors.pressedColor = new Color(0.3f, 0.45f, 0.55f, 1f);
                button.colors = colors;

                var labelGo = new GameObject("Label", typeof(RectTransform));
                labelGo.transform.SetParent(buttonGo.transform, false);
                var labelRect = (RectTransform)labelGo.transform;
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(12f, 4f);
                labelRect.offsetMax = new Vector2(-12f, -4f);
                var label = labelGo.AddComponent<TextMeshProUGUI>();
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.fontSize = 16f;
                label.text = $"{stage.Level}. {stage.DisplayName}";
                label.raycastTarget = false;

                var stageLevel = stage.Level;
                button.onClick.AddListener(() => SelectStage(stageLevel));
                _stageButtons.Add(button);
            }

            RefreshStageButtons();
        }

        void SelectStage(int stageLevel)
        {
            if (_selectedStageLevel == stageLevel) return;
            _selectedStageLevel = stageLevel;
            _selectedNodeId = 0;
            Rebuild();
        }

        void RefreshStageButtons()
        {
            var stages = CfgMgr.Cfgs?.TbHumanCivilizationLevel?.DataList;
            if (stages == null) return;
            var index = 0;
            foreach (var stage in stages)
            {
                if (stage == null || stage.Level <= 0 || index >= _stageButtons.Count) continue;
                var button = _stageButtons[index++];
                var unlocked = _progression != null && _progression.GetCivilizationLevel() >= stage.Level;
                button.interactable = true;
                var image = button.GetComponent<Image>();
                if (image != null)
                {
                    image.color = stage.Level == _selectedStageLevel
                        ? new Color(0.25f, 0.38f, 0.48f, 1f)
                        : unlocked
                            ? new Color(0.12f, 0.18f, 0.22f, 0.98f)
                            : new Color(0.08f, 0.09f, 0.11f, 0.9f);
                }
            }
        }

        void RefreshStageHeader()
        {
            var stage = CfgMgr.Cfgs?.TbHumanCivilizationLevel?.GetOrDefault(_selectedStageLevel);
            if (stageTitle != null) stageTitle.text = stage == null ? "Human Civilization" : $"{stage.Level}. {stage.DisplayName}";
            if (stageDescription != null) stageDescription.text = stage?.Desc ?? string.Empty;

            var total = 0;
            var unlocked = 0;
            foreach (var row in CfgMgr.Cfgs?.TbHumanTechNode?.DataList ?? new List<HumanTechNode>())
            {
                if (!IsNodeInSelectedStage(row)) continue;
                total++;
                if (_progression != null && _progression.GetTechNodeLevel(row.NodeId) >= row.MaxLevel) unlocked++;
            }
            if (stageProgress != null) stageProgress.text = $"{unlocked}/{total} technologies";
            RefreshStageButtons();
        }

        int FindInitialSelection(IReadOnlyList<HumanTechNode> table)
        {
            int first = 0;
            foreach (var row in table)
            {
                if (!IsNodeInSelectedStage(row))
                {
                    continue;
                }

                if (first == 0) first = row.NodeId;
                if (_progression.GetTechNodeVisualState(row.NodeId) == HumanTechNodeVisualState.Unlockable)
                {
                    return row.NodeId;
                }
            }

            return first;
        }

        void BuildConnections(IReadOnlyList<HumanTechNode> table, Dictionary<int, Vector2> positions)
        {
            var connections = new GameObject("Connections", typeof(RectTransform));
            connections.transform.SetParent(nodeViewContainer, false);
            var connectionRect = connections.GetComponent<RectTransform>();
            connectionRect.anchorMin = new Vector2(0f, 1f);
            connectionRect.anchorMax = new Vector2(0f, 1f);
            connectionRect.pivot = new Vector2(0f, 1f);
            connectionRect.anchoredPosition = Vector2.zero;
            connectionRect.sizeDelta = Vector2.zero;
            connections.transform.SetAsFirstSibling();

            foreach (var row in table)
            {
                var levelCfg = row == null ? null : CfgMgr.Cfgs?.TbHumanTechNodeLevel?.Get(row.NodeId, 1);
                if (!IsNodeInSelectedStage(row) || row.NodeId <= 0 || levelCfg?.PrereqNodeIds == null
                    || !positions.TryGetValue(row.NodeId, out var target))
                {
                    continue;
                }

                foreach (var prerequisite in levelCfg.PrereqNodeIds)
                {
                    var prerequisiteNode = CfgMgr.Cfgs?.TbHumanTechNode?.GetOrDefault(prerequisite);
                    if (!IsNodeInSelectedStage(prerequisiteNode) || !positions.TryGetValue(prerequisite, out var source))
                    {
                        continue;
                    }

                    var line = new GameObject($"Line_{prerequisite}_{row.NodeId}", typeof(RectTransform), typeof(Image));
                    line.transform.SetParent(connections.transform, false);
                    line.GetComponent<Image>().color = new Color(0.72f, 0.78f, 0.86f, 0.75f);
                    var rect = line.GetComponent<RectTransform>();
                    var delta = target - source;
                    rect.anchorMin = new Vector2(0f, 1f);
                    rect.anchorMax = new Vector2(0f, 1f);
                    rect.pivot = new Vector2(0f, 0.5f);
                    rect.anchoredPosition = source;
                    rect.sizeDelta = new Vector2(delta.magnitude, 4f);
                    rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
                }
            }
        }

        public void SelectNode(int nodeId)
        {
            _selectedNodeId = nodeId;
            ShowDetail(nodeId);
        }

        void ShowDetail(int nodeId)
        {
            if (_progression == null || nodeId <= 0)
            {
                ClearDetail();
                return;
            }

            var node = CfgMgr.Cfgs?.TbHumanTechNode?.GetOrDefault(nodeId);
            var current = _progression.GetTechNodeLevel(nodeId);
            var state = _progression.GetTechNodeVisualState(nodeId);
            var next = CfgMgr.Cfgs?.TbHumanTechNodeLevel?.Get(nodeId, current + 1);
            if (node == null)
            {
                ClearDetail();
                return;
            }

            if (detailTitle != null) detailTitle.text = node.DisplayName;
            if (detailBody != null)
            {
                var lines = new List<string>
                {
                    $"当前等级：{current}/{node.MaxLevel}",
                    $"所需文明等级：{node.RequiredCivilizationLevel}",
                    $"效果：{FormatEffect(next?.EffectKey ?? EHumanCivilizationAttribute.None, next?.EffectValue ?? 0)}",
                    $"消耗：{FormatCosts(next)}",
                    $"前置：{FormatPrerequisites(next)}"
                };
                detailBody.text = string.Join("\n", lines);
            }

            if (detailStatusHint != null)
            {
                detailStatusHint.text = state switch
                {
                    HumanTechNodeVisualState.Unlocked => "已解锁",
                    HumanTechNodeVisualState.Unlockable => "可以解锁",
                    HumanTechNodeVisualState.InsufficientCost => "人类通识不足",
                    _ => "尚未满足解锁条件"
                };
            }
        }

        static string FormatCosts(HumanTechNodeLevel level)
        {
            if (level?.UnlockCosts == null || level.UnlockCosts.Count == 0) return "无";
            var parts = new List<string>();
            foreach (var cost in level.UnlockCosts)
            {
                if (cost != null && cost.Count > 0) parts.Add($"{cost.ItemId} x{cost.Count}");
            }
            return parts.Count == 0 ? "无" : string.Join(", ", parts);
        }

        static string FormatPrerequisites(HumanTechNodeLevel level)
        {
            if (level?.PrereqNodeIds == null || level.PrereqNodeIds.Count == 0) return "无";
            var parts = new List<string>();
            foreach (var id in level.PrereqNodeIds)
            {
                var node = CfgMgr.Cfgs?.TbHumanTechNode?.GetOrDefault(id);
                parts.Add(node?.DisplayName ?? id.ToString());
            }
            return string.Join(", ", parts);
        }

        static string FormatEffect(EHumanCivilizationAttribute effect, long value)
        {
            if (effect == EHumanCivilizationAttribute.None) return "无";
            return $"{effect} +{value}";
        }

        void EnsureDetailReferences()
        {
            detailTitle ??= transform.Find("DetailArea/DetailTitle")?.GetComponent<TextMeshProUGUI>();
            detailBody ??= transform.Find("DetailArea/DetailBody")?.GetComponent<TextMeshProUGUI>();
            detailStatusHint ??= transform.Find("DetailArea/DetailStatusHint")?.GetComponent<TextMeshProUGUI>();
        }

        void EnsureStageReferences()
        {
            if (stageNavigation == null)
            {
                stageNavigation = transform.Find("StageNavigation") as RectTransform;
            }

            if (stageNavigation == null)
            {
                var root = new GameObject("StageNavigation", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
                root.transform.SetParent(transform, false);
                stageNavigation = (RectTransform)root.transform;
                stageNavigation.anchorMin = new Vector2(0.01f, 0.08f);
                stageNavigation.anchorMax = new Vector2(0.2f, 0.92f);
                stageNavigation.offsetMin = Vector2.zero;
                stageNavigation.offsetMax = Vector2.zero;
                root.GetComponent<Image>().color = new Color(0.06f, 0.08f, 0.1f, 0.92f);
                var layout = root.GetComponent<VerticalLayoutGroup>();
                layout.padding = new RectOffset(8, 8, 8, 8);
                layout.spacing = 6f;
                layout.childControlWidth = true;
                layout.childControlHeight = false;
                root.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            }

            var treeRoot = transform.Find("TreeRoot") as RectTransform;
            if (treeRoot != null)
            {
                treeRoot.anchorMin = new Vector2(0.22f, 0f);
                treeRoot.anchorMax = new Vector2(0.68f, 1f);
                treeRoot.offsetMin = new Vector2(8f, 14f);
                treeRoot.offsetMax = new Vector2(-8f, -14f);
            }

            stageTitle ??= FindOrCreateStageText("StageTitle", 20f, TextAlignmentOptions.TopLeft);
            stageDescription ??= FindOrCreateStageText("StageDescription", 14f, TextAlignmentOptions.TopLeft);
            stageProgress ??= FindOrCreateStageText("StageProgress", 13f, TextAlignmentOptions.BottomLeft);
        }

        TextMeshProUGUI FindOrCreateStageText(string name, float fontSize, TextAlignmentOptions alignment)
        {
            var existing = transform.Find($"StageNavigation/{name}")?.GetComponent<TextMeshProUGUI>();
            if (existing != null) return existing;
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(stageNavigation, false);
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(0f, name == "StageDescription" ? 72f : 30f);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.enableWordWrapping = true;
            text.raycastTarget = false;
            return text;
        }

        void ClearDetail()
        {
            if (detailTitle != null) detailTitle.text = string.Empty;
            if (detailBody != null) detailBody.text = string.Empty;
            if (detailStatusHint != null) detailStatusHint.text = string.Empty;
        }

        void ClearGeneratedContent()
        {
            _generatedNodes.Clear();
            if (nodeViewContainer == null) return;
            for (int i = nodeViewContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(nodeViewContainer.GetChild(i).gameObject);
            }
        }

        void EnsureContainer()
        {
            if (nodeViewContainer != null) return;
            nodeViewContainer = transform.Find("TreeRoot/Scroll View/Viewport/Content")
                ?? transform.Find("TreeRoot/Scroll/Viewport/Content")
                ?? transform.Find("TreeRoot/Viewport/Content")
                ?? transform.Find("TreeRoot");
        }

        public bool TryUnlock(int nodeId)
        {
            SelectNode(nodeId);
            if (_progression == null)
            {
                Debug.LogWarning("Human civilization data unavailable");
                return false;
            }

            if (!_progression.TryUnlockTechNode(nodeId, out var reason))
            {
                Debug.LogWarning($"Human tech unlock failed: {reason}");
                Refresh();
                return false;
            }

            Refresh();
            return true;
        }
    }
}
