using System.Collections.Generic;
using cfg.demo;
using My.Config;
using My.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace My.UI.CultTech
{
    // 中心外扩径向教义树（与人类科技账册布局区分）
    public sealed class CultTechTreeView : MonoBehaviour
    {
        static readonly float[] RingRadius = { 0f, 180f, 300f, 400f };

        [SerializeField] Transform nodeViewContainer;
        [SerializeField] TextMeshProUGUI faithText;
        [SerializeField] TextMeshProUGUI detailTitle;
        [SerializeField] TextMeshProUGUI detailBody;
        [SerializeField] TextMeshProUGUI detailStatusHint;
        [SerializeField] Button unlockButton;

        readonly Dictionary<int, CultTechNodeView> _nodeViewMap = new();
        DemonCultSystem _cult;
        int _selectedNodeId;

        public void SetVisible(bool visible)
        {
            if (nodeViewContainer != null) nodeViewContainer.gameObject.SetActive(visible);
            if (faithText != null) faithText.gameObject.SetActive(visible);
            if (detailTitle != null) detailTitle.gameObject.SetActive(visible);
            if (detailBody != null) detailBody.gameObject.SetActive(visible);
            if (detailStatusHint != null) detailStatusHint.gameObject.SetActive(visible);
            if (unlockButton != null) unlockButton.gameObject.SetActive(visible);
        }

        public void Bind(DemonCultSystem cult)
        {
            _cult = cult;
            EnsureLayout();
            Rebuild();
        }

        public void Refresh()
        {
            if (_cult == null)
            {
                if (faithText != null)
                {
                    faithText.text = "Faith unavailable";
                }

                ClearDetail();
                return;
            }

            if (faithText != null)
            {
                faithText.text = $"信仰 {_cult.Faith}  ·  铭刻 {_cult.GetUnlockedTechCount()}";
            }

            foreach (var pair in _nodeViewMap)
            {
                pair.Value?.Refresh(_cult, pair.Key == _selectedNodeId);
            }

            ShowDetail(_selectedNodeId);
        }

        public void OnNodeClicked(int nodeId)
        {
            SelectNode(nodeId);
            var state = _cult?.GetTechNodeVisualState(nodeId) ?? CultTechNodeVisualState.Locked;
            if (state == CultTechNodeVisualState.Unlockable)
            {
                TryUnlock(nodeId);
            }
        }

        public bool TryUnlock(int nodeId)
        {
            SelectNode(nodeId);
            if (_cult == null)
            {
                Debug.LogWarning("Cult data unavailable");
                return false;
            }

            if (!_cult.TryUnlockNode(nodeId, out var reason))
            {
                Debug.LogWarning($"Cult tech unlock failed: {reason}");
                Refresh();
                return false;
            }

            Refresh();
            return true;
        }

        public void SelectNode(int nodeId)
        {
            _selectedNodeId = nodeId;
            ShowDetail(nodeId);
        }

        void Rebuild()
        {
            EnsureLayout();
            _nodeViewMap.Clear();
            if (_cult == null || nodeViewContainer == null)
            {
                ClearDetail();
                return;
            }

            var table = CfgMgr.Cfgs?.TbCultTechNode?.DataList;
            if (table == null)
            {
                Debug.LogWarning("[CultTechTreeView] Cult node config is missing");
                return;
            }

            var positions = new Dictionary<int, Vector2>();
            var binders = nodeViewContainer.GetComponentsInChildren<CultTechNodeBinder>(true);
            foreach (var binder in binders)
            {
                var node = CfgMgr.Cfgs?.TbCultTechNode?.GetOrDefault(binder.CultNodeId);
                var view = binder.GetComponent<CultTechNodeView>() ?? binder.GetComponentInChildren<CultTechNodeView>(true);
                bool valid = node != null && view != null;
                binder.gameObject.SetActive(valid);
                if (!valid)
                {
                    continue;
                }

                view.BindHost(this);
                _nodeViewMap[binder.CultNodeId] = view;
                if (view.transform is RectTransform rect)
                {
                    positions[binder.CultNodeId] = rect.anchoredPosition;
                }
            }

            BuildConnections(table, positions);
            if (_selectedNodeId <= 0 || !_nodeViewMap.ContainsKey(_selectedNodeId))
            {
                _selectedNodeId = FindInitialSelection(table);
            }

            if (_nodeViewMap.Count != CountConfiguredNodes())
            {
                Debug.LogError($"[CultTechTreeView] Prefab layout has {_nodeViewMap.Count}/{CountConfiguredNodes()} configured nodes.");
            }

            Refresh();
        }

        int CountConfiguredNodes()
        {
            var table = CfgMgr.Cfgs?.TbCultTechNode?.DataList;
            int count = 0;
            if (table != null)
            {
                foreach (var row in table)
                {
                    if (row != null && row.NodeId > 0) count++;
                }
            }

            return count;
        }

        static Vector2 ResolveRadialPosition(CultTechNode row)
        {
            int ring = Mathf.Clamp(row.Ring, 0, RingRadius.Length - 1);
            float radius = RingRadius[ring];
            if (radius <= 0.01f)
            {
                return Vector2.zero;
            }

            float rad = row.AngleDeg * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius);
        }

        int FindInitialSelection(IReadOnlyList<CultTechNode> table)
        {
            int first = 0;
            foreach (var row in table)
            {
                if (row == null)
                {
                    continue;
                }

                if (first == 0)
                {
                    first = row.NodeId;
                }

                if (_cult.GetTechNodeVisualState(row.NodeId) == CultTechNodeVisualState.Unlockable)
                {
                    return row.NodeId;
                }
            }

            return first;
        }

        void BuildConnections(IReadOnlyList<CultTechNode> table, Dictionary<int, Vector2> positions)
        {
            var connections = nodeViewContainer.Find("Connections");
            if (connections == null)
            {
                Debug.LogError("CultTechTreeView requires a Connections prefab container.");
                return;
            }

            for (int index = connections.childCount - 1; index >= 0; index--)
            {
                Destroy(connections.GetChild(index).gameObject);
            }

            var connectionPrefab = Resources.Load<GameObject>("UI/Prefabs/PlayerProgressionHubPanelSub/CultTechConnection_View");
            if (connectionPrefab == null)
            {
                Debug.LogError("CultTechTreeView requires CultTechConnection_View.prefab.");
                return;
            }

            foreach (var row in table)
            {
                var levelCfg = row == null ? null : CfgMgr.Cfgs?.TbCultTechNodeLevel?.Get(row.NodeId, 1);
                if (row == null || row.NodeId <= 0 || levelCfg?.PrereqNodeIds == null
                    || !positions.TryGetValue(row.NodeId, out var target))
                {
                    continue;
                }

                foreach (var prerequisite in levelCfg.PrereqNodeIds)
                {
                    if (!positions.TryGetValue(prerequisite, out var source))
                    {
                        continue;
                    }

                    var line = Instantiate(connectionPrefab, connections, false);
                    line.name = $"Line_{prerequisite}_{row.NodeId}";
                    var image = line.GetComponent<Image>();
                    if (image != null)
                    {
                        image.color = new Color(0.62f, 0.28f, 0.4f, 0.7f);
                    }
                    var rect = line.GetComponent<RectTransform>();
                    var delta = target - source;
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0f, 0.5f);
                    rect.anchoredPosition = source;
                    rect.sizeDelta = new Vector2(delta.magnitude, 3f);
                    rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
                }
            }
        }

        void ShowDetail(int nodeId)
        {
            if (_cult == null || nodeId <= 0)
            {
                ClearDetail();
                return;
            }

            var node = CfgMgr.Cfgs?.TbCultTechNode?.GetOrDefault(nodeId);
            if (node == null)
            {
                ClearDetail();
                return;
            }

            int current = _cult.GetTechNodeLevel(nodeId);
            var state = _cult.GetTechNodeVisualState(nodeId);
            var next = CfgMgr.Cfgs?.TbCultTechNodeLevel?.Get(nodeId, current + 1);

            if (detailTitle != null)
            {
                detailTitle.text = node.DisplayName;
            }

            if (detailBody != null)
            {
                var lines = new List<string>
                {
                    node.Desc ?? string.Empty,
                    $"当前等级：{current}/{node.MaxLevel}",
                    $"环位：R{node.Ring} / {node.AngleDeg}°",
                    $"效果：{(string.IsNullOrEmpty(next?.EffectDesc) ? (current > 0 ? "已铭刻" : "—") : next.EffectDesc)}",
                    $"信仰消耗：{(next == null ? "—" : next.FaithCost.ToString())}",
                    $"前置：{FormatPrerequisites(next)}",
                };
                detailBody.text = string.Join("\n", lines);
            }

            if (detailStatusHint != null)
            {
                detailStatusHint.text = state switch
                {
                    CultTechNodeVisualState.Unlocked => "已铭刻",
                    CultTechNodeVisualState.Unlockable => "可以铭刻",
                    CultTechNodeVisualState.InsufficientFaith => "信仰不足",
                    _ => "尚未满足铭刻条件",
                };
            }

            if (unlockButton != null)
            {
                unlockButton.interactable = state == CultTechNodeVisualState.Unlockable;
                var label = unlockButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                {
                    label.text = state == CultTechNodeVisualState.Unlocked ? "已铭刻" : "铭刻";
                }
            }
        }

        static string FormatPrerequisites(CultTechNodeLevel level)
        {
            if (level?.PrereqNodeIds == null || level.PrereqNodeIds.Count == 0)
            {
                return "无";
            }

            var parts = new List<string>();
            foreach (var id in level.PrereqNodeIds)
            {
                var node = CfgMgr.Cfgs?.TbCultTechNode?.GetOrDefault(id);
                parts.Add(node?.DisplayName ?? id.ToString());
            }

            return string.Join(", ", parts);
        }

        void ClearDetail()
        {
            if (detailTitle != null)
            {
                detailTitle.text = string.Empty;
            }

            if (detailBody != null)
            {
                detailBody.text = string.Empty;
            }

            if (detailStatusHint != null)
            {
                detailStatusHint.text = string.Empty;
            }

            if (unlockButton != null)
            {
                unlockButton.interactable = false;
            }
        }

        void ClearGeneratedContent()
        {
            _nodeViewMap.Clear();
            if (nodeViewContainer == null)
            {
                return;
            }

            var connections = nodeViewContainer.Find("Connections");
            if (connections != null)
            {
                for (int index = connections.childCount - 1; index >= 0; index--)
                {
                    Destroy(connections.GetChild(index).gameObject);
                }
            }
        }

        void EnsureLayout()
        {
            nodeViewContainer ??= transform.Find("TreeRoot");
            faithText ??= transform.Find("FaithText")?.GetComponent<TextMeshProUGUI>();
            detailTitle ??= transform.Find("DetailArea/DetailTitle")?.GetComponent<TextMeshProUGUI>()
                ?? transform.Find("DetailTitle")?.GetComponent<TextMeshProUGUI>();
            detailBody ??= transform.Find("DetailArea/DetailBody")?.GetComponent<TextMeshProUGUI>()
                ?? transform.Find("DetailBody")?.GetComponent<TextMeshProUGUI>();
            detailStatusHint ??= transform.Find("DetailArea/DetailStatusHint")?.GetComponent<TextMeshProUGUI>()
                ?? transform.Find("DetailStatusHint")?.GetComponent<TextMeshProUGUI>();
            unlockButton ??= transform.Find("DetailArea/UnlockButton")?.GetComponent<Button>()
                ?? transform.Find("UnlockButton")?.GetComponent<Button>();
            if (nodeViewContainer == null || faithText == null || detailTitle == null || detailBody == null
                || detailStatusHint == null || unlockButton == null)
                Debug.LogError("CultTechTreeView requires its layout nodes in the CultPanel prefab.");
        }

        void WireUnlockButton()
        {
            if (unlockButton == null)
            {
                return;
            }

            unlockButton.onClick.RemoveAllListeners();
            unlockButton.onClick.AddListener(() =>
            {
                if (_selectedNodeId > 0)
                {
                    TryUnlock(_selectedNodeId);
                }
            });
        }
    }
}
