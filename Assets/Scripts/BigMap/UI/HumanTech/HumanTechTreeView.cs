using System.Collections.Generic;
using My.Config;
using My.Player;
using TMPro;
using UnityEngine;

namespace My.UI.HumanTech
{
    public sealed class HumanTechTreeView : MonoBehaviour
    {
        const string NodePrefabPath = "UI/Prefabs/PlayerProgressionHubPanelSub/TalentNode_View";
        [SerializeField] Transform nodeViewContainer;
        [SerializeField] TextMeshProUGUI debugTipText;
        readonly List<HumanTechNodeView> _generatedNodes = new();
        HumanCivilizationSystem _progression;
        public void Bind(HumanCivilizationSystem progression) { _progression = progression; Rebuild(); }
        public void Refresh()
        {
            if (_progression == null) { if (debugTipText != null) debugTipText.text = "Human civilization data unavailable"; return; }
            if (debugTipText != null) debugTipText.text = $"Civilization Lv{_progression.GetCivilizationLevel()}  Tech {_progression.GetUnlockedTechCount()}";
            foreach (var node in _generatedNodes) node?.Refresh(_progression);
        }
        void Rebuild()
        {
            EnsureContainer(); ClearNodes();
            if (_progression == null || nodeViewContainer == null) return;
            var prefab = Resources.Load<GameObject>(NodePrefabPath);
            var table = CfgMgr.Cfgs?.TbHumanTechNode?.DataList;
            if (prefab == null || table == null) return;
            foreach (var row in table)
            {
                if (row == null || row.TechTreeId != "human_main") continue;
                var go = Instantiate(prefab, nodeViewContainer); go.name = $"HumanTechNode_{row.NodeId}";
                if (go.transform is RectTransform rt) { rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(0f, 1f); rt.anchoredPosition = new Vector2(row.PosX, row.PosY); }
                var oldTalentView = go.GetComponent<My.UI.Talent.TalentTreeNodeView>();
                if (oldTalentView != null) oldTalentView.enabled = false;
                var nodeView = go.AddComponent<HumanTechNodeView>(); nodeView.Bind(row.NodeId, this); _generatedNodes.Add(nodeView);
            }
            Refresh();
        }
        void ClearNodes()
        {
            foreach (var node in _generatedNodes) if (node != null) Destroy(node.gameObject);
            _generatedNodes.Clear();
        }
        void EnsureContainer()
        {
            if (nodeViewContainer != null) return;
            nodeViewContainer = transform.Find("TreeRoot/Scroll View/Viewport/Content") ?? transform.Find("TreeRoot/Scroll/Viewport/Content") ?? transform.Find("TreeRoot/Viewport/Content") ?? transform.Find("TreeRoot");
        }
        public bool TryUnlock(int nodeId)
        {
            if (_progression == null || !_progression.TryUnlockTechNode(nodeId, out var reason)) { Debug.LogWarning($"Human tech unlock failed: {reason}"); return false; }
            Refresh(); return true;
        }
    }
}