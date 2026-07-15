using My.Player;
using My.UI;
using UnityEngine;

namespace My.UI.HumanTech
{
    public sealed class HumanTechHoverProvider : BaseUIHoverProvider
    {
        int _nodeId;
        HumanCivilizationSystem _progression;

        protected override void Awake()
        {
            base.Awake();
            InnerParams = new HoverTipParams
            {
                TipType = EHoverTipType.HumanTech,
                BindPos = Vector3.zero,
            };
        }

        public void Configure(int nodeId, HumanCivilizationSystem progression)
        {
            _nodeId = nodeId;
            _progression = progression;
        }

        public int NodeId => _nodeId;
        public HumanCivilizationSystem Progression => _progression;

        public override HoverTipParams? GetSimpleTipInfo()
        {
            return _nodeId > 0 && _progression != null ? InnerParams : null;
        }
    }
}