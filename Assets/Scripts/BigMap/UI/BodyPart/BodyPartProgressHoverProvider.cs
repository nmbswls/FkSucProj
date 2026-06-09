using System.Text;
using cfg.demo;
using My.Config;
using My.UI;
using UnityEngine;

namespace My.UI.BodyPart
{
    // 部位养成里程碑节点悬停：详情由 BodyPartProgressTip 展示
    public sealed class BodyPartProgressHoverProvider : BaseUIHoverProvider
    {
        int _milestoneId;
        int _currentLevel;

        protected override void Awake()
        {
            base.Awake();
            InnerParams = new HoverTipParams
            {
                TipType = EHoverTipType.BodyPartProgress,
                BindPos = Vector3.zero,
            };
        }

        public void Configure(BodyPartProgressInfo cfg, int currentLevel)
        {
            _milestoneId = cfg != null ? cfg.Id : 0;
            _currentLevel = Mathf.Max(0, currentLevel);
        }

        public override HoverTipParams? GetSimpleTipInfo()
        {
            if (_milestoneId <= 0)
            {
                return null;
            }

            return InnerParams;
        }

        public string GetDisplayName()
        {
            var cfg = GetCfg();
            return cfg != null ? $"Lv{cfg.Level} 里程碑" : string.Empty;
        }

        public string GetDetailText()
        {
            var cfg = GetCfg();
            if (cfg == null)
            {
                return string.Empty;
            }

            bool unlocked = _currentLevel >= cfg.Level;
            var lines = new StringBuilder();

            if (!string.IsNullOrEmpty(cfg.Desc))
            {
                lines.Append(cfg.Desc);
            }

            if (!unlocked)
            {
                if (lines.Length > 0)
                {
                    lines.AppendLine();
                }

                lines.Append($"（未解锁，需要 Lv{cfg.Level}）");
            }

            if (cfg.GlobalBonuses != null)
            {
                for (int i = 0; i < cfg.GlobalBonuses.Count; i++)
                {
                    var bonus = cfg.GlobalBonuses[i];
                    if (bonus == null)
                    {
                        continue;
                    }

                    if (lines.Length > 0)
                    {
                        lines.AppendLine();
                    }

                    string prefix = unlocked ? string.Empty : "[锁定] ";
                    lines.Append($"{prefix}全局属性 {bonus.AttrId}: +{bonus.Val}");
                }
            }

            if (!string.IsNullOrEmpty(cfg.PassiveSkillId))
            {
                if (lines.Length > 0)
                {
                    lines.AppendLine();
                }

                string prefix = unlocked ? string.Empty : "[锁定] ";
                lines.Append($"{prefix}被动: {cfg.PassiveSkillId}");
            }

            if (lines.Length == 0)
            {
                lines.Append(unlocked ? "(无额外加成)" : "(解锁后生效)");
            }

            return lines.ToString();
        }

        BodyPartProgressInfo GetCfg()
        {
            if (_milestoneId <= 0 || CfgMgr.Cfgs == null)
            {
                return null;
            }

            return CfgMgr.Cfgs.TbBodyPartProgressInfo.GetOrDefault(_milestoneId);
        }
    }
}
