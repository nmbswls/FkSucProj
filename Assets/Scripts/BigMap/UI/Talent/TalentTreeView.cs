using System.Collections.Generic;
using My.Config;
using TMPro;
using UnityEngine;

namespace My.UI.Talent
{
    public sealed class TalentTreeView : MonoBehaviour
    {
        public const string PlayerMainTreeId = "player_main";
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
