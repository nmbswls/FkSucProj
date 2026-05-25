using My.Config;
using My.UI;
using UnityEngine;

namespace My.UI.Talent
{
    public sealed class TalentNodeHoverProvider : BaseUIHoverProvider
    {
        int _nodeId;

        protected override void Awake()
        {
            base.Awake();
            InnerParams = new HoverTipParams
            {
                TipType = EHoverTipType.Talent,
                BindPos = Vector3.zero,
            };
        }

        public void SetNodeId(int nodeId)
        {
            _nodeId = nodeId;
        }

        public int NodeId => _nodeId;

        public override HoverTipParams? GetSimpleTipInfo()
        {
            if (_nodeId <= 0)
            {
                return null;
            }

            return InnerParams;
        }

        public string GetDisplayName()
        {
            var row = CfgMgr.Cfgs?.TbTalentNode?.GetOrDefault(_nodeId);
            if (row != null && !string.IsNullOrEmpty(row.DisplayName))
            {
                return row.DisplayName;
            }

            return $"Node {_nodeId}";
        }

        public string GetDetailText()
        {
            var node = CfgMgr.Cfgs?.TbTalentNode?.GetOrDefault(_nodeId);
            if (node == null)
            {
                return string.Empty;
            }

            var levelRow = CfgMgr.Cfgs?.TbTalentNodeLevel?.Get(_nodeId, 1);
            if (levelRow == null)
            {
                return $"最大等级: {node.MaxLevel}";
            }

            var lines = new System.Text.StringBuilder();
            lines.AppendLine($"最大等级: {node.MaxLevel}");
            if (levelRow.StatBonuses != null && levelRow.StatBonuses.Count > 0)
            {
                lines.AppendLine("属性加成:");
                foreach (var bonus in levelRow.StatBonuses)
                {
                    if (bonus == null)
                    {
                        continue;
                    }

                    lines.AppendLine($"  属性{bonus.AttrId}: +{bonus.Val}");
                }
            }

            if (!string.IsNullOrEmpty(levelRow.PassiveSkillId))
            {
                lines.AppendLine($"被动: {levelRow.PassiveSkillId}");
            }

            return lines.ToString().TrimEnd();
        }
    }
}
