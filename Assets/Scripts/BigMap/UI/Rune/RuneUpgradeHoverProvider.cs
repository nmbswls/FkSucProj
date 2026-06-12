using cfg.demo;
using My.Config;
using My.Player;
using My.UI;
using UnityEngine;

namespace My.UI.Rune
{
    public sealed class RuneUpgradeHoverProvider : BaseUIHoverProvider
    {
        string _upgradeId;
        ERuneUpgradeNodeState _state;
        bool _isInitial;
        PlayerRuneSystem _runeSystem;

        public string UpgradeId => _upgradeId;
        public ERuneUpgradeNodeState State => _state;
        public bool IsInitial => _isInitial;
        public PlayerRuneSystem RuneSystem => _runeSystem;
        public RuneUpgradeInfo Def => RuneUpgradeCatalog.GetOrDefault(_upgradeId);

        protected override void Awake()
        {
            base.Awake();
            InnerParams = new HoverTipParams
            {
                TipType = EHoverTipType.RuneUpgrade,
                BindPos = Vector3.zero,
            };
        }

        public void Configure(
            string upgradeId,
            ERuneUpgradeNodeState state,
            bool isInitial,
            PlayerRuneSystem runeSystem)
        {
            _upgradeId = upgradeId ?? string.Empty;
            _state = state;
            _isInitial = isInitial;
            _runeSystem = runeSystem;
        }

        public override HoverTipParams? GetSimpleTipInfo()
        {
            if (string.IsNullOrEmpty(_upgradeId))
            {
                return null;
            }

            return InnerParams;
        }
    }
}
