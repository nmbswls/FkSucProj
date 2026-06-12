using cfg.demo;
using My.Config;
using My.Map.Entity;
using My.Player;
using My.UI;
using UnityEngine;

namespace My.UI.SkillLoadout
{
    public sealed class SkillInSchoolHoverProvider : BaseUIHoverProvider
    {
        int _entryId;
        string _skillId;
        bool _isLearned;

        public int EntryId => _entryId;
        public string SkillId => _skillId;
        public bool IsLearned => _isLearned;

        public SkillLearnEntry Entry => SkillLearnCatalog.TryGetLearnEntry(_entryId);
        public EntitySkillData SkillConfig => SkillLibrary.GetSkillConfig(_skillId);

        protected override void Awake()
        {
            base.Awake();
            InnerParams = new HoverTipParams
            {
                TipType = EHoverTipType.SkillInSchool,
                BindPos = Vector3.zero,
            };
        }

        public void Configure(int entryId, string skillId, bool isLearned)
        {
            _entryId = entryId;
            _skillId = skillId ?? string.Empty;
            _isLearned = isLearned;
        }

        public override HoverTipParams? GetSimpleTipInfo()
        {
            if (_entryId <= 0 || string.IsNullOrEmpty(_skillId))
            {
                return null;
            }

            return InnerParams;
        }

        public string GetDisplayName()
        {
            var entry = Entry;
            if (entry != null && !string.IsNullOrEmpty(entry.DisplayName))
            {
                return entry.DisplayName;
            }

            var cfg = SkillConfig;
            if (cfg != null && !string.IsNullOrEmpty(cfg.Desc))
            {
                return cfg.Desc;
            }

            return _skillId;
        }

        public string GetSummaryText()
        {
            var cfg = SkillConfig;
            if (cfg == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrEmpty(cfg.Desc))
            {
                return cfg.Desc;
            }

            return cfg.IsPassive ? "被动技能" : "主动技能";
        }

        public string GetStateText()
        {
            if (_isLearned)
            {
                return "已学习";
            }

            var mgr = MainGameManager.Instance?.gameLogicManager?.playerDataManager;
            if (mgr == null)
            {
                return "无法学习";
            }

            if (mgr.CanLearnSkillFromEntry(_entryId, out _))
            {
                return "可学习";
            }

            return "学习条件未满足";
        }

        public string GetHintText()
        {
            if (_isLearned)
            {
                return "拖拽到技能栏装备";
            }

            var mgr = MainGameManager.Instance?.gameLogicManager?.playerDataManager;
            if (mgr != null && mgr.CanLearnSkillFromEntry(_entryId, out _))
            {
                return "点击选中后，按学习按钮确认";
            }

            return "满足学习条件后可学习";
        }
    }
}
