using cfg.demo;
using My.UI;
using My.Player;
using TMPro;
using UnityEngine;

namespace My.UI.HumanTech
{
    public sealed class HumanTechHoverTipPanel : MonoBehaviour, IHoverTipPanel
    {
        [SerializeField] RectTransform root;
        [SerializeField] TextMeshProUGUI titleText;
        [SerializeField] TextMeshProUGUI summaryText;
        [SerializeField] TextMeshProUGUI stateText;
        [SerializeField] TextMeshProUGUI hintText;

        void Awake()
        {
            root ??= transform as RectTransform;
            titleText ??= transform.Find("TitleText")?.GetComponent<TextMeshProUGUI>();
            summaryText ??= transform.Find("SummaryText")?.GetComponent<TextMeshProUGUI>();
            stateText ??= transform.Find("StateText")?.GetComponent<TextMeshProUGUI>();
            hintText ??= transform.Find("HintText")?.GetComponent<TextMeshProUGUI>();
        }

        public void OnHoverTipUpdate(HoverTipParams tipParams, IHoverInfoProvider provider)
        {
            Awake();
            if (provider is not HumanTechHoverProvider tech || tech.Progression == null)
            {
                Clear();
                return;
            }

            if (root != null)
            {
                var pos = (Vector3)provider.TooltipPosition;
                pos.z = 0;
                root.position = pos;
            }

            var node = My.Config.CfgMgr.Cfgs?.TbHumanTechNode?.GetOrDefault(tech.NodeId);
            if (node == null)
            {
                Clear();
                return;
            }

            int current = tech.Progression.GetTechNodeLevel(tech.NodeId);
            var next = My.Config.CfgMgr.Cfgs?.TbHumanTechNodeLevel?.Get(tech.NodeId, current + 1);
            var state = tech.Progression.GetTechNodeVisualState(tech.NodeId);
            Set(titleText, node.DisplayName);
            Set(summaryText, $"Lv{current}/{node.MaxLevel}  Civilization Lv{node.RequiredCivilizationLevel}\nEffect: {FormatEffect(next)}");
            Set(stateText, state switch
            {
                HumanTechNodeVisualState.Unlocked => "Unlocked",
                HumanTechNodeVisualState.Unlockable => "Unlockable",
                HumanTechNodeVisualState.InsufficientCost => "Insufficient human knowledge",
                _ => "Locked"
            });
            Set(hintText, $"Cost: {FormatCosts(next)}\nPrerequisite: {FormatPrerequisites(next)}");
        }

        string FormatEffect(HumanTechNodeLevel level)
        {
            return level == null || level.EffectKey == EHumanCivilizationAttribute.None
                ? "None"
                : $"{level.EffectKey} +{level.EffectValue}";
        }

        string FormatCosts(HumanTechNodeLevel level)
        {
            if (level?.UnlockCosts == null || level.UnlockCosts.Count == 0) return "None";
            var values = new System.Collections.Generic.List<string>();
            foreach (var cost in level.UnlockCosts)
            {
                if (cost != null && cost.Count > 0) values.Add($"{cost.ItemId} x{cost.Count}");
            }
            return values.Count == 0 ? "None" : string.Join(", ", values);
        }

        string FormatPrerequisites(HumanTechNodeLevel level)
        {
            if (level?.PrereqNodeIds == null || level.PrereqNodeIds.Count == 0) return "None";
            var values = new System.Collections.Generic.List<string>();
            foreach (var id in level.PrereqNodeIds)
            {
                values.Add(My.Config.CfgMgr.Cfgs?.TbHumanTechNode?.GetOrDefault(id)?.DisplayName ?? id.ToString());
            }
            return string.Join(", ", values);
        }

        void Clear()
        {
            Set(titleText, string.Empty); Set(summaryText, string.Empty); Set(stateText, string.Empty); Set(hintText, string.Empty);
        }

        static void Set(TextMeshProUGUI text, string value)
        {
            if (text != null) text.text = value ?? string.Empty;
        }
    }
}