using System;
using System.Collections.Generic;
using My.Config;
using TMPro;
using UnityEngine;

namespace My.UI.Talent
{
    public sealed class TalentTreeView : MonoBehaviour
    {
        public const string PlayerMainTreeId = "player_main";

        [Header("Player main tree layout")]
        [Tooltip("Height of each prerequisite-depth band, from the root band upward.")]
        [SerializeField] float[] stageHeights = { 180f, 150f, 150f, 150f };
        [SerializeField] float stageGap = 0f;
        [SerializeField] float bottomPadding = 70f;

        [SerializeField] Transform nodeViewContainer;
        [SerializeField] TextMeshProUGUI debugTipText;
        [SerializeField] TalentDetailView detailView;

        readonly Dictionary<int, TalentTreeNodeView> _nodeViewMap = new();
        GameObject _layoutInstance;
        ITalentProgressionContext _progression;
        string _treeId = PlayerMainTreeId;
        int _selectedNodeId;

        public string TreeId => _treeId;

        public void Bind(string treeId, ITalentProgressionContext progression)
        {
            _treeId = string.IsNullOrEmpty(treeId) ? PlayerMainTreeId : treeId;
            _progression = progression;
            _selectedNodeId = 0;
            RebuildLayout();
            Refresh();
        }

        public void Refresh()
        {
            EnsureNodeViewContainer();
            _nodeViewMap.Clear();

            var binders = nodeViewContainer != null
                ? nodeViewContainer.GetComponentsInChildren<TalentTreeNodeBinder>(true)
                : System.Array.Empty<TalentTreeNodeBinder>();
            int matchingBinders = 0;
            for (int i = 0; i < binders.Length; i++)
            {
                var binder = binders[i];
                var node = CfgMgr.Cfgs?.TbTalentNode?.GetOrDefault(binder.TalentNodeId);
                bool matches = node != null && node.TalentTreeId == _treeId;
                binder.gameObject.SetActive(matches);
                if (!matches)
                {
                    continue;
                }

                matchingBinders++;
                var view = binder.GetComponentInChildren<TalentTreeNodeView>(true);
                RefreshNodeView(view, binder.TalentNodeId);
            }

            if (_treeId == PlayerMainTreeId)
            {
                ApplyPlayerMainLayout();
            }

            int configuredNodeCount = CountConfiguredNodes();
            bool layoutComplete = matchingBinders == configuredNodeCount && configuredNodeCount > 0;

            RefreshDetail();
            if (debugTipText != null)
            {
                debugTipText.text = _progression == null
                    ? "未进入游戏上下文，无法读取养成数据。"
                    : layoutComplete ? string.Empty : "天赋树布局配置不完整。";
            }

            if (!layoutComplete)
            {
                Debug.LogError(
                    $"[TalentTreeView] Tree {_treeId} layout has {matchingBinders}/{configuredNodeCount} configured nodes.");
            }
        }

        public void SelectNode(int nodeId)
        {
            _selectedNodeId = nodeId;
            foreach (var pair in _nodeViewMap)
            {
                pair.Value.SetSelected(pair.Key == nodeId);
            }

            RefreshDetail();
        }

        public bool TryUpgradeNode(int nodeId, out string failReason)
        {
            if (_progression == null)
            {
                failReason = "no_progression_context";
                return false;
            }

            bool upgraded = _progression.TryUpgradeTalentNode(nodeId, out failReason);
            Refresh();
            return upgraded;
        }

        void RebuildLayout()
        {
            EnsureNodeViewContainer();
            ClearGeneratedLayout();
            SetStaticConnectionsVisible(_treeId == PlayerMainTreeId);

            var tree = CfgMgr.Cfgs?.TbTalentTree?.GetOrDefault(_treeId);
            if (tree == null || string.IsNullOrEmpty(tree.LayoutPrefabPath) || nodeViewContainer == null)
            {
                return;
            }

            var prefab = Resources.Load<GameObject>(tree.LayoutPrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[TalentTreeView] Layout prefab not found: {tree.LayoutPrefabPath}");
                return;
            }

            _layoutInstance = Instantiate(prefab, nodeViewContainer);
            _layoutInstance.name = $"{_treeId}_Layout";
            if (_layoutInstance.transform is RectTransform layoutRect)
            {
                layoutRect.anchorMin = Vector2.zero;
                layoutRect.anchorMax = Vector2.one;
                layoutRect.offsetMin = Vector2.zero;
                layoutRect.offsetMax = Vector2.zero;
                layoutRect.localScale = Vector3.one;
            }
        }

        int CountConfiguredNodes()
        {
            var table = CfgMgr.Cfgs?.TbTalentNode?.DataList;
            int count = 0;
            if (table != null)
            {
                for (int i = 0; i < table.Count; i++)
                {
                    if (table[i] != null && table[i].TalentTreeId == _treeId)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        void RefreshNodeView(TalentTreeNodeView view, int nodeId)
        {
            if (view == null)
            {
                Debug.LogWarning($"[TalentTreeView] Missing node view for node {nodeId}");
                return;
            }

            view.BindHost(this);
            view.Refresh(_progression, nodeId, nodeId == _selectedNodeId);
            _nodeViewMap[nodeId] = view;
        }

        void RefreshDetail()
        {
            if (detailView == null)
            {
                return;
            }

            if (_selectedNodeId <= 0)
            {
                detailView.Clear();
                return;
            }

            detailView.ShowNode(_selectedNodeId, _progression);
        }

        void ClearGeneratedLayout()
        {
            if (_layoutInstance != null)
            {
                Destroy(_layoutInstance);
                _layoutInstance = null;
            }
        }

        void SetStaticConnectionsVisible(bool visible)
        {
            if (nodeViewContainer == null)
            {
                return;
            }

            var connections = nodeViewContainer.Find("Connections");
            if (connections != null)
            {
                connections.gameObject.SetActive(visible);
            }
        }

        void ApplyPlayerMainLayout()
        {
            if (nodeViewContainer is not RectTransform content)
            {
                return;
            }

            var nodeRects = new Dictionary<int, RectTransform>();
            var binders = content.GetComponentsInChildren<TalentTreeNodeBinder>(true);
            for (int i = 0; i < binders.Length; i++)
            {
                if (binders[i].TryGetComponent<RectTransform>(out var rect))
                {
                    nodeRects[binders[i].TalentNodeId] = rect;
                }
            }

            var depthByNode = new Dictionary<int, int>();
            foreach (var pair in nodeRects)
            {
                depthByNode[pair.Key] = ResolveNodeDepth(pair.Key, new HashSet<int>());
            }

            int maxDepth = 0;
            foreach (var depth in depthByNode.Values)
            {
                maxDepth = Math.Max(maxDepth, depth);
            }

            float totalHeight = bottomPadding;
            for (int depth = 0; depth <= maxDepth; depth++)
            {
                totalHeight += GetStageHeight(depth);
                if (depth < maxDepth)
                {
                    totalHeight += stageGap;
                }
            }

            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, totalHeight);

            foreach (var pair in nodeRects)
            {
                int depth = depthByNode[pair.Key];
                float bandHeight = GetStageHeight(depth);
                float lowerBands = bottomPadding;
                for (int lowerDepth = 0; lowerDepth < depth; lowerDepth++)
                {
                    lowerBands += GetStageHeight(lowerDepth) + stageGap;
                }

                var position = pair.Value.anchoredPosition;
                position.y = -lowerBands - bandHeight * 0.5f;
                pair.Value.anchoredPosition = position;
            }

            RebuildConnectionGeometry(nodeRects);
        }

        float GetStageHeight(int depth)
        {
            if (stageHeights == null || stageHeights.Length == 0)
            {
                return 150f;
            }

            if (depth < stageHeights.Length)
            {
                return Mathf.Max(1f, stageHeights[depth]);
            }

            return Mathf.Max(1f, stageHeights[stageHeights.Length - 1]);
        }

        int ResolveNodeDepth(int nodeId, HashSet<int> visiting)
        {
            if (!visiting.Add(nodeId))
            {
                Debug.LogError($"[TalentTreeView] Cycle detected in talent prerequisites at node {nodeId}.");
                return 0;
            }

            var level = CfgMgr.Cfgs?.TbTalentNodeLevel?.Get(nodeId, 1);
            int depth = 0;
            if (level?.PrereqNodeIds != null)
            {
                foreach (var prerequisite in level.PrereqNodeIds)
                {
                    depth = Mathf.Max(depth, ResolveNodeDepth(prerequisite, visiting) + 1);
                }
            }

            visiting.Remove(nodeId);
            return depth;
        }

        void RebuildConnectionGeometry(IReadOnlyDictionary<int, RectTransform> nodeRects)
        {
            var connections = nodeViewContainer.Find("Connections");
            if (connections == null)
            {
                return;
            }

            foreach (Transform child in connections)
            {
                var parts = child.name.Split('_');
                if (parts.Length != 3
                    || !int.TryParse(parts[1], out var fromId)
                    || !int.TryParse(parts[2], out var toId)
                    || !nodeRects.TryGetValue(fromId, out var from)
                    || !nodeRects.TryGetValue(toId, out var to))
                {
                    continue;
                }

                var line = child as RectTransform;
                if (line == null)
                {
                    continue;
                }

                Vector2 delta = to.anchoredPosition - from.anchoredPosition;
                line.anchorMin = new Vector2(0.5f, 0.5f);
                line.anchorMax = new Vector2(0.5f, 0.5f);
                line.anchoredPosition = (from.anchoredPosition + to.anchoredPosition) * 0.5f;
                line.sizeDelta = new Vector2(delta.magnitude, Mathf.Max(1f, line.sizeDelta.y));
                line.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            }
        }

        void EnsureNodeViewContainer()
        {
            if (nodeViewContainer != null)
            {
                return;
            }

            var treeRoot = transform.Find("TreeRoot");
            nodeViewContainer = treeRoot?.Find("Scroll View/Viewport/Content")
                                ?? treeRoot?.Find("Scroll/Viewport/Content")
                                ?? treeRoot?.Find("Viewport/Content")
                                ?? treeRoot;
        }
    }
}
