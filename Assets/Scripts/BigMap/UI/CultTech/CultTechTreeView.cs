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

        readonly List<CultTechNodeView> _generatedNodes = new();
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

            foreach (var node in _generatedNodes)
            {
                node?.Refresh(_cult);
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
            ClearGeneratedContent();
            if (_cult == null || nodeViewContainer == null)
            {
                ClearDetail();
                return;
            }

            var table = CfgMgr.Cfgs?.TbCultTechNode?.DataList;
            if (table == null)
            {
                Debug.LogWarning("[CultTechTreeView] Cult node config is missing");
                ClearDetail();
                return;
            }

            var positions = new Dictionary<int, Vector2>();
            foreach (var row in table)
            {
                if (row == null || row.NodeId <= 0) continue;
                var pos = ResolveRadialPosition(row);
                var nodeView = CultTechNodeView.Create(nodeViewContainer, row.NodeId, this);
                if (nodeView.transform is RectTransform rect)
                {
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = pos;
                }
                _generatedNodes.Add(nodeView);
                positions[row.NodeId] = pos;
            }

            BuildConnections(table, positions);
            if (_selectedNodeId <= 0 || CfgMgr.Cfgs?.TbCultTechNode?.GetOrDefault(_selectedNodeId) == null)
                _selectedNodeId = FindInitialSelection(table);
            Refresh();
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
            var connections = new GameObject("Connections", typeof(RectTransform));
            connections.transform.SetParent(nodeViewContainer, false);
            var connectionRect = connections.GetComponent<RectTransform>();
            connectionRect.anchorMin = new Vector2(0.5f, 0.5f);
            connectionRect.anchorMax = new Vector2(0.5f, 0.5f);
            connectionRect.pivot = new Vector2(0.5f, 0.5f);
            connectionRect.anchoredPosition = Vector2.zero;
            connectionRect.sizeDelta = Vector2.zero;
            connections.transform.SetAsFirstSibling();

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

                    var line = new GameObject($"Line_{prerequisite}_{row.NodeId}", typeof(RectTransform), typeof(Image));
                    line.transform.SetParent(connections.transform, false);
                    line.GetComponent<Image>().color = new Color(0.62f, 0.28f, 0.4f, 0.7f);
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
            _generatedNodes.Clear();
            if (nodeViewContainer == null)
            {
                return;
            }

            for (int i = nodeViewContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(nodeViewContainer.GetChild(i).gameObject);
            }
        }

        void EnsureLayout()
        {
            EnsureBackground();
            EnsureFaithText();
            EnsureTreeRoot();
            EnsureDetailArea();
            WireUnlockButton();
        }

        void EnsureBackground()
        {
            var bg = transform.Find("Background")?.GetComponent<Image>();
            if (bg != null)
            {
                return;
            }

            var go = new GameObject("Background", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            go.transform.SetAsFirstSibling();
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = new Color(0.06f, 0.04f, 0.08f, 0.96f);
        }

        void EnsureFaithText()
        {
            if (faithText != null)
            {
                return;
            }

            faithText = transform.Find("FaithText")?.GetComponent<TextMeshProUGUI>();
            if (faithText != null)
            {
                return;
            }

            var go = new GameObject("FaithText", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.02f, 0.92f);
            rect.anchorMax = new Vector2(0.68f, 0.98f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            faithText = go.AddComponent<TextMeshProUGUI>();
            faithText.fontSize = 22f;
            faithText.alignment = TextAlignmentOptions.MidlineLeft;
            faithText.raycastTarget = false;
        }

        void EnsureTreeRoot()
        {
            if (nodeViewContainer != null)
            {
                return;
            }

            nodeViewContainer = transform.Find("TreeRoot");
            if (nodeViewContainer != null)
            {
                return;
            }

            var go = new GameObject("TreeRoot", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.02f, 0.06f);
            rect.anchorMax = new Vector2(0.68f, 0.9f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            nodeViewContainer = go.transform;
        }

        void EnsureDetailArea()
        {
            detailTitle ??= transform.Find("DetailArea/DetailTitle")?.GetComponent<TextMeshProUGUI>();
            detailBody ??= transform.Find("DetailArea/DetailBody")?.GetComponent<TextMeshProUGUI>();
            detailStatusHint ??= transform.Find("DetailArea/DetailStatusHint")?.GetComponent<TextMeshProUGUI>();
            unlockButton ??= transform.Find("DetailArea/UnlockButton")?.GetComponent<Button>();

            var detailRoot = transform.Find("DetailArea") as RectTransform;
            if (detailRoot == null)
            {
                var go = new GameObject("DetailArea", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(transform, false);
                detailRoot = (RectTransform)go.transform;
                detailRoot.anchorMin = new Vector2(0.7f, 0.06f);
                detailRoot.anchorMax = new Vector2(0.98f, 0.9f);
                detailRoot.offsetMin = Vector2.zero;
                detailRoot.offsetMax = Vector2.zero;
                go.GetComponent<Image>().color = new Color(0.1f, 0.06f, 0.12f, 0.94f);
            }

            detailTitle ??= CreateDetailText(detailRoot, "DetailTitle", new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.96f), 22f);
            detailBody ??= CreateDetailText(detailRoot, "DetailBody", new Vector2(0.05f, 0.28f), new Vector2(0.95f, 0.8f), 15f);
            detailStatusHint ??= CreateDetailText(detailRoot, "DetailStatusHint", new Vector2(0.05f, 0.16f), new Vector2(0.95f, 0.26f), 15f);

            if (unlockButton == null)
            {
                var btnGo = new GameObject("UnlockButton", typeof(RectTransform), typeof(Image), typeof(Button));
                btnGo.transform.SetParent(detailRoot, false);
                var btnRect = (RectTransform)btnGo.transform;
                btnRect.anchorMin = new Vector2(0.15f, 0.04f);
                btnRect.anchorMax = new Vector2(0.85f, 0.14f);
                btnRect.offsetMin = Vector2.zero;
                btnRect.offsetMax = Vector2.zero;
                btnGo.GetComponent<Image>().color = new Color(0.45f, 0.18f, 0.28f, 1f);
                unlockButton = btnGo.GetComponent<Button>();

                var labelGo = new GameObject("Label", typeof(RectTransform));
                labelGo.transform.SetParent(btnGo.transform, false);
                var labelRect = (RectTransform)labelGo.transform;
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
                var label = labelGo.AddComponent<TextMeshProUGUI>();
                label.text = "铭刻";
                label.fontSize = 18f;
                label.alignment = TextAlignmentOptions.Center;
                label.raycastTarget = false;
            }
        }

        static TextMeshProUGUI CreateDetailText(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax, float fontSize)
        {
            var existing = parent.Find(name)?.GetComponent<TextMeshProUGUI>();
            if (existing != null)
            {
                return existing;
            }

            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.enableWordWrapping = true;
            text.raycastTarget = false;
            return text;
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
