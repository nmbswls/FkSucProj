using cfg.demo;
using My.Config;
using My.Map.Logic;
using My.Map.SavePoint;
using UnityEngine;

namespace My.Map.Entity
{
    // 由 DynamicEntityRefreshInfo + EntityInitInfo4SavePoint 驱动生成；不入地图 EntityRecords 持久化，仅依赖 StaticId 与出现/消失条件刷新。
    public class LogicEntitySavePoint : LogicEntityBase
    {
        public SavePoint Cfg { get; private set; }

        public LogicEntitySavePoint(GameLogicManager logicManager, long instId, string cfgId, Vector2 orgPos, LogicEntityRecord bindingRecord)
            : base(logicManager, instId, cfgId, orgPos, bindingRecord)
        {
        }

        public override EEntityType Type => EEntityType.SavePoint;

        public string SavePointId => CfgId;

        public bool IsFormallyUnlocked => SavePointUnlockHelper.IsFormallyUnlocked(LogicManager, SavePointId);

        public bool CanShowAndInteract => SavePointUnlockHelper.CanShowAndInteract(LogicManager, SavePointId);

        public bool NeedsTribute =>
            Cfg != null && Cfg.RequireTribute && !IsFormallyUnlocked;

        protected override void LoadCfg()
        {
            Cfg = CfgMgr.Cfgs?.TbSavePoint?.GetOrDefault(CfgId);
            if (Cfg == null && !string.IsNullOrEmpty(CfgId))
            {
                Debug.LogWarning("[SavePoint] Missing TbSavePoint row for cfgId: " + CfgId);
            }
        }

        protected override void OnTick(float dt)
        {
        }
    }
}
