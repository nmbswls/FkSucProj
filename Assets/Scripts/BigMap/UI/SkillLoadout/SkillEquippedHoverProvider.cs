using cfg.demo;
using My.Map.Entity;
using My.UI;
using UnityEngine;

namespace My.UI.SkillLoadout
{
    public sealed class SkillEquippedHoverProvider : BaseUIHoverProvider
    {
        string _skillId;

        public string SkillId => _skillId;
        public EntitySkillData SkillConfig => SkillLibrary.GetSkillConfig(_skillId);

        protected override void Awake()
        {
            base.Awake();
            InnerParams = new HoverTipParams
            {
                TipType = EHoverTipType.SkillEquipped,
                BindPos = Vector3.zero,
            };
        }

        public void Configure(string skillId)
        {
            _skillId = skillId ?? string.Empty;
        }

        public override HoverTipParams? GetSimpleTipInfo()
        {
            if (string.IsNullOrEmpty(_skillId))
            {
                return null;
            }

            return InnerParams;
        }

        public string GetDisplayName()
        {
            return SkillEquippedHoverTextUtil.ResolveDisplayName(SkillConfig, _skillId);
        }

        public string GetSummaryText()
        {
            return SkillEquippedHoverTextUtil.ResolveSummary(SkillConfig);
        }

        public string GetStateText()
        {
            return SkillEquippedHoverTextUtil.ResolveStateText(_skillId, SkillConfig);
        }

        public string GetHintText()
        {
            return SkillEquippedHoverTextUtil.ResolveHintText(_skillId, SkillConfig);
        }
    }
}
