using System.Collections.Generic;
using cfg.demo;
using My;
using My.Config;
using My.Map.Logic;
using UnityEngine;

namespace My.Map
{
    public class AreaEffectLogicEntity : LogicEntityBase
    {
        public MapAreaEffect CfgRow { get; private set; }

        public AreaEffectLogicEntity(GameLogicManager logicManager, long instId, string cfgId, Vector2 orgPos, LogicEntityRecord bindingRecord) : base(logicManager, instId, cfgId, orgPos, bindingRecord)
        {
        }

        public HashSet<long> currAffectedEntites = new();
        public HashSet<long> newEntities = new();
        public int TotalTriggerCnt = 0;

        public override EEntityType Type => EEntityType.AreaEffect;

        public override void Initialize()
        {
            base.Initialize();

            CfgRow = CfgMgr.Cfgs?.TbMapAreaEffect?.GetOrDefault(CfgId);
            if (CfgRow == null)
            {
                Debug.LogError($"AreaEffectLogicEntity Luban row missing: {CfgId}");
            }
        }

        protected override void OnTick(float dt)
        {
            base.OnTick(dt);
        }

        public void UpdateAffectedLogics(List<ILogicEntity> inEnties)
        {
            if (CfgRow == null || string.IsNullOrEmpty(CfgRow.AreaBuffId))
            {
                return;
            }

            newEntities.Clear();
            foreach (var inEntity in inEnties)
            {
                if (!currAffectedEntites.Contains(inEntity.Id))
                {
                    LogicManager.globalBuffManager.RequestAddBuff(inEntity.Id, CfgRow.AreaBuffId, casterId: Id);
                }

                newEntities.Add(inEntity.Id);
            }

            foreach (var curEntity in currAffectedEntites)
            {
                if (!newEntities.Contains(curEntity))
                {
                    LogicManager.globalBuffManager.RemoveAllBuffById(curEntity, CfgRow.AreaBuffId, casterId: Id);
                }
            }

            var t = currAffectedEntites;
            currAffectedEntites = newEntities;
            newEntities = t;
        }

        public override void DoEntityDestroyed(string reason)
        {
            base.DoEntityDestroyed(reason);

            if (CfgRow != null && !string.IsNullOrEmpty(CfgRow.AreaBuffId))
            {
                foreach (var curEntity in currAffectedEntites)
                {
                    LogicManager.globalBuffManager.RemoveAllBuffById(curEntity, CfgRow.AreaBuffId, casterId: Id);
                }
            }

            currAffectedEntites.Clear();
        }
    }
}
