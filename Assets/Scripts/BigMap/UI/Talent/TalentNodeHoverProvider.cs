using My.Player;
using My.UI;
using UnityEngine;

namespace My.UI.Talent
{
    public sealed class TalentNodeHoverProvider : BaseUIHoverProvider
    {
        int _nodeId;
        ITalentProgressionContext _progression;

        protected override void Awake()
        {
            base.Awake();
            InnerParams = new HoverTipParams
            {
                TipType = EHoverTipType.Talent,
                BindPos = Vector3.zero,
            };
        }

        public void Configure(int nodeId, ITalentProgressionContext progression)
        {
            _nodeId = nodeId;
            _progression = progression;
        }

        public void SetNodeId(int nodeId)
        {
            _nodeId = nodeId;
        }

        public ITalentProgressionContext Progression => _progression;

        public int NodeId => _nodeId;

        public override HoverTipParams? GetSimpleTipInfo()
        {
            if (_nodeId <= 0)
            {
                return null;
            }

            return InnerParams;
        }
    }
}
